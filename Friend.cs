using I2PSharp;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace Privatus
{
    public enum FriendStatus
    {
        ONLINE,
        OFFLINE,
        BLOCKED,
        UNKNOWN
    }

    public class Friend
    {
        public PeerConnection FriendConnectedSession { get; set; }
        public FriendStatus Status { get; set; }

        public string Name { get; private set; }
        public string PublicKey { get; private set; }

        public ImageSource Avatar { get; set; }

        public StackPanel ConversationContainer { get; set; }
        public FriendBlock FriendBlock { get; set; }
        public bool IsAddedToFriendList { get; set; }

        public bool HasLatestAvatar { get; set; }

        public string Path { get; private set; }

        public DateTime LastDisconnect = new DateTime();
        public Friend(string path, PeerConnection friendConnectedSession, string name, string publicKey, FriendStatus status, ImageSource avatar, bool IsInFriendList = false, bool hasLatestAvatar = false)
        {
            Path = path;
            FriendConnectedSession = friendConnectedSession;
            Name = name;
            PublicKey = publicKey;
            Status = status;
            Avatar = avatar;
            IsAddedToFriendList = IsInFriendList;
            HasLatestAvatar = hasLatestAvatar;
        }
        public void HandleFriendDisconnect()
        {
            Status = FriendStatus.OFFLINE;
            FriendBlock.UpdateStatusLabel(Status);
            FriendConnectedSession = null;
        }

    }
}
