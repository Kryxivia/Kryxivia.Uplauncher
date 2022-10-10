using Ionic.Zip;
using KryxiviaUpdater.Core;
using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KryxiviaUpdater.Updater
{
    public class Updater
    {
        private string _pathVersion;
        private VersionApp _clientVersionApp, _serverVersionApp;
        private const string _urlServer = "https://cdn.kryxivia.io/";
        private Action<int, int, string> _updateVersionProgress;
        private Action<int, string> _updatePercentProgress;
        private Action<NewsList> _setNewsList;
        private Action _unzipFileLog;
        private string _kryxiviaFolder;
        private ILog _log;

        public IEnumerable<FileCheckSum> FilesToDownload { get; set; }
        public List<string> VersionsToDownload { get;set; }
        public string DownloadFolder
        {
            get
            {
                return _clientVersionApp.DownloadFolder;
            }
            set
            {
                _clientVersionApp.DownloadFolder = value;
            }
        }
        public string TmpFolder
        {
            get
            {
                return _clientVersionApp.TmpFolder;
            }
            set
            {
                _clientVersionApp.TmpFolder = value;
            }
        }

        public bool AutomaticUpdate
        {
            get
            {
                return _clientVersionApp.AutomaticUpdate;
            }
            set
            {
                _clientVersionApp.AutomaticUpdate = value;
            }
        }

        public Updater(ILog log, string kryxiviaFolder, string pathVersion,
            Action<int, int, string> updateVersionProgress, Action<int, string> updatePourcentProgress,
            Action<NewsList> setNewsList, Action unzipFileLog)
        {
            _log = log;
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
            _log.Info("STARTING SETUP");
            _setNewsList(await GetServerNews());
            _log.Info("SET NEWS LIST");
            return await CheckUpdate();

        }

        public async Task<UpdaterState> CheckUpdate()
        {
            if (!File.Exists(_pathVersion)) File.WriteAllText(_pathVersion, JsonConvert.SerializeObject(new VersionApp()));
            _clientVersionApp = JsonConvert.DeserializeObject<VersionApp>(File.ReadAllText(_pathVersion));
            _serverVersionApp = await GetServerVersionApp();
            if (_clientVersionApp != null & _serverVersionApp != null)
            {
                if (_serverVersionApp?.Versions.Count != _clientVersionApp?.Versions.Count)
                {
                    // first time we download directly base
                    if (_clientVersionApp.Versions.Count == 0)
                    {
                        VersionsToDownload = new List<string> { "0.0.0" };
                        _log.Info("FIRST TIME INSTALLING UPDATER");
                    }
                    else
                    {
                        VersionsToDownload = _serverVersionApp?.Versions.Except(_clientVersionApp?.Versions).ToList();
                        _log.Info($"INSTALL {VersionsToDownload.Count} VERSIONS");
                    }
                    return UpdaterState.Downloading;
                }
            }

            _log.Info("ALL GOOD WE CAN CONNECT !");
            return UpdaterState.Connecting;
        }

        public async Task<UpdaterState> RepairClients()
        {
            var currentFilesChecksum = new List<FileCheckSum>();
            foreach (var file in Directory.GetFiles(
                Path.Combine(new string[] {DownloadFolder, _kryxiviaFolder }), "*.*", SearchOption.AllDirectories))
            {
                currentFilesChecksum.Add(new FileCheckSum
                {
                    Checksum = Utils.GenerateChecksum(file),
                    FilePath = file.Replace(DownloadFolder + Path.DirectorySeparatorChar, ""),
                }); ;
            }

            var files = _serverVersionApp.FilesChecksum.Except(currentFilesChecksum).ToList();
            int count = 1;
            foreach (var file in files)
            {
                var downloadFileUrl = $"{_urlServer}{file.FilePath}";
                var destinationFilePath = Path.GetFullPath(file.FilePath);

                var directoryPath = Path.GetDirectoryName(destinationFilePath);
                Directory.CreateDirectory(directoryPath);
                var speedCache = "";
                using (var client = new HttpClientDownloadWithProgress(downloadFileUrl, destinationFilePath))
                {
                    client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage, speed) =>
                    {
                        _updateVersionProgress(count, files.Count(), speedCache);
                        _updatePercentProgress((int)progressPercentage, progressPercentage+"");
                    };

                    client.SpeedChanged += (speed) =>
                    {
                        speedCache = speed;
                        _updateVersionProgress(count, files.Count(), speed);
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
            _log.Info("START DOWNLOADING");
            int count = 0;
            _log.Info("GETTING THE FILES SIZE");
            var size = await GetFilesSize(VersionsToDownload.Select(x => $"{_urlServer}{x}/size").ToList());
            foreach (var version in VersionsToDownload)
            {
                count++;
                var filesVersion = await GetZipVersion($"{_urlServer}{version}");
                for (int i = 0; i < filesVersion; i++)
                {
                    var downloadFileUrl = $"{_urlServer}{version}/{i}.zip";
                    _log.Info($"START DOWNLOADING {downloadFileUrl}");
                    string destinationFilePath = System.IO.Path.Combine(new string[] {
                    TmpFolder,
                    $"{i}.zip" });

                    double speedCache = 0;

                    using (var client = new HttpClientDownloadWithProgress(downloadFileUrl, destinationFilePath))
                    {
                        client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage, speed) =>
                        {
                            size -= (long)(totalBytesDownloaded / (1024 * 1024));
                            _updateVersionProgress(i + 1, filesVersion, $" - {speedCache} MB/s");
                            if(speedCache == 0)
                            {
                                _updatePercentProgress((int)progressPercentage, $" {(int)progressPercentage}");
                            }
                            else
                            {
                                var time = (int)((size / speedCache) / 60);
                                var remaining = time <= 0 ? 1 : time;
                                _updatePercentProgress((int)progressPercentage, $" Time remaining { remaining } minutes - {(int)progressPercentage}");
                            }

                        };

                        client.SpeedChanged += (speed) =>
                        {
                            speedCache = double.Parse(speed);
                            _updateVersionProgress(i + 1, filesVersion, $" - {speed} MB/s");
                        };

                        await client.StartDownload();
                        _updateVersionProgress(i + 1, filesVersion, "");
                        UnzipArchive(destinationFilePath);
                    }
                }

                _clientVersionApp?.Versions.Add(version);
                WriteProgressDownload();

            }

            WriteServerChecksumFile();
            _log.Info("ALL VERSIONS DOWNLOADED !");
        }

        private async Task<long> GetFilesSize(List<string> files)
        {
            long size = 0;
            for(int i = 0; i < files.Count; i++)
            {
                var content = files[0];
                using (HttpClient client = new HttpClient())
                {
                    size += long.Parse(await client.GetStringAsync(content));
                }
            }

            return size;
        }
        private void UnzipArchive(string pathArchive)
        {
            _unzipFileLog();
            using (var zip = Ionic.Zip.ZipFile.Read(pathArchive))
            {
                zip.ExtractAll(DownloadFolder
                              , ExtractExistingFileAction.OverwriteSilently);
            }
            File.Delete(pathArchive);
        }

        public void WriteProgressDownload()
        {
            File.WriteAllText(_pathVersion, JsonConvert.SerializeObject(_clientVersionApp));
        }

        public void WriteServerChecksumFile()
        {
            _serverVersionApp.TmpFolder = _clientVersionApp.TmpFolder;
            _serverVersionApp.DownloadFolder = _clientVersionApp.DownloadFolder;
            File.WriteAllText(_pathVersion, JsonConvert.SerializeObject(_serverVersionApp));
        }

        private async Task<VersionApp> GetServerVersionApp()
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


        private async Task<NewsList> GetServerNews()
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
