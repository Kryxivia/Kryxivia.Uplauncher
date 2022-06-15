using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Core
{
    public class FileCheckSum : IEquatable<FileCheckSum>
    {
        public string Checksum { get; set; } = "";
        public string FilePath { get; set; } = "";

        public bool Equals(FileCheckSum obj) => Checksum == obj.Checksum && FilePath == obj.FilePath;
        public override int GetHashCode()
        {
            int hash = 19;
            hash *=  23 + Checksum.GetHashCode();
            hash *=  23 + FilePath.GetHashCode();
            return hash;
        }
    }

    public class VersionApp
    {
        public List<string> Versions { get; set; } = new List<string>();
        public List<string> NotYetUploaded { get; set; } = new List<string>();
        public List<FileCheckSum> FilesChecksum { get; set; } = new List<FileCheckSum>();
        public string Path { get; set; } = Directory.GetCurrentDirectory();
    }
}
