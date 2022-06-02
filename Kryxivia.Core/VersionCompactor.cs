using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Core
{
    public class VersionCompactor
    {
        private List<FileCheckSum> _filesChecksum;
		private string _pathZip;
        public VersionCompactor(string pathZip, List<FileCheckSum> filesChecksum)
        {
            _filesChecksum = filesChecksum;
			_pathZip = pathZip;

		}

        public void CompressAndZip()
        {
            
			using (ZipArchive zipFile = ZipFile.Open($"{_pathZip}/{_pathZip}.zip", ZipArchiveMode.Create))
			{
				foreach(var file in _filesChecksum)
                {
                    zipFile.CreateEntryFromFile(file.FilePath, file.FilePath, CompressionLevel.Optimal);  
                }

            }
		}
    }
}
