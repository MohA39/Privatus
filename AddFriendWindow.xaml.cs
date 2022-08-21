using I2PSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Privatus
{
    /// <summary>
    /// Interaction logic for AddFriendWindow.xaml
    /// </summary>
    /// 

    public class CompleteEventArgs : EventArgs
    {
        public bool Success { get; private set; }
        public Friend NewFriend { get; private set; }

        public CompleteEventArgs(bool success, Friend newFriend)
        {
            Success = success;
            NewFriend = newFriend;
        }
    }
    public partial class AddFriendWindow : Window
    {
        private readonly SAMSubsession _SAMSubsession;
        private readonly UserManager _UserManager;
        private string _TargetPublicKey;
        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        private static Color AddFriendHoverColor = Color.FromRgb(38, 133, 54);
        private static readonly SolidColorBrush AddFriendHoverBrush = new SolidColorBrush(AddFriendHoverColor);

        private bool _IsRequestSent = false;
        private const int Timeout = 5000;
        public delegate void CompleteEventHandler(object sender, CompleteEventArgs completeEventArgs);
        public event CompleteEventHandler OnComplete;
        public AddFriendWindow(SAMSubsession ConnectorSubsession, UserManager manager)
        {
            InitializeComponent();
            _SAMSubsession = ConnectorSubsession;
            _UserManager = manager;

        }

        private void AddFriendBG_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = AddFriendHoverBrush;
        }

        private void AddFriendBG_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = TransparentBrush;
        }

        private async void AddFriendButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _TargetPublicKey = PublicKeyTextBox.Text;
            if (e.ChangedButton == MouseButton.Left && !_IsRequestSent)
            {


                if (!_UserManager.Friends.Any(x => x.PublicKey.ToLower() == _TargetPublicKey.ToLower()))
                {
                    PeerConnection ConnectedPeer = await _SAMSubsession.ConnectAsync(_TargetPublicKey);

                    if (ConnectedPeer != null)
                    {
                        _IsRequestSent = true;
                        ConnectedPeer.OnMessage += ConnectedPeer_OnMessage;
                        ConnectedPeer.OnDisconnect += ConnectedPeer_OnDisconnect;
                        await ConnectedPeer.SendString($"~COMMAND REQUESTFRIEND NAME=\"{_UserManager.GetUser().Name}\" AVATAR=\"{ImageUtils.ImageToBase64(_UserManager.GetUser().Avatar)}\"");
                    }
                    else
                    {
                        MessageBox.Show("Failed to connect to friend.");
                    }
                }
                else
                {
                    MessageBox.Show("Already a friend.");
                }
            }
        }

        private void ConnectedPeer_OnDisconnect(object sender, DisconnectEventArgs e)
        {
            MessageBox.Show("Friend request timed out or rejected.");
            Application.Current.Dispatcher.Invoke(() => PublicKeyTextBox.Text = string.Empty);
            _IsRequestSent = false;
        }

        private void ConnectedPeer_OnMessage(object sender, MessageEventArgs e)
        {
            PeerConnection thisConnectedPeer = (PeerConnection)sender;
            if (e.Message.StartsWith("~RESPONSE ACCEPTFRIEND"))
            {
                (SAMResponseResults result, Dictionary<string, string> response) ParsedResponse = Utils.TryParseResponse(e.Message);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Friend friend = _UserManager.CreateFriend(ParsedResponse.response["USERNAME"], thisConnectedPeer.PeerPublicKey, ImageUtils.Base64ToImage(ParsedResponse.response["AVATAR"]), thisConnectedPeer); // TO BE REPLACED WITH REAL IMAGE
                    MessageBox.Show("Friend added!");

                    thisConnectedPeer.OnMessage -= ConnectedPeer_OnMessage;

                    if (OnComplete != null)
                    {
                        OnComplete(this, new CompleteEventArgs(true, friend));
                    }
                    thisConnectedPeer.OnDisconnect -= ConnectedPeer_OnDisconnect;
                    Close();
                });

            }
        }


    }
}
