using SecurityDriven.Inferno;
using SecurityDriven.Inferno.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Privatus
{

    public class UserManager
    {
        public enum UserAuthStatus
        {
            IncorrectPassword,
            BadHash,
            InvalidUser,
            MissingData,
            Success
        }

        private static readonly string _DataDirectory = AppDomain.CurrentDomain.BaseDirectory + "data\\";
        private static readonly string _UsersDirectory = AppDomain.CurrentDomain.BaseDirectory + "data\\users\\";
        private readonly string _CurrentUserDirectory;
        private readonly string _CurrentUserDataFile;
        private readonly string _CurrentUserFriendsDirectory;

        private readonly string _Username;
        private readonly string _Password;
        private readonly string _HashedUsername;

        private readonly EncryptedFileManager _encryptedFileManager;
        public User CurrentUser { get; private set; }

        public List<Friend> Friends { get; private set; }
        public List<string> Blocklist = new List<string>();
        public UserManager(string username, string password)
        {
            _Username = username;
            _Password = password;

            _encryptedFileManager = new EncryptedFileManager(password, _DataDirectory);
            _HashedUsername = CalculateHash(username);
            _CurrentUserDirectory = _UsersDirectory + _HashedUsername + "\\";
            _CurrentUserDataFile = _CurrentUserDirectory + "data.";
            _CurrentUserFriendsDirectory = _CurrentUserDirectory + "friends\\";

            Friends = new List<Friend>();
        }

        public UserAuthStatus AuthenticateUser()
        {
            if (Directory.Exists(_CurrentUserDirectory))
            {
                if (File.Exists(_CurrentUserDataFile))
                {
                    if (_encryptedFileManager.AuthenticateFile(_CurrentUserDataFile))
                    {
                        Dictionary<string, string> DataFileContent = ReadAllDataFile(_CurrentUserDataFile);
                        if (DataFileContent["passwordhash"] == CalculateHash(_Password))
                        {
                            return UserAuthStatus.Success;
                        }
                        else
                        {
                            return UserAuthStatus.BadHash;
                        }
                    }
                    else
                    {
                        return UserAuthStatus.IncorrectPassword;
                    }
                }
                else
                {
                    return UserAuthStatus.MissingData;
                }
            }
            else
            {
                return UserAuthStatus.InvalidUser;
            }
        }

        public bool UserExists()
        {
            if (Directory.Exists(_CurrentUserDirectory))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public User CreateUser(string PrivateKey, string PublicKey)
        {
            if (UserExists())
            {
                throw new Exception("User already exists");
            }

            Directory.CreateDirectory(_CurrentUserDirectory);
            Directory.CreateDirectory(_CurrentUserFriendsDirectory);

            Dictionary<string, string> UserData = new Dictionary<string, string>
            {
                { "name", _Username },
                { "passwordhash", CalculateHash(_Password) },
                { "privatekey",  PrivateKey},
                { "publickey",  PublicKey}
            };

            BitmapSource image = GenerateBlackImage(64, 64);

            _encryptedFileManager.WriteEncryptedData(_CurrentUserDirectory + "Avatar.", ImageUtils.ImageToMemoryStream(image).ToArray());
            File.Create(_CurrentUserDirectory + "Blocklist.");
            WriteKeyValuePairs(_CurrentUserDataFile, UserData);
            return new User(_Username, PrivateKey, PublicKey, image);

        }

        public User GetUser()
        {
            if (CurrentUser == null)
            {
                Directory.CreateDirectory(_CurrentUserFriendsDirectory);
                Dictionary<string, string> KeyValuePairs = ReadAllDataFile(_CurrentUserDataFile);

                string BlocklistPath = _CurrentUserDirectory + "Blocklist.";
                if (new FileInfo(BlocklistPath).Length > 0)
                {
                    Blocklist = new List<string>(_encryptedFileManager.GetFileString(BlocklistPath).Split(Environment.NewLine));
                }

                CurrentUser = new User(KeyValuePairs["name"], KeyValuePairs["privatekey"], KeyValuePairs["publickey"], GetCurrentUserAvatar());
            }


            return CurrentUser;
        }

        public Friend CreateFriend(string FriendName, string PublicKey, BitmapSource Avatar, I2PSharp.PeerConnection FriendPeerConnection = null)
        {
            string FriendFolderName = Path.GetRandomFileName();

            Dictionary<string, string> FriendData = new Dictionary<string, string>
            {
                { "name", FriendName },
                { "publickey", PublicKey },
                { "haslatestavatar", "true" }
            };

            string FriendDirectory = _CurrentUserFriendsDirectory + FriendFolderName + "\\";
            Directory.CreateDirectory(FriendDirectory);
            WriteKeyValuePairs(FriendDirectory + "data", FriendData);
            _encryptedFileManager.WriteEncryptedData(FriendDirectory + "Avatar.", ImageUtils.ImageToMemoryStream(Avatar).ToArray());
            return new Friend(FriendDirectory, FriendPeerConnection, FriendName, PublicKey, FriendStatus.ONLINE, Avatar, false, true);
        }

        public void UpdateFriend(Friend friend)
        {
            Dictionary<string, string> FriendData = new Dictionary<string, string>
            {
                { "name", friend.Name },
                { "publickey", friend.PublicKey },
                { "haslatestavatar", friend.HasLatestAvatar.ToString() }
            };

            WriteKeyValuePairs(friend.Path + "data", FriendData);

            _encryptedFileManager.WriteEncryptedData(friend.Path + "Avatar.", ImageUtils.ImageToMemoryStream((BitmapSource)friend.Avatar).ToArray());
        }

        public void UpdateBlocklist(string PublicKey = null)
        {
            if (PublicKey != null)
            {
                Blocklist.Add(PublicKey);
            }
            _encryptedFileManager.WriteEncryptedData(_CurrentUserDirectory + "Blocklist.", string.Join(Environment.NewLine, Blocklist));
        }
        public BitmapSource GetCurrentUserAvatar()
        {
            string AvatarFile = _CurrentUserDirectory + "Avatar.";
            if (File.Exists(AvatarFile))
            {
                return (BitmapSource)new ImageSourceConverter().ConvertFrom(_encryptedFileManager.GetFile(AvatarFile));
            }
            else
            {
                return GenerateBlackImage(64, 64);
            }
        }

        private BitmapSource GenerateBlackImage(int Width, int Height)
        {
            int BytesPerPixel = 3;
            int stride = 4 * (((Width * BytesPerPixel) + 3) / 4);
            return BitmapSource.Create(Width, Height, 72, 72, PixelFormats.Bgr24, BitmapPalettes.WebPalette, new byte[Height * stride], stride);
        }
        public List<Friend> GetAllFriends()
        {
            if (Friends.Count == 0)
            {
                string[] FriendFolders = Directory.GetDirectories(_CurrentUserFriendsDirectory);
                foreach (string FriendFolder in FriendFolders)
                {
                    Dictionary<string, string> FriendData = ReadAllDataFile(FriendFolder + "\\" + "Data.");
                    Friends.Add(new Friend(FriendFolder + "\\", null, FriendData["name"], FriendData["publickey"], FriendStatus.UNKNOWN, ImageUtils.MemoryStreamToImage(new MemoryStream(_encryptedFileManager.GetFile(FriendFolder + "\\" + "Avatar."))), false, Convert.ToBoolean(FriendData["haslatestavatar"])));
                }
            }

            return Friends;
        }
        public Friend GetFriendByPublicKey(string PublicKey)
        {
            foreach (Friend f in Friends)
            {
                if (f.PublicKey == PublicKey)
                {
                    return f;
                }
            }
            return null;
        }
        public BitmapSource ChangeAvatar(string AvatarPath)
        {
            BitmapSource image = new BitmapImage(new Uri(AvatarPath));
            CurrentUser.Avatar = image;
            _encryptedFileManager.WriteEncryptedData(_CurrentUserDirectory + "Avatar.", File.ReadAllBytes(AvatarPath));

            return image;
        }
#nullable enable
        public static string? GetUserDirectoryFromUsername(string Username)
        {
            return _UsersDirectory + CalculateHash(Username) + "\\";
        }

        private static string CalculateHash(string data)
        {
            using System.Security.Cryptography.HMAC? hmac = SuiteB.HmacFactory(); // HMACSHA384
            return hmac.ComputeHash(data.ToBytes()).ToBase16();
        }

        private Dictionary<string, string> ReadAllDataFile(string DataFile) // Key:Value format
        {
            Dictionary<string, string> Result = new Dictionary<string, string>();
            string FileData = _encryptedFileManager.GetFileString(DataFile);
            foreach (string Line in FileData.Split(Environment.NewLine))
            {
                try
                {
                    string[] KeyValuePair = Line.Split(':');
                    Result.Add(KeyValuePair[0], KeyValuePair[1]);
                }
                catch (IndexOutOfRangeException)
                {
                    // there's an extra newline present, ignore
                }
            }

            return Result;
        }
        private void WriteKeyValuePairs(string DataFile, Dictionary<string, string> KeyValuePairs)
        {
            string FileData = string.Empty;
            foreach (KeyValuePair<string, string> kvps in KeyValuePairs)
            {
                FileData += string.Format("{0}:{1}{2}", kvps.Key, kvps.Value, Environment.NewLine);
            }
            _encryptedFileManager.WriteEncryptedData(DataFile, FileData);
        }

    }
}
