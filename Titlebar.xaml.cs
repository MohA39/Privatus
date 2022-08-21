using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Privatus
{
    /// <summary>
    /// Interaction logic for Titlebar.xaml
    /// </summary>
    public partial class Titlebar : UserControl
    {


        private static Color _CloseHoverColor = Color.FromRgb(160, 18, 57);
        private static readonly SolidColorBrush _CloseHoverBrush = new SolidColorBrush(_CloseHoverColor);
        private static Color _MinimizeHoverColor = Color.FromRgb(160, 151, 18);
        private static readonly SolidColorBrush _MinimizeHoverBrush = new SolidColorBrush(_MinimizeHoverColor);


        private static readonly SolidColorBrush _TransparentBrush = new SolidColorBrush(Colors.Transparent);
        private Window _window;
        public Titlebar()
        {
            InitializeComponent();

        }

        private void Bar_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _window.DragMove();
            }
        }

        private void Bar_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }
#endif

            _window = Window.GetWindow(this);
        }

        private void MinimizeButtonBG_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = _MinimizeHoverBrush;
        }

        private void MinimizeButtonBG_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = _TransparentBrush;

        }

        private void CloseButtonBG_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = _CloseHoverBrush;
        }

        private void CloseButtonBG_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Border)sender).Background = _TransparentBrush;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _window.Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            _window.WindowState = WindowState.Minimized;
        }
    }
}
