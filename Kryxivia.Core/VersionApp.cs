using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Core
{
    public class FileCheckSum : IEquatable<FileCheckSum>
    {
        public string Checksum { get; set; } = "";
        public string FilePath { get; set; } = "";

        public bool Equals(FileCheckSum obj) => Checksum == obj.Checksum;
        public override int GetHashCode()
        {
            int hash = 19;
            hash *=  23 + Checksum.GetHashCode();
            return hash;
        }
    }

    public class VersionApp
    {
        public List<string> Versions { get; set; } = new List<string>();
        public List<string> NotYetUploaded { get; set; } = new List<string>();
        public List<FileCheckSum> FilesChecksum { get; set; } = new List<FileCheckSum>();
        public string DownloadFolder { get; set; } = Directory.GetCurrentDirectory();
        public string TmpFolder { get; set; } = Directory.GetCurrentDirectory();
    }
}
