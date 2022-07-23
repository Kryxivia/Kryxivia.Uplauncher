using KryxiviaUpdater.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Linq;
using System.Timers;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace KryxiviaUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KryxiviaUpdater.Updater.Updater _updater;
        private KryxiviaUpdater.Updater.KryxiviaAPI _kryxiviaAPI;
        private int currentNewsIndex = 0;
        private Dictionary<int, Image> _imagesNews;
        private NewsList _newsList;
        private const string _linkWebsite = "https://kryxivia.io/";
        private const string _linkBlog = "https://kryxivia.io/en/devblog";
        private const string _linkHelp = "https://docs.kryxivia.io/";
        private const int _maxNews = 3;
        private string _pathKryxivia = $"client{System.IO.Path.DirectorySeparatorChar}Kryxivia.exe";
        private Timer _timer;
        private UpdaterState _updaterState;
        private Process _process;

        private int _timerToAutomaticUpdate = 10; // in minutes

        private const string _website = "https://kryxivia.io/";
        private const string _instagram = "https://www.instagram.com/kryxivia/";
        private const string _twitter = "https://twitter.com/kryxivia";
        private const string _telegram = "https://t.me/kryxivia";
        private const string _discord = "https://discord.com/invite/eH4fPm66qZ";
        private const string _youtube = "https://www.youtube.com/channel/UCZV1N-ajeO16504yw4RFR7A";
        private const string _trailer = "https://www.youtube.com/watch?v=KSp3Upu2U4c";
        private static System.Threading.Mutex mutex;


        public MainWindow()
        {
            bool createdNew;
            mutex = new System.Threading.Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}", out createdNew);
            if (!createdNew)
            {
                Environment.Exit(0);
            }

            InitializeComponent();
            Initialize();
            _process = null;
            _timer = new Timer();
            _timer.Elapsed += new ElapsedEventHandler(async (o, e) =>
            {
                if( _process == null && (_updaterState == UpdaterState.Playing || _updaterState == UpdaterState.Connecting))
                {
                    _updaterState = await _updater.Setup();
                    if (_updaterState == Core.UpdaterState.Downloading)
                    {
                        UpdateState(_updaterState);
                        await _updater.StartDownload();
                    }
                    _updaterState = await _kryxiviaAPI.Setup();
                    UpdateState(_updaterState);
                }
            });
            _timer.Interval = TimeSpan.FromMinutes(_timerToAutomaticUpdate).TotalMilliseconds;
            _timer.Enabled = true;
            _timer.Stop();

            _updater = new Updater.Updater("client", "versionApp.json", UpdateVersionProgress, UpdatePourcentProgress, SetNewsList, UnzipFileLog);
            _kryxiviaAPI = new Updater.KryxiviaAPI("https://kryx-app-auth-api.azurewebsites.net/", "https://auth-app.kryxivia.io/"
                , UpdateState, UpdateAddress);
            UpdateAddress("");
            Task.Run(async () =>
            {
                _updaterState =  await _updater.Setup();
                if (_updaterState == Core.UpdaterState.Downloading)
                {
                    UpdateState(_updaterState);
                   await _updater.StartDownload();
                }
                _updaterState = await _kryxiviaAPI.Setup();
                UpdateState(_updaterState);
                RefreshAutomaticUpdate();
            });

        }


        public void Initialize()
        {
            l_pourcent.Text = "";
            l_version.Text = "";
            t_news.Text = "";
            t_news_date.Text  = "";
            progress.Value = 100;
            currentNewsIndex = 0;
            l_play.Cursor = Cursors.No;
            CloseSettings();
        }
        
        public void OpenSettings()
        {
            p_settings.Visibility = Visibility.Visible;
            settings_bg.Visibility = Visibility.Visible;
            settings_bg_black.Visibility = Visibility.Visible;
            settings_close.Visibility = Visibility.Visible;
            repear.Visibility = Visibility.Visible;
            folder_file.Visibility = Visibility.Visible;
            folder_tmp.Visibility = Visibility.Visible;
            t_folder_file.Visibility = Visibility.Visible;
            t_folder_tmp.Visibility = Visibility.Visible;
            tmp_location.Visibility = Visibility.Visible;
            download_location.Visibility= Visibility.Visible;
            explorer.Visibility = Visibility.Visible;
            l_automatic_updater.Visibility = Visibility.Visible;
            b_automatic_updater.Visibility=Visibility.Visible;
            t_folder_file.Text = _updater.DownloadFolder;
            t_folder_tmp.Text = _updater.TmpFolder;
            RefreshAutomaticUpdate();
        }

        public void CloseSettings()
        {
            p_settings.Visibility = Visibility.Hidden;
            settings_bg.Visibility = Visibility.Hidden;
            settings_bg_black.Visibility = Visibility.Hidden;
            settings_close.Visibility = Visibility.Hidden;
            repear.Visibility = Visibility.Hidden;
            folder_file.Visibility = Visibility.Hidden;
            folder_tmp.Visibility = Visibility.Hidden;
            t_folder_file.Visibility = Visibility.Hidden;
            t_folder_tmp.Visibility = Visibility.Hidden;
            tmp_location.Visibility = Visibility.Hidden;
            download_location.Visibility = Visibility.Hidden;
            explorer.Visibility = Visibility.Hidden;
            l_automatic_updater.Visibility = Visibility.Hidden;
            b_automatic_updater.Visibility = Visibility.Hidden;
        }

        private void RefreshAutomaticUpdate()
        {
            if (_updater.AutomaticUpdate)
            {
                b_automatic_updater.Source = new BitmapImage(new Uri("Resources/Images/automatic_update_activate.png", UriKind.Relative));
                _timer.Start();
            }
            else
            {
                b_automatic_updater.Source = new BitmapImage(new Uri("Resources/Images/automatic_update.png", UriKind.Relative));
                _timer.Stop();
            }
        }

        public void UpdateVersionProgress(int start, int count, string speed)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_version.Text = $"{start}/{count} {speed}";

            }), DispatcherPriority.Render);
        }

        public void UpdatePourcentProgress(int pourcent, string msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_pourcent.Text = $"{msg}%";
                progress.Value = pourcent;

            }), DispatcherPriority.Render);
        }

        public void UnzipFileLog()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_pourcent.Text = $"Unzip file in progress...";
                progress.Value = 100;

            }), DispatcherPriority.Background);
        }

        public void SetNewsList(NewsList newsList)
        {
            _newsList = newsList;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if(_newsList != null && _newsList?.News.Count > 0)
                {
                    t_news.Text = _newsList.News[0].Contents;
                    t_news.Text += "\n";
                    t_news_date.Text = _newsList.News[0].Date;

                }

            }), DispatcherPriority.Background);
        }


        public void UpdateAddress(string address)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_address.Text = address;
            }));
        }
        public void UpdateState(UpdaterState state)
        {
            _updaterState = state;
            RefreshUI();
        }
        private void RefreshUI()
        {
            if(_updaterState == UpdaterState.Playing)
            {
                if (_process == null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        l_pourcent.Text = $"100%";
                        l_version.Text = "";
                        progress.Value = 100;
                        l_play.Cursor = Cursors.Hand;
                        l_play.Source = new BitmapImage(new Uri("Resources/Images/play.png", UriKind.Relative));

                    }), DispatcherPriority.Background);
                }
                else
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        l_pourcent.Text = $"100%";
                        l_version.Text = "";
                        progress.Value = 100;
                        l_play.Cursor = Cursors.No;
                        l_play.Source = new BitmapImage(new Uri("Resources/Images/play_disabled.png", UriKind.Relative));
                    }), DispatcherPriority.Background);

                }

            }
            else if(_updaterState == UpdaterState.Connecting)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Text = $"100%";
                    l_version.Text = "";
                    progress.Value = 100;
                    l_play.Cursor = Cursors.Hand;
                    l_play.Source = new BitmapImage(new Uri("Resources/Images/sign_in.png", UriKind.Relative));

                }), DispatcherPriority.Background);
            }
            else if (_updaterState == UpdaterState.Repearing)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Text = $"Repearing...";
                    l_version.Text = "";
                    progress.Value = 100;
                    l_play.Cursor = Cursors.No;
                }), DispatcherPriority.Background);
            }else if(_updaterState == UpdaterState.Downloading)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Text = $"100%";
                    l_version.Text = "";
                    progress.Value = 100;
                    l_play.Cursor = Cursors.No;
                    l_play.Source = new BitmapImage(new Uri("Resources/Images/play_disabled.png", UriKind.Relative));

                }), DispatcherPriority.Background);
            }

        }

        private void OpenLink(string link)
        {
            var process = new System.Diagnostics.ProcessStartInfo(link)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(process);
        }

        private void Window_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }catch(Exception ex)
            {
                // LOG ERROR HERE
            }
            
        }

        private void l_website_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenLink(_linkWebsite);
        }

        private void l_blog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenLink(_linkBlog);
        }

        private void l_help_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenLink(_linkHelp);
        }

        private void l_play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var clientPath = System.IO.Path.Combine(new string[]
            {
                _updater.DownloadFolder,
                _pathKryxivia
            });

            if (File.Exists(clientPath) && _updaterState == UpdaterState.Playing && _process == null)
            {
                Process _process = new Process();
                _process.StartInfo.FileName = clientPath;
                _process.StartInfo.Arguments = string.Format("token \"{0}\"", _kryxiviaAPI.jwtRaw);
                _process.EnableRaisingEvents = true;


                _process.Start();
                _process.Exited += (s, m) =>
                {
                    _process = null;
                    RefreshUI();
                };
                RefreshUI();
                WindowState = WindowState.Minimized;
            }
            else if (_updaterState == UpdaterState.Connecting)
            {
                _kryxiviaAPI.OpenWebSite();
            }
        }
        private void open_directory_download_MouseDown(object sender, MouseButtonEventArgs e)
        {

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if(result == CommonFileDialogResult.Ok)
            {
                var folder = System.IO.Path.GetDirectoryName(dialog.FileName);
                _updater.DownloadFolder = folder;
                _updater.WriteProgressDownload();
                OpenSettings();
            }
        }

        private void open_directory_tmp_MouseDown(object sender, MouseButtonEventArgs e)
        {

            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            CommonFileDialogResult result = dialog.ShowDialog();
            if (result == CommonFileDialogResult.Ok)
            {
                var folder = System.IO.Path.GetDirectoryName(dialog.FileName);
                _updater.TmpFolder = folder;
                _updater.WriteProgressDownload();
                OpenSettings();
            }
        }

        private void repear_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseSettings();
            if (_updaterState == UpdaterState.Connecting || _updaterState == UpdaterState.Playing)
            {
                UpdateState(UpdaterState.Repearing);
                var task = new Task(async () =>
                {
                    var state = await _updater.RepairClients();
                    _updaterState = await _kryxiviaAPI.Setup();
                    UpdateState(_updaterState);
                });
                task.Start();
            }
        }

        private void explorer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var prc = new System.Diagnostics.Process();
            prc.StartInfo.FileName = _updater.DownloadFolder;
            prc.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Maximized;
            prc.Start();
        }
        #region simple button event

        private void b_reduce_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void b_close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void folder_tmp_MouseEnter(object sender, MouseEventArgs e)
        {
            folder_tmp.Source = new BitmapImage(new Uri("Resources/Images/folder_over.png", UriKind.Relative));
        }
        private void l_play_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_updaterState == UpdaterState.Playing && _process == null)
            {
                l_play.Source = new BitmapImage(new Uri("Resources/Images/play_over.png", UriKind.Relative));
            }else if(_updaterState == UpdaterState.Connecting)
            {
                l_play.Source = new BitmapImage(new Uri("Resources/Images/sign_in_over.png", UriKind.Relative));
            }
        }

        private void l_play_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_updaterState == UpdaterState.Playing && _process == null)
            {
                l_play.Source = new BitmapImage(new Uri("Resources/Images/play.png", UriKind.Relative));
            }
            else if (_updaterState == UpdaterState.Connecting)
            {
                l_play.Source = new BitmapImage(new Uri("Resources/Images/sign_in.png", UriKind.Relative));
            }
        }
        private void folder_file_MouseLeave(object sender, MouseEventArgs e)
        {
            folder_file.Source = new BitmapImage(new Uri("Resources/Images/folder.png", UriKind.Relative));
        }

        private void folder_file_MouseEnter(object sender, MouseEventArgs e)
        {
            folder_file.Source = new BitmapImage(new Uri("Resources/Images/folder_over.png", UriKind.Relative));
        }

        private void folder_tmp_MouseLeave(object sender, MouseEventArgs e)
        {
            folder_tmp.Source = new BitmapImage(new Uri("Resources/Images/folder.png", UriKind.Relative));
        }
        private void b_reapear_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void b_close_MouseEnter(object sender, MouseEventArgs e)
        {
            b_close.Source = new BitmapImage(new Uri("Resources/Images/close_over.png", UriKind.Relative));
        }

        private void b_close_MouseLeave(object sender, MouseEventArgs e)
        {
            b_close.Source = new BitmapImage(new Uri("Resources/Images/close.png", UriKind.Relative));
        }

        private void b_reduce_MouseEnter(object sender, MouseEventArgs e)
        {
            b_reduce.Source = new BitmapImage(new Uri("Resources/Images/reduce_over.png", UriKind.Relative));
        }

        private void b_reduce_MouseLeave(object sender, MouseEventArgs e)
        {
            b_reduce.Source = new BitmapImage(new Uri("Resources/Images/reduce.png", UriKind.Relative));
        }

        private void settings_close_MouseEnter(object sender, MouseEventArgs e)
        {
            settings_close.Source = new BitmapImage(new Uri("Resources/Images/close_over.png", UriKind.Relative));
        }

        private void settings_close_MouseLeave(object sender, MouseEventArgs e)
        {
            settings_close.Source = new BitmapImage(new Uri("Resources/Images/close.png", UriKind.Relative));
        }

        private void settings_close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            CloseSettings();
        }

        private void b_settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            OpenSettings();
        }
        private void b_settings_MouseEnter(object sender, MouseEventArgs e)
        {
            b_settings.Source = new BitmapImage(new Uri("Resources/Images/repear_over.png", UriKind.Relative));
        }

        private void b_settings_MouseLeave(object sender, MouseEventArgs e)
        {
            b_settings.Source = new BitmapImage(new Uri("Resources/Images/repear.png", UriKind.Relative));
        }

        private void website_MouseEnter(object sender, MouseEventArgs e)
        {
            website.Source = new BitmapImage(new Uri("Resources/Images/Social/world-wide-web_over.png", UriKind.Relative));
        }

        private void website_MouseLeave(object sender, MouseEventArgs e)
        {
            website.Source = new BitmapImage(new Uri("Resources/Images/Social/world-wide-web.png", UriKind.Relative));
        }

        private void instagram_MouseEnter(object sender, MouseEventArgs e)
        {
            instagram.Source = new BitmapImage(new Uri("Resources/Images/Social/instagram_over.png", UriKind.Relative));
        }

        private void instagram_MouseLeave(object sender, MouseEventArgs e)
        {
            instagram.Source = new BitmapImage(new Uri("Resources/Images/Social/instagram.png", UriKind.Relative));
        }

        private void twitter_MouseEnter(object sender, MouseEventArgs e)
        {
            twitter.Source = new BitmapImage(new Uri("Resources/Images/Social/twitter_over.png", UriKind.Relative));
        }

        private void twitter_MouseLeave(object sender, MouseEventArgs e)
        {
            twitter.Source = new BitmapImage(new Uri("Resources/Images/Social/twitter.png", UriKind.Relative));
        }

        private void telegram_MouseEnter(object sender, MouseEventArgs e)
        {
            telegram.Source = new BitmapImage(new Uri("Resources/Images/Social/telegram_over.png", UriKind.Relative));
        }

        private void telegram_MouseLeave(object sender, MouseEventArgs e)
        {
            telegram.Source = new BitmapImage(new Uri("Resources/Images/Social/telegram.png", UriKind.Relative));
        }

        private void discord_MouseEnter(object sender, MouseEventArgs e)
        {
            discord.Source = new BitmapImage(new Uri("Resources/Images/Social/discord_over.png", UriKind.Relative));
        }

        private void discord_MouseLeave(object sender, MouseEventArgs e)
        {
            discord.Source = new BitmapImage(new Uri("Resources/Images/Social/discord.png", UriKind.Relative));
        }

        private void youtube_MouseEnter(object sender, MouseEventArgs e)
        {
            youtube.Source = new BitmapImage(new Uri("Resources/Images/Social/youtube_over.png", UriKind.Relative));
        }

        private void youtube_MouseLeave(object sender, MouseEventArgs e)
        {
            youtube.Source = new BitmapImage(new Uri("Resources/Images/Social/youtube.png", UriKind.Relative));
        }

        private void repear_MouseEnter(object sender, MouseEventArgs e)
        {
            repear.Source = new BitmapImage(new Uri("Resources/Images/rep_button_over.png", UriKind.Relative));
        }

        private void repear_MouseLeave(object sender, MouseEventArgs e)
        {
            repear.Source = new BitmapImage(new Uri("Resources/Images/rep_button.png", UriKind.Relative));
        }

        private void explorer_MouseEnter(object sender, MouseEventArgs e)
        {
            explorer.Source = new BitmapImage(new Uri("Resources/Images/explorer_over.png", UriKind.Relative));
        }

        private void explorer_MouseLeave(object sender, MouseEventArgs e)
        {
            explorer.Source = new BitmapImage(new Uri("Resources/Images/explorer.png", UriKind.Relative));
        }

        private void automatic_updater_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_updater.AutomaticUpdate)
            {
                b_automatic_updater.Source = new BitmapImage(new Uri("Resources/Images/automatic_update_activate_over.png", UriKind.Relative));
            }
            else
            {
                b_automatic_updater.Source = new BitmapImage(new Uri("Resources/Images/automatic_update_over.png", UriKind.Relative));
            }
        }

        private void automatic_updater_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_updater.AutomaticUpdate)
            {
                b_automatic_updater.Source = new BitmapImage(new Uri("Resources/Images/automatic_update_activate.png", UriKind.Relative));
            }
            else
            {
                b_automatic_updater.Source = new BitmapImage(new Uri("Resources/Images/automatic_update.png", UriKind.Relative));
            }
        }

        private void automatic_update_MouseDown(object sender, MouseEventArgs e)
        {
            _updater.AutomaticUpdate = !_updater.AutomaticUpdate;
            _updater.WriteProgressDownload();
            RefreshAutomaticUpdate();
        }

        private void website_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_website);
        }

        private void instagram_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_instagram);
        }

        private void twitter_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_twitter);
        }

        private void telegram_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_telegram);
        }

        private void discord_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_discord);
        }

        private void youtube_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_youtube);
        }

        private void trailer_MouseDown(object sender, MouseEventArgs e)
        {
            OpenWebSite(_trailer);
        }

        private void OpenWebSite(string link)
        {
            var process = new System.Diagnostics.ProcessStartInfo(link)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(process);
        }
        #endregion
    }
}
