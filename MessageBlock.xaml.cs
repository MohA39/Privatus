using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Privatus
{
    /// <summary>
    /// Interaction logic for FriendBlock.xaml
    /// </summary>
    /// 
    public enum MessageSource
    {
        CurrentUser,
        Friend
    }

    public class Message
    {
        public MessageSource Source { get; private set; }
        public string Username { get; private set; }
        public ImageSource Avatar { get; private set; }
        public string Timestamp { get; private set; }

        public List<object> Content { get; set; }
        public Message(MessageSource source, string username, ImageSource avatar, string timestamp = null)
        {
            Content = new List<object>();
            Source = source;
            Username = username;
            Avatar = avatar;
            Timestamp = timestamp == null ? DateTime.Now.ToString() : timestamp;
        }
    }
    public partial class MessageBlock : UserControl
    {
        public Message Message { get; private set; }
        public MessageSource MessageSource { get; private set; }

        public MessageBlock(Message message)
        {
            InitializeComponent();
            Message = message;
            UsernameLabel.Content = message.Username;
            AvatarImage.Source = message.Avatar;
            TimestampLabel.Content = message.Timestamp;
            MessageSource = message.Source;
        }


        public void AppendMessage(string message)
        {
            TextBox textbox = new TextBox()
            {
                Text = message,
                Foreground = new SolidColorBrush(Color.FromRgb(185, 187, 190)),
                Background = new SolidColorBrush(Colors.Transparent),
                SelectionBrush = new SolidColorBrush(Color.FromRgb(0, 75, 134)),
                /*HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,*/
                Margin = new Thickness(0, 2, 0, 2), // Top and bottom margins
                TextWrapping = TextWrapping.Wrap,
                BorderThickness = new Thickness(0, 0, 0, 0),
                IsReadOnly = true
            };


            Message.Content.Add(message);
            Container.Children.Add(textbox);
        }

        public void AppendImage(BitmapSource imagesource)
        {
            imagesource.Freeze();
            Image image = new Image()
            {
                Source = imagesource,
                MinHeight = 100,
                MinWidth = 100

            };


            /*
            BitmapEncoder encoder = new PngBitmapEncoder();
            MemoryStream MS = new MemoryStream();
            
            encoder.Frames.Add(BitmapFrame.Create(imagesource));
            
            encoder.Save(MS);*/

            Message.Content.Add(image);
            Container.Children.Add(image);
        }
    }
}