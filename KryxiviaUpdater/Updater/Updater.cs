using KryxiviaUpdater.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Updater
{
    public class Updater
    {
        private string _pathVersion;
        private VersionApp? _clientVersionApp, _serverVersionApp;
        private const string _urlServer = "http://144.91.65.207/";
        private Action<int, int> _updateVersionProgress;
        private Action<int> _updatePercentProgress;
        private Action<NewsList?> _setNewsList;
        private Action _unzipFileLog;
        private string _kryxiviaFolder;
        public IEnumerable<FileCheckSum> FilesToDownload { get; set; }
        public List<string> VersionsToDownload { get;set; }
        public string CurrentPath
        {
            get
            {
                return _clientVersionApp.Path;
            }
            set
            {
                _clientVersionApp.Path = value;
            }
        }
        public Updater(string kryxiviaFolder, string pathVersion,
            Action<int, int> updateVersionProgress, Action<int> updatePourcentProgress,
            Action<NewsList?> setNewsList, Action unzipFileLog)
        {
            _pathVersion = pathVersion;
            _clientVersionApp = null;
            _serverVersionApp = null;
            _updateVersionProgress = updateVersionProgress;
            _updatePercentProgress = updatePourcentProgress;
            _setNewsList = setNewsList;
            _unzipFileLog = unzipFileLog;
            _kryxiviaFolder = kryxiviaFolder;
        }

        public async Task<UpdaterState> Setup()
        {
            _setNewsList(await GetServerNews());
            if (!File.Exists(_pathVersion)) File.WriteAllText(_pathVersion, JsonConvert.SerializeObject(new VersionApp()));
            _clientVersionApp = JsonConvert.DeserializeObject<VersionApp>(File.ReadAllText(_pathVersion));
            _serverVersionApp = await GetServerVersionApp();
            if(_clientVersionApp != null & _serverVersionApp != null)
            {
                if(_serverVersionApp?.Versions.Count != _clientVersionApp?.Versions.Count)
                {
                    // first time we download directly base
                    if(_clientVersionApp.Versions.Count == 0)
                    {
                        VersionsToDownload = new List<string> { "0.0.0" };
                    }
                    else
                    {
                        VersionsToDownload = _serverVersionApp?.Versions.Except(_clientVersionApp?.Versions).ToList();
                    }
                    return UpdaterState.Downloading;
                }
            }

            return UpdaterState.Connecting;
        }

        public async Task<UpdaterState> RepairClients()
        {
            var currentFilesChecksum = new List<FileCheckSum>();
            foreach (var file in Directory.GetFiles(_kryxiviaFolder, "*.*", SearchOption.AllDirectories))
            {
                currentFilesChecksum.Add(new FileCheckSum
                {
                    Checksum = Utils.GenerateChecksum(file),
                    FilePath = file,
                });
            }

            var files = _serverVersionApp.FilesChecksum.Except(currentFilesChecksum).ToList();
            int count = 1;
            foreach (var file in files)
            {
                var downloadFileUrl = $"{_urlServer}{file.FilePath}";
                var destinationFilePath = Path.GetFullPath(file.FilePath);

                    var directoryPath = Path.GetDirectoryName(destinationFilePath);
                    Directory.CreateDirectory(directoryPath);

                using (var client = new HttpClientDownloadWithProgress(downloadFileUrl, destinationFilePath))
                {
                    client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                        _updateVersionProgress(count, files.Count());
                        _updatePercentProgress((int)progressPercentage);
                    };

                    await client.StartDownload();
                    count++;
                }
            }

            WriteServerChecksumFile();
            return UpdaterState.Playing;
        }

        public async Task StartDownload()
        {
            int count = 0;
            foreach(var version in VersionsToDownload)
            {
                count++;
                var filesVersion = await GetZipVersion($"{_urlServer}{version}");
                for(int i = 0;i < filesVersion; i++)
                {
                    var downloadFileUrl = $"{_urlServer}{version}/{i}.zip";
                    string destinationFilePath = System.IO.Path.Combine(new string[] {
                    System.IO.Path.GetTempPath(),
                    $"{i}.zip" });


                    using (var client = new HttpClientDownloadWithProgress(downloadFileUrl, destinationFilePath))
                    {
                        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage) => {
                            _updateVersionProgress(i + 1, filesVersion);
                            _updatePercentProgress((int)progressPercentage);
                        };

                        await client.StartDownload();
                        UnzipArchive(destinationFilePath);
                    }
                }

                _clientVersionApp?.Versions.Add(version);
                WriteProgressDownload();

            }

            WriteServerChecksumFile();
        }

        private void UnzipArchive(string pathArchive)
        {
            _unzipFileLog();
            ZipFile.ExtractToDirectory(pathArchive, Directory.GetCurrentDirectory(), true);
        }

        public void WriteProgressDownload()
        {
            File.WriteAllText(_pathVersion, JsonConvert.SerializeObject(_clientVersionApp));
        }

        public void WriteServerChecksumFile()
        {
            File.WriteAllText(_pathVersion, JsonConvert.SerializeObject(_serverVersionApp));
        }


        private async Task<VersionApp?> GetServerVersionApp()
        {
            var versionAppContents = $"{_urlServer}/versionApp.json";
            var result = "";
            using (HttpClient client = new HttpClient())
            {
                result =  await client.GetStringAsync(versionAppContents);
            }

            return JsonConvert.DeserializeObject<VersionApp>(result);
        }

        private async Task<int> GetZipVersion(string urlVersion)
        {
            var versionAppContents = $"{urlVersion}/files";
            var result = "";
            using (HttpClient client = new HttpClient())
            {
                result = await client.GetStringAsync(versionAppContents);
            }

            return int.Parse(result);
        }


        private async Task<NewsList?> GetServerNews()
        {
            var newsContents = $"{_urlServer}/news.json";
            var result = "";
            using (HttpClient client = new HttpClient())
            {
                result = await client.GetStringAsync(newsContents);
            }
            return JsonConvert.DeserializeObject<NewsList>(result);
        }
    }
}
