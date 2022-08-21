using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Privatus
{
    /// <summary>
    /// Interaction logic for FriendBlock.xaml
    /// </summary>
    /// 

    public class RequestResponseEventArgs : EventArgs
    {
        public bool IsAccepted { get; private set; }

        public RequestResponseEventArgs(bool isAccepted)
        {
            IsAccepted = isAccepted;
        }
    }
    public partial class FriendRequestBlock : UserControl
    {
        private static readonly SolidColorBrush _AcceptBrush = new SolidColorBrush(Color.FromRgb(38, 133, 54));
        private static readonly SolidColorBrush _DeclineBrush = new SolidColorBrush(Color.FromRgb(160, 18, 57));
        private static readonly SolidColorBrush _TransparentBrush = new SolidColorBrush(Colors.Transparent);

        public Friend Friend { get; private set; }
        public string FriendName { get; private set; } = "USERNAME";

        public delegate void RequestResponseHandler(object sender, RequestResponseEventArgs e);
        public event RequestResponseHandler OnRequestResponse;

        public FriendRequestBlock(string Name, BitmapSource Avatar)
        {
            InitializeComponent();

            DataContext = this;
            FriendName = Name;
            UsernameLabel.Content = FriendName;
            AvatarImage.Source = Avatar;

        }

        private void DeclineButtonBG_MouseEnter(object sender, MouseEventArgs e)
        {
            DeclineButtonBG.Background = _DeclineBrush;
        }

        private void DeclineButtonBG_MouseLeave(object sender, MouseEventArgs e)
        {
            DeclineButtonBG.Background = _TransparentBrush;
        }

        private void AcceptButtonBG_MouseEnter(object sender, MouseEventArgs e)
        {
            AcceptButtonBG.Background = _AcceptBrush;
        }

        private void AcceptButtonBG_MouseLeave(object sender, MouseEventArgs e)
        {
            AcceptButtonBG.Background = _TransparentBrush;
        }

        private void DeclineButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (OnRequestResponse != null)
                {
                    OnRequestResponse(this, new RequestResponseEventArgs(false));
                }
            }

        }

        private void AcceptButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (OnRequestResponse != null)
                {
                    OnRequestResponse(this, new RequestResponseEventArgs(true));
                }
            }

        }
    }
}
