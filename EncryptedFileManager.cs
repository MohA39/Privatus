using SecurityDriven.Inferno;
using SecurityDriven.Inferno.Extensions;
using System;
using System.Collections.Generic;
using System.IO;

namespace Privatus
{
    internal class EncryptedFileManager
    {
        private readonly string _Password;
        private readonly string _WorkingDirectory;
        private readonly Dictionary<string, byte[]> _DecryptedFiles = new Dictionary<string, byte[]>();

        public EncryptedFileManager(string password, string directory)
        {
            _Password = password;
            _WorkingDirectory = directory;
        }

        public bool AuthenticateFile(string directory)
        {
            return SuiteB.Authenticate(_Password.ToBytes(), new ArraySegment<byte>(File.ReadAllBytes(GetFullDirectory(directory.ToLower()))));
        }
        public byte[] GetFile(string directory)
        {
            string FileLocation = GetFullDirectory(directory.ToLower());

            if (!File.Exists(FileLocation))
            {
                throw new Exception("File does not exist: " + FileLocation);
            }
            // Data is already decrypted
            if (_DecryptedFiles.TryGetValue(FileLocation, out byte[] data))
            {
                return data;
            }
            else
            {
                byte[] FileBytes = File.ReadAllBytes(FileLocation);
                if (SuiteB.Authenticate(_Password.ToBytes(), new ArraySegment<byte>(FileBytes)))
                {
                    data = SuiteB.Decrypt(_Password.ToBytes(), new ArraySegment<byte>(FileBytes));
                    _DecryptedFiles.Add(FileLocation, data);
                    return data;
                }
                else
                {
                    throw new Exception("Invalid password");
                }

            }

        }

        public string GetFileString(string directory)
        {
            return Utils.SafeUTF8.GetString(GetFile(directory)).Replace("\0", "");
        }
        public void WriteEncryptedData(string directory, string Data)
        {
            File.WriteAllBytes(GetFullDirectory(directory), SuiteB.Encrypt(_Password.ToBytes(), new ArraySegment<byte>(Data.ToBytes())));
        }
        public void WriteEncryptedData(string directory, byte[] Data)
        {
            File.WriteAllBytes(GetFullDirectory(directory), SuiteB.Encrypt(_Password.ToBytes(), new ArraySegment<byte>(Data)));
        }
        private string GetFullDirectory(string directory)
        {
            string drive = Path.GetPathRoot(directory);

            if (Directory.Exists(drive))
            {
                return directory;
            }
            else
            {
                return _WorkingDirectory + directory;
            }
        }
    }
}
