using System.Windows.Media.Imaging;

namespace Privatus
{
    public class User
    {
        public string Name { get; private set; }
        public string PrivateKey { get; private set; }
        public string PublicKey { get; private set; }

        public BitmapSource Avatar { get; set; }
        public User(string name, string privateKey, string publicKey, BitmapSource avatar)
        {
            Name = name;
            PrivateKey = privateKey;
            PublicKey = publicKey;
            Avatar = avatar;
        }
    }
}
