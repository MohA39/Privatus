using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Privatus
{
    /// <summary>
    /// Interaction logic for FriendBlock.xaml
    /// </summary>
    /// 

    public partial class FriendBlock : UserControl
    {
        public Friend Friend { get; private set; }
        public string FriendName { get; private set; } = "USERNAME";
        public FriendStatus FriendStatus { get; private set; }

        private int _UnreadMessageCount;


        public int UnreadMessageCount
        {
            get => _UnreadMessageCount;
            set
            {
                MessageNotificationEllipse.Visibility = value > 0 ? Visibility.Visible : Visibility.Hidden;
                MessageNotificationLabel.Visibility = value > 0 ? Visibility.Visible : Visibility.Hidden;
                MessageNotificationLabel.Content = value.ToString();
                _UnreadMessageCount = value;

            }
        }

        public FriendBlock(Friend friend)
        {
            InitializeComponent();

            DataContext = this;
            UnreadMessageCount = 0;

            Friend = friend;
            FriendName = friend.Name;
            UsernameLabel.Content = FriendName;
            AvatarImage.Source = friend.Avatar;
            FriendStatus = friend.Status;

            UpdateStatusLabel(FriendStatus);
        }

        public void UpdateStatusLabel(FriendStatus FriendStatus)
        {
            SolidColorBrush OnlineBrush = new SolidColorBrush(Color.FromRgb(38, 133, 54));
            SolidColorBrush OfflineBlockedBrush = new SolidColorBrush(Color.FromRgb(160, 18, 57));
            SolidColorBrush UnknownBrush = new SolidColorBrush(Color.FromRgb(160, 151, 18));

            switch (FriendStatus)
            {
                case FriendStatus.ONLINE:
                    StatusLabel.Foreground = OnlineBrush;
                    break;
                case FriendStatus.OFFLINE:
                    StatusLabel.Foreground = OfflineBlockedBrush;
                    break;
                case FriendStatus.BLOCKED:
                    StatusLabel.Foreground = OfflineBlockedBrush;
                    break;
                case FriendStatus.UNKNOWN:
                    StatusLabel.Foreground = UnknownBrush;
                    break;
            }

            StatusLabel.Content = FriendStatus.ToString();
        }

        public void UpdateAvatar()
        {
            AvatarImage.Source = Friend.Avatar;
        }

        public void Select()
        {
            FriendBlockContainer.Background = new SolidColorBrush(Color.FromRgb(0x2F, 0x31, 0x36));
        }

        public void Deselect()
        {
            FriendBlockContainer.Background = new SolidColorBrush(Color.FromRgb(0x28, 0x2A, 0x2E));

        }
    }
}
