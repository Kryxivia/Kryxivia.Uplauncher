using System;
using System.Collections.Generic;
using System.IO;
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

        public Tuple<int,long> CompressAndZip()
        {
            var step = 0;
            double totalLength = 0;
            int i = 0;
            long size = 0;
            while (i < _filesChecksum.Count)
            {
                using (ZipArchive zipFile = ZipFile.Open($"{_pathZip}/{step}.zip", ZipArchiveMode.Create))
                {
                    while(i < _filesChecksum.Count && totalLength <= 300)
                    {
                        totalLength += (new FileInfo(_filesChecksum[i].FilePath).Length) / (1024d * 1024d);
                        zipFile.CreateEntryFromFile(_filesChecksum[i].FilePath,
                            _filesChecksum[i].FilePath, CompressionLevel.Optimal);
                        i++;
                    }
                    size += (long)((new FileInfo($"{_pathZip}/{step}.zip").Length) / (1024d * 1024d));
                    totalLength = 0;
                    step++;
                }
            }

            return Tuple.Create(step, size);

        }
    }
}
