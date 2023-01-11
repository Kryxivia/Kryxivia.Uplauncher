using Ionic.Zip;
using KryxiviaUpdater.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace KryxiviaAutoUpdate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window
    {
        private string _updaterLocalVersion, _updaterServerVersion;
        private const string _urlServer = "https://cdn.kryxivia.io/";

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            Task.Run(async () =>
            {
                await UpdateProgressBar(10);
                await Task.Delay(1000);
                await UpdateProgressBar(30);
                await setup();
                if (_updaterLocalVersion != _updaterServerVersion)
                {
                    await DownloadUpdater();
                    File.WriteAllText("updaterVersion", _updaterServerVersion);
                }

                await UpdateProgressBar(100);
                await Task.Delay(1000);
                Process _process = new Process();
                _process.StartInfo.FileName = "KryxiviaUpdater.exe";
                _process.StartInfo.Verb = "runas";
                _process.Start();

                Environment.Exit(0);

            });
        }

        private async Task UpdateProgressBar(int value)
        {
            await Dispatcher.BeginInvoke(new Action(() =>
            {
                progressbar.Value = value;

            }), DispatcherPriority.Render);
        }
        public async Task setup()
        {
            if (!File.Exists("updaterVersion")) File.WriteAllText("updaterVersion", "1.0.0");
            _updaterLocalVersion = File.ReadAllText("updaterVersion");
            _updaterServerVersion = await GetServerUpdaterApp();
        }

        private async Task<string> GetServerUpdaterApp()
        {
            var versionAppContents = $"{_urlServer}updater/version";
            var result = "";
            using (HttpClient client = new HttpClient())
            {
                result = await client.GetStringAsync(versionAppContents);
            }

            return result;
        }

        private async Task DownloadUpdater()
        {
            var downloadFileUrl = $"{_urlServer}updater/{_updaterServerVersion}.zip";
            string destinationFilePath = $"{_updaterServerVersion}.zip";

            using (var client = new HttpClientDownloadWithProgress(downloadFileUrl, destinationFilePath))
            {
                client.ProgressChanged += (totalFileSize, totalBytesDownloaded, progressPercentage, speed) =>
                {
                };

                await client.StartDownload();
                UnzipArchive(destinationFilePath);
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }
            catch (Exception ex)
            {
                // LOG ERROR HERE
            }
        }

        private void UnzipArchive(string pathArchive)
        {
            using (var zip = Ionic.Zip.ZipFile.Read(pathArchive))
            {
                zip.ExtractAll("./"
                              , ExtractExistingFileAction.OverwriteSilently);
            }
            File.Delete(pathArchive);
        }

    }
}
