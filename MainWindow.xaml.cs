using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Privatus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        private static UserManager _UserManager;
        private static bool IsLogin = true;
        private static readonly SolidColorBrush TransparentBrush = new SolidColorBrush(Colors.Transparent);
        private static Color SignHoverColor = Color.FromRgb(38, 133, 54);
        private static readonly SolidColorBrush SignHoverBrush = new SolidColorBrush(SignHoverColor);

        public MainWindow()
        {
            InitializeComponent();

            ComponentDispatcher.ThreadIdle += new EventHandler((object sender, EventArgs e) =>
            {

                if (Keyboard.IsKeyToggled(Key.CapsLock))
                {
                    CapsWarningLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    CapsWarningLabel.Visibility = Visibility.Hidden;
                }
            });
        }

        private void SignButtonBG_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = SignHoverBrush;
        }

        private void SignButtonBG_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = TransparentBrush;
        }



        private void SwitchButton_MouseEnter(object sender, MouseEventArgs e)
        {
            SwitchButton.TextDecorations = TextDecorations.Underline;
        }

        private void SwitchButton_MouseLeave(object sender, MouseEventArgs e)
        {
            SwitchButton.TextDecorations = null;
        }

        private void SwitchButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                IsLogin = !IsLogin;

                if (IsLogin)
                {
                    SwitchButton.Text = "Register";
                }
                else
                {
                    SwitchButton.Text = "Login";
                }
            }

        }

        private void LoginRegisterButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _UserManager = new UserManager(UsernameBox.Text, Passwordbox.Password);

                if (UsernameBox.Text.Length < 2)
                {
                    MessageBox.Show("Usernames must be at least 2 characters in length");
                    return;
                }

                if (Passwordbox.Password.Length < 14)
                {
                    MessageBox.Show("Passwords must be at least 14 characters in length");
                    return;
                }
                if (IsLogin)
                {
                    switch (_UserManager.AuthenticateUser())
                    {
                        case UserManager.UserAuthStatus.Success:
                            new ChatWindow(_UserManager, false).Show();
                            Close();
                            break;
                        case UserManager.UserAuthStatus.IncorrectPassword:
                            MessageBox.Show("Incorrect password");
                            break;
                        case UserManager.UserAuthStatus.InvalidUser:
                            MessageBox.Show("User does not exist.");
                            break;
                        case UserManager.UserAuthStatus.BadHash:
                            MessageBox.Show("Error: Bad hash");
                            break;
                        case UserManager.UserAuthStatus.MissingData:
                            MessageBox.Show("Error: Missing data");
                            break;
                        default:
                            MessageBox.Show("Incorrect password");
                            break;
                    }
                }
                else
                {
                    if (!_UserManager.UserExists())
                    {
                        new ChatWindow(_UserManager, true).Show();
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("User already exists");
                    }
                }
            }

        }
    }
}
