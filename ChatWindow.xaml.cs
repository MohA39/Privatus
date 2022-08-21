using I2PSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;


namespace Privatus
{
    /// <summary>
    /// Interaction logic for ChatWindow.xaml
    /// </summary>
    public partial class ChatWindow : Window
    {
        private User _CurrentUser;
        private readonly bool _CreateUser;
        private SAMSession _SAMSession;
        public UserManager _UserManager;

        private Friend _OpenChat;
        private SAMSubsession _PeerSubsession;
        private readonly DispatcherTimer KeepAliveTimer = new DispatcherTimer();
        private bool _PingPaused = false;
        private bool _AutoScroll = true;
        public ChatWindow(UserManager userManager, bool CreateUser)
        {
            InitializeComponent();
            _UserManager = userManager;
            _SAMSession = new SAMSession(7656);
            _CreateUser = CreateUser;

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {

            while (true)
            {
                try
                {
                    await _SAMSession.ConnectAsync();
                    break;
                }
                catch (SocketException)
                {
                    MessageBox.Show("Error: I2P SAM is not started.");
                }
            }


            if (_CreateUser)
            {
                (string PrivateKey, string PublicKey) = await _SAMSession.GenerateDestinationAsync();
                _CurrentUser = _UserManager.CreateUser(PrivateKey, PublicKey);
            }
            else
            {
                _CurrentUser = _UserManager.GetUser();
            }

            _UserManager.GetAllFriends();
            double DefaultSize = PublicKeyTextBlock.FontSize;
            UsernameLabel.Content = _CurrentUser.Name;


            PublicKeyTextBlock.FontSize = 9;
            PublicKeyTextBlock.Text = "Uninitialized";

            AvatarPictureBox.Source = _CurrentUser.Avatar;


            await _SAMSession.CreateSessionAsync(_CurrentUser.PrivateKey, new string[] { "i2cp.enableBlackList=true", $"i2cp.accessList=\"{string.Join(',', _UserManager.Blocklist)}\"" });

            _PeerSubsession = await _SAMSession.CreateSTREAMSubsessionAsync();
            _PeerSubsession.OnConnect += PeerSubsession_OnConnect;
            await TryConnectToAllFriends();
            AddFriendButton.IsEnabled = true;
            PublicKeyTextBlock.FontSize = DefaultSize;
            PublicKeyTextBlock.Text = _CurrentUser.PublicKey.Replace('-', '‑'); // non-breaking hyphen to prevent wrapping

            KeepAliveTimer.Tick += KeepAliveTimer_Tick;
            KeepAliveTimer.Interval = TimeSpan.FromSeconds(2);
            KeepAliveTimer.Start();

            CommandManager.RegisterClassCommandBinding(typeof(ChatWindow), new CommandBinding(ApplicationCommands.Paste, OnPasteExecute));
            await _PeerSubsession.AcceptConnectionAsync();

        }

        private async void KeepAliveTimer_Tick(object sender, EventArgs e)
        {
            List<Friend> FriendsToPing = _UserManager.Friends.Where(f => f.Status == FriendStatus.ONLINE).ToList();

            if (FriendsToPing.Count == 0)
            {
                await TryConnectToAllFriends();
            }
            if (_PingPaused == true)
            {
                return;
            }
            foreach (Friend f in FriendsToPing)
            {
                if (f.Status == FriendStatus.ONLINE)
                {
                    await f.FriendConnectedSession.SendString("~PING");
                }
            }
        }

        private async void PeerSubsession_OnConnect(object sender, ConnectionEventArgs e)
        {
            SAMSubsession thisSubsession = (SAMSubsession)sender;
            Friend SubsessionFriend = _UserManager.GetFriendByPublicKey(e.PeerConnection.PeerPublicKey);
            e.PeerConnection.OnMessage += ConnectedPeer_OnMessage;
            e.PeerConnection.OnDisconnect += ConnectedPeer_OnDisconnect;

            if (SubsessionFriend != null)
            {
                SubsessionFriend.FriendConnectedSession = e.PeerConnection;
                SubsessionFriend.Status = FriendStatus.ONLINE;

                if (!SubsessionFriend.HasLatestAvatar)
                {
                    await SendAvatar(SubsessionFriend, ImageUtils.ImageToMemoryStream(_CurrentUser.Avatar).ToArray());
                }
                if (!SubsessionFriend.IsAddedToFriendList)
                {
                    AddFriendToFriendlist(SubsessionFriend);
                }
                else
                {
                    if (_OpenChat == SubsessionFriend)
                    {
                        ConversationContainerScrollViewer.AllowDrop = true;
                        SendMessageBox.IsEnabled = true;
                    }
                    SubsessionFriend.FriendBlock.UpdateStatusLabel(FriendStatus.ONLINE);
                }

            }

            if (e.ConnectionType == ConnectionTypes.AcceptedPeer)
            {
                await _PeerSubsession.AcceptConnectionAsync();
            }

        }

        private void ConnectedPeer_OnDisconnect(object sender, DisconnectEventArgs e)
        {

            PeerConnection peerConnection = (PeerConnection)sender;
            Friend SubsessionFriend = _UserManager.GetFriendByPublicKey(peerConnection.PeerPublicKey);
            SubsessionFriend.LastDisconnect = DateTime.Now;
            if (SubsessionFriend != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_OpenChat == SubsessionFriend)
                    {
                        ConversationContainerScrollViewer.AllowDrop = false;
                        SendMessageBox.IsEnabled = false;
                    }
                    SubsessionFriend.HandleFriendDisconnect();
                });
            }
        }

        private async void ConnectedPeer_OnMessage(object sender, MessageEventArgs e)
        {
            PeerConnection thisConnectedPeer = (PeerConnection)sender;

            Friend SubsessionFriend = _UserManager.GetFriendByPublicKey(thisConnectedPeer.PeerPublicKey);

            if (SubsessionFriend != null)
            {
                if (e.Message.StartsWith("~")) // COMMAND
                {
                    if (e.Message == "~PING" && !_PingPaused)
                    {
                        await thisConnectedPeer.SendString("~PONG");
                    }

                    if (e.Message.StartsWith("~ACCEPTIMAGE"))
                    {
                        SubsessionFriend.FriendConnectedSession.SetReadMode(ReadModes.Byte);
                        int size = Convert.ToInt32(Utils.TryParseResponse(e.Message).response["SIZE"]);

                        byte[] ImageBytes = await SubsessionFriend.FriendConnectedSession.ReadBytes(size, 5000);
                        if (ImageBytes != null)
                        {
                            MemoryStream MS = new MemoryStream(ImageBytes);

                            Application.Current.Dispatcher.Invoke(() =>
                            {

                                GetCreateMessageBlock(SubsessionFriend).AppendImage(ImageUtils.MemoryStreamToImage(MS));
                                IncrementUnreadCounter(SubsessionFriend);
                            });
                        }
                        else
                        {
                            MessageBox.Show("Image receive failed.");
                        }


                        SubsessionFriend.FriendConnectedSession.SetReadMode(ReadModes.String);
                        SubsessionFriend.FriendConnectedSession.WaitForMessages();
                    }

                    if (e.Message.StartsWith("~COMMAND UPDATEAVATAR"))
                    {
                        byte[] Imagebytes = Convert.FromBase64String(Utils.TryParseResponse(e.Message).response["AVATAR"]);
                        SubsessionFriend.Avatar = ImageUtils.MemoryStreamToImage(new MemoryStream(Imagebytes));
                        Application.Current.Dispatcher.Invoke(() => SubsessionFriend.FriendBlock.UpdateAvatar());
                        _UserManager.UpdateFriend(SubsessionFriend);
                    }
                }
                else // MESSAGE
                {



                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        GetCreateMessageBlock(SubsessionFriend).AppendMessage(e.Message);
                        IncrementUnreadCounter(SubsessionFriend);
                        PlaceFriendBlockAtTop(SubsessionFriend.FriendBlock);
                    }
                    );
                }

            }
            else
            {
                if (e.Message.StartsWith("~COMMAND REQUESTFRIEND"))
                {

                    (SAMResponseResults result, Dictionary<string, string> response) = Utils.TryParseResponse(e.Message);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        BitmapSource FriendAvatar = ImageUtils.Base64ToImage(response["AVATAR"]);
                        FriendRequestBlock friendRequestBlock = new FriendRequestBlock(response["NAME"], FriendAvatar);
                        friendRequestBlock.OnRequestResponse += async (object sender, RequestResponseEventArgs e) =>
                        {
                            FriendlistBox.Children.Remove((FriendRequestBlock)sender);
                            if (e.IsAccepted)
                            {
                                await thisConnectedPeer.SendString($"~RESPONSE ACCEPTFRIEND USERNAME=\"{_CurrentUser.Name}\" AVATAR=\"{ImageUtils.ImageToBase64(_CurrentUser.Avatar)}\"");


                                Friend f = _UserManager.CreateFriend(response["NAME"], thisConnectedPeer.PeerPublicKey, FriendAvatar, thisConnectedPeer);
                                f.Status = FriendStatus.ONLINE;
                                _UserManager.Friends.Add(f);

                                if (!f.IsAddedToFriendList)
                                {
                                    AddFriendToFriendlist(f);
                                }

                            }
                            else
                            {
                                thisConnectedPeer.Dispose();
                            }

                        };

                        FriendlistBox.Children.Insert(0, friendRequestBlock);
                    });


                }
            }

        }

        private void IncrementUnreadCounter(Friend MessageSender)
        {
            if (_OpenChat != null && MessageSender.FriendBlock == _OpenChat.FriendBlock)
            {
                if (!_AutoScroll)
                {
                    MessageSender.FriendBlock.UnreadMessageCount++;
                }
            }
            else
            {
                MessageSender.FriendBlock.UnreadMessageCount++;
            }
        }
        private void AddFriendButton_Click(object sender, RoutedEventArgs e)
        {
            AddFriendWindow Window = new AddFriendWindow(_PeerSubsession, _UserManager);
            Window.OnComplete += (object Sender, CompleteEventArgs completeEventArgs) =>
            {
                _UserManager.Friends.Add(completeEventArgs.NewFriend);
                if (!completeEventArgs.NewFriend.IsAddedToFriendList)
                {
                    AddFriendToFriendlist(completeEventArgs.NewFriend);
                }

            };
            Window.Show();
        }

        private async void PublicKeyLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Clipboard.SetText(_CurrentUser.PublicKey);
            string OldText = PublicKeyTextBlock.Text;
            PublicKeyTextBlock.Text = "Copied!";

            double DefaultSize = PublicKeyTextBlock.FontSize;
            PublicKeyTextBlock.FontSize = 9;
            await Task.Run(async () =>
            {
                await Task.Delay(600);
                Dispatcher.Invoke(() =>
                {
                    PublicKeyTextBlock.Text = OldText;
                    PublicKeyTextBlock.FontSize = DefaultSize;

                });

            });
        }

        private async Task TryConnectToAllFriends()
        {
            List<Friend> friendsCopy = _UserManager.Friends.ToList();
            foreach (Friend f in friendsCopy)
            {
                if (!_UserManager.Blocklist.Contains(f.PublicKey))
                {
                    if (DateTime.Now - f.LastDisconnect < TimeSpan.FromSeconds(10)) // Friend disconnected last 10 seconds.
                    {
                        continue;
                    }
                    if (f.Status != FriendStatus.ONLINE)
                    {
                        PeerConnection connectedSession = null;
                        try
                        {
                            connectedSession = await _PeerSubsession.ConnectAsync(f.PublicKey).WaitAsync(TimeSpan.FromSeconds(3));

                        }
                        catch (TimeoutException) { }

                        if (connectedSession != null)
                        {
                            f.FriendConnectedSession = connectedSession;
                            if (!f.HasLatestAvatar)
                            {
                                await SendAvatar(f, ImageUtils.ImageToMemoryStream(_CurrentUser.Avatar).ToArray());
                                f.HasLatestAvatar = true;
                                _UserManager.UpdateFriend(f);
                            }
                            f.Status = FriendStatus.ONLINE;
                        }
                        else
                        {
                            f.Status = FriendStatus.OFFLINE;
                        }
                    }
                }
                else
                {
                    f.Status = FriendStatus.BLOCKED;
                    if (f.FriendConnectedSession != null)
                    {

                        f.FriendConnectedSession.Dispose();
                    }
                }
                if (!f.IsAddedToFriendList)
                {
                    AddFriendToFriendlist(f);
                }
            }
        }

        private void AddFriendToFriendlist(Friend F)
        {
            FriendBlock friendBlock = new FriendBlock(F);

            ContextMenu contextMenu = new ContextMenu();

            MenuItem BlockFriendItem = new MenuItem
            {
                Header = "BLOCK",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 18, 57))
            };

            MenuItem UnblockFriendItem = new MenuItem
            {
                Header = "UNBLOCK",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 18, 57))
            };

            BlockFriendItem.Click += async (object sender, RoutedEventArgs e) =>
            {

                if (_OpenChat == F)
                {
                    _OpenChat = null;
                }

                if (F.FriendConnectedSession != null)
                {
                    F.FriendConnectedSession.Dispose();
                }
                F.Status = FriendStatus.BLOCKED;
                F.FriendBlock.UpdateStatusLabel(FriendStatus.BLOCKED);
                _UserManager.UpdateBlocklist(F.PublicKey);

                contextMenu.Items.Remove(BlockFriendItem);
                contextMenu.Items.Insert(0, UnblockFriendItem);

                // Recreate session to update blocklist, surely there's a better way?
                await RecreateSession();


            };




            UnblockFriendItem.Click += async (object sender, RoutedEventArgs e) =>
            {

                if (_UserManager.Blocklist.Contains(F.PublicKey))
                {
                    _UserManager.Blocklist.Remove(F.PublicKey);
                    _UserManager.UpdateBlocklist();
                }
                F.Status = FriendStatus.OFFLINE;
                F.FriendBlock.UpdateStatusLabel(FriendStatus.OFFLINE);

                contextMenu.Items.Remove(UnblockFriendItem);
                contextMenu.Items.Insert(0, BlockFriendItem);

                // Recreate session to update blocklist, surely there's a better way?
                await RecreateSession();


            };


            if (_UserManager.Blocklist.Contains(F.PublicKey))
            {
                contextMenu.Items.Add(UnblockFriendItem);
            }
            else
            {
                contextMenu.Items.Add(BlockFriendItem);
            }

            MenuItem CopyPublicKeyItem = new MenuItem
            {
                Header = "Copy public key"
            };
            CopyPublicKeyItem.Click += (object sender, RoutedEventArgs e) =>
             {
                 Clipboard.SetText(F.PublicKey);

             };
            contextMenu.Items.Add(CopyPublicKeyItem);

            friendBlock.MouseUp += FriendBlock_MouseUp;



            friendBlock.ContextMenu = contextMenu;

            FriendlistBox.Children.Add(friendBlock);
            F.ConversationContainer = new StackPanel();
            F.FriendBlock = friendBlock;
            F.IsAddedToFriendList = true;
        }

        private async Task RecreateSession()
        {
            await _SAMSession.EndAsync();
            _SAMSession = new SAMSession(7656);
            await _SAMSession.ConnectAsync();
            await _SAMSession.CreateSessionAsync(_CurrentUser.PrivateKey, new string[] { "i2cp.enableBlackList=true", $"i2cp.accessList=\"{string.Join(',', _UserManager.Blocklist)}\"" });
            _PeerSubsession = await _SAMSession.CreateSTREAMSubsessionAsync();
            _PeerSubsession.OnConnect += PeerSubsession_OnConnect;
            await TryConnectToAllFriends();
            await _PeerSubsession.AcceptConnectionAsync();
        }
        private MessageBlock GetCreateMessageBlock(User user, string timestamp = null)
        {
            return GetCreateMessageBlock(_OpenChat.ConversationContainer, new Message(MessageSource.CurrentUser, user.Name, user.Avatar, timestamp));
        }

        private MessageBlock GetCreateMessageBlock(Friend friend, string timestamp = null)
        {
            return GetCreateMessageBlock(friend.ConversationContainer, new Message(MessageSource.Friend, friend.Name, friend.Avatar, timestamp));
        }

        private MessageBlock GetCreateMessageBlock(StackPanel ConversationContainer, Message message)
        {
            MessageBlock LastMessageBlock = ConversationContainer.Children.Count > 0 ? (MessageBlock)ConversationContainer.Children[ConversationContainer.Children.Count - 1] : null;



            if (message.Source == MessageSource.Friend)
            {
                if (LastMessageBlock != null && LastMessageBlock.MessageSource == MessageSource.Friend)
                {
                    return LastMessageBlock;
                }
                else
                {
                    MessageBlock messageBlock = new MessageBlock(message);
                    ConversationContainer.Children.Add(messageBlock);
                    return messageBlock;
                }

            }
            else
            {
                if (LastMessageBlock != null && LastMessageBlock.MessageSource == MessageSource.CurrentUser)
                {
                    return LastMessageBlock;

                }
                else
                {
                    MessageBlock messageBlock = new MessageBlock(message);
                    ConversationContainer.Children.Add(messageBlock);
                    return messageBlock;
                }
            }
        }




        private void FriendBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {

                FriendBlock block = (FriendBlock)sender;

                if (_OpenChat != null)
                {
                    _OpenChat.FriendBlock.Deselect();
                }

                block.Select();
                _OpenChat = block.Friend;
                ConversationContainerScrollViewer.Content = _OpenChat.ConversationContainer;

                if (block.Friend.Status == FriendStatus.ONLINE)
                {
                    ConversationContainerScrollViewer.AllowDrop = true;
                    SendMessageBox.IsEnabled = true;
                }
                else
                {
                    ConversationContainerScrollViewer.AllowDrop = false;
                    SendMessageBox.IsEnabled = false;
                }

                _OpenChat.FriendBlock.UnreadMessageCount = 0;
            }

        }

        private async void SendMessageBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter && SendMessageBox.Text.Length > 0)
            {
                await _OpenChat.FriendConnectedSession.SendString(SendMessageBox.Text);
                PlaceFriendBlockAtTop(_OpenChat.FriendBlock);
                GetCreateMessageBlock(_CurrentUser).AppendMessage(SendMessageBox.Text);

                SendMessageBox.Text = string.Empty;
            }
        }

        private async void ConversationContainerScrollViewer_Drop(object sender, DragEventArgs e)
        {
            if (_OpenChat != null)
            {
                double MaxPixelCount = 1280 * 720; // 720P (420P: 720 * 420)

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] AcceptableExtensions = new string[] { ".JPEG", ".JPG", ".PNG", ".BMP" };
                    string file = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];

                    if (AcceptableExtensions.Contains(Path.GetExtension(file).ToUpper()))
                    {


                        BitmapImage image = new BitmapImage(new Uri(file));
                        await SendImage(image, MaxPixelCount);

                    }
                    else
                    {
                        MessageBox.Show("Unsupported file type.");
                    }

                }
            }

        }

        private async Task SendImage(BitmapSource image, double MaxPixelCount)
        {
            byte[] ImageBytes;
            double ImagePixelCount = image.PixelWidth * image.PixelHeight;
            double Scale = Math.Min(Math.Sqrt(MaxPixelCount / ImagePixelCount), 1);

            ImageBytes = ImageUtils.ImageToMemoryStream(new TransformedBitmap(image, new ScaleTransform(Scale, Scale))).ToArray();

            _PingPaused = true;
            await _OpenChat.FriendConnectedSession.SendString($"~ACCEPTIMAGE SIZE={ImageBytes.Length}");
            Thread.Sleep(500); // bad, wait until the peer starts accepting
            await _OpenChat.FriendConnectedSession.SendBytes(ImageBytes);
            _PingPaused = false;
            GetCreateMessageBlock(_CurrentUser).AppendImage(image);
        }
        private void ConversationContainerScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange == 0)
            {
                if (ConversationContainerScrollViewer.VerticalOffset == ConversationContainerScrollViewer.ScrollableHeight)
                {
                    if (_OpenChat != null)
                    {
                        _OpenChat.FriendBlock.UnreadMessageCount = 0;
                    }
                    _AutoScroll = true;
                }
                else
                {
                    _AutoScroll = false;
                }
            }
            else
            {
                if (_AutoScroll)
                {
                    ConversationContainerScrollViewer.ScrollToVerticalOffset(ConversationContainerScrollViewer.ExtentHeight);
                }
            }
        }

        private void AvatarPictureBox_MouseLeave(object sender, MouseEventArgs e)
        {
            ChangeAvatarTextBlock.Visibility = Visibility.Hidden;
        }

        private void AvatarPictureBox_MouseEnter(object sender, MouseEventArgs e)
        {
            ChangeAvatarTextBlock.Visibility = Visibility.Visible;
        }

        private async void AvatarPictureBox_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog
                {
                    FileName = "Avatar",
                    DefaultExt = ".jpeg",
                    Filter = "JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg",
                    CheckFileExists = true
                };

                bool? result = dialog.ShowDialog();
                if (result == true)
                {
                    string filepath = dialog.FileName;

                    BitmapSource Image = ImageUtils.ResizeImage(_UserManager.ChangeAvatar(filepath), 64, 64);
                    byte[] ImageBytes = ImageUtils.ImageToMemoryStream(Image).ToArray();
                    AvatarPictureBox.Source = Image;

                    foreach (Friend f in _UserManager.Friends)
                    {
                        if (f.Status == FriendStatus.ONLINE)
                        {
                            await SendAvatar(f, ImageBytes);
                            f.HasLatestAvatar = true;
                        }
                        else
                        {
                            f.HasLatestAvatar = false;
                        }
                    }
                    ;
                }
            }
        }

        public async Task SendAvatar(Friend friend, byte[] avatarbytes)
        {
            await friend.FriendConnectedSession.SendString($"~COMMAND UPDATEAVATAR AVATAR=\"{Convert.ToBase64String(avatarbytes)}\"");
        }

        public void PlaceFriendBlockAtTop(FriendBlock friendBlock)
        {

            for (int i = 0; i < FriendlistBox.Children.Count; i++)
            {
                UIElement child = FriendlistBox.Children[i];

                if (child.GetType() == typeof(FriendBlock))
                {
                    if (i != FriendlistBox.Children.IndexOf(friendBlock))
                    {
                        FriendlistBox.Children.Remove(friendBlock);
                        FriendlistBox.Children.Insert(i, friendBlock);
                    }
                    return;
                }
            }
        }

        private async void OnPasteExecute(object sender, ExecutedRoutedEventArgs e)
        {
            BitmapSource image = Clipboard.GetImage();

            if (image != null)
            {
                await SendImage(image, 1280 * 720);
            }
            MessageBox.Show("Paste!");
        }
    }
}
