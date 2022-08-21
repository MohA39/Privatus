using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Privatus
{
    internal class ImageUtils
    {

        public static MemoryStream ImageToMemoryStream(BitmapSource Image, int QualityLevel = 80)
        {
            using MemoryStream MemoryStream = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder
            {
                QualityLevel = QualityLevel
            };
            encoder.Frames.Add(BitmapFrame.Create(Image));
            encoder.Save(MemoryStream);

            return MemoryStream;

        }

        public static BitmapSource MemoryStreamToImage(MemoryStream memoryStream)
        {
            JpegBitmapDecoder decoder = new JpegBitmapDecoder(memoryStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }

        public static string ImageToBase64(BitmapSource Image, int QualityLevel = 80)
        {
            using MemoryStream MemoryStream = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder
            {
                QualityLevel = QualityLevel
            };
            encoder.Frames.Add(BitmapFrame.Create(Image));
            encoder.Save(MemoryStream);

            return Convert.ToBase64String(MemoryStream.ToArray());
        }
        public static BitmapSource Base64ToImage(string Base64)
        {
            return MemoryStreamToImage(new MemoryStream(Convert.FromBase64String(Base64)));
        }
        public static TransformedBitmap ResizeImage(BitmapSource image, double width, double height)
        {
            double scaleX = width / image.PixelWidth;
            double scaleY = height / image.PixelHeight;

            return new TransformedBitmap(image, new ScaleTransform(scaleX, scaleY));
        }
    }
}
