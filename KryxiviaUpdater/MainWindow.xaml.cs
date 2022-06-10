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
        private object _state = new object();
        private Dictionary<int, Image> _imagesNews;
        private NewsList? _newsList;
        private const string _linkWebsite = "https://yetanotherchris.dev/csharp/6-ways-to-get-the-current-directory-in-csharp/";
        private const string _linkBlog = "";
        private const string _linkHelp = "";
        private const int _maxNews = 3;
        private string _pathKryxivia = $"kryxivia{System.IO.Path.DirectorySeparatorChar}Kryxivia.exe";
        private Timer _timer;
        private UpdaterState _updaterState;
        public MainWindow()
        {
            InitializeComponent();
            Initialize();

            _timer = new Timer();
            _timer.Elapsed += new ElapsedEventHandler((o, e) =>
            {
                SetNewsIndex(currentNewsIndex, true);
            });

            _timer.Interval = 10000;
            _timer.Enabled = true;

            _updater = new Updater.Updater("kryxivia","version_app.json",UpdateVersionProgress, UpdatePourcentProgress, SetNewsList, UnzipFileLog);
            _kryxiviaAPI = new Updater.KryxiviaAPI("https://kryx-app-auth-api.azurewebsites.net/", "http://93.23.21.204/"
                , UpdateState);
            _imagesNews = new Dictionary<int, Image>()
            {
                {0, b_news1},
                {1, b_news2},
                {2, b_news3},
            };

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
            });

        }

        public void Initialize()
        {
            l_pourcent.Content = "";
            l_version.Content  = "";
            t_news.Text = "";
            l_news.Content = "";
            p_progressBar.Value = 100;
            currentNewsIndex = 0;
            b_play.Cursor = Cursors.No;
        }
        public void UpdateVersionProgress(int start, int count)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_version.Content = $"{start}/{count}";

            }), DispatcherPriority.Background);
        }

        public void UpdatePourcentProgress(int pourcent)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_pourcent.Content = $"Updating... {pourcent}%";
                progress.Value = pourcent;

            }), DispatcherPriority.Background);
        }

        public void UnzipFileLog()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                l_pourcent.Content = $"Unzip file in progress...";
                progress.Value = 100;

            }), DispatcherPriority.Background);
        }

        public void SetNewsList(NewsList? newsList)
        {
            _newsList = newsList;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                SetNewsIndex(currentNewsIndex);

            }), DispatcherPriority.Background);
        }

        public void SetNewsIndex(int index, bool withIncrement = false)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                lock (_state)
                {
                    if (withIncrement)
                    {
                        if (++index >= _maxNews)
                            index = 0;
                    }
                    currentNewsIndex = index;


                    foreach (var entry in _imagesNews)
                    {

                        if (entry.Key == index)
                        {
                            entry.Value.Source = new BitmapImage(new Uri("Resources/Images/news_ellipse.png", UriKind.Relative));
                            if (_newsList?.News.ElementAtOrDefault(index) != null)
                            {
                                t_news.Text = _newsList.News[index].Contents;
                                l_news.Content = $"{_newsList.News[index].Date}  •  {_newsList.News[index].Type}";
                            }
                        }
                        else
                        {
                            entry.Value.Source = new BitmapImage(new Uri("Resources/Images/news_ellipse1.png", UriKind.Relative));
                        }
                    }
                }
            }), DispatcherPriority.Normal);

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
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Content = $"Finish... 100%";
                    l_version.Content = "";
                    progress.Value = 100;
                    b_play.Cursor = Cursors.Hand;
                    Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/Images/bg_play.png", UriKind.RelativeOrAbsolute)));

                }), DispatcherPriority.Background);
            }else if(_updaterState == UpdaterState.Connecting)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Content = $"Finish... 100%";
                    l_version.Content = "";
                    progress.Value = 100;
                    b_play.Cursor = Cursors.Hand;
                    Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/Images/bg_connect.png", UriKind.RelativeOrAbsolute)));

                }), DispatcherPriority.Background);
            }
            else if (_updaterState == UpdaterState.Repearing)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Content = $"Repearing...";
                    l_version.Content = "";
                    progress.Value = 100;
                    b_play.Cursor = Cursors.No;
                    Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/Images/bg.png", UriKind.RelativeOrAbsolute)));

                }), DispatcherPriority.Background);
            }else if(_updaterState == UpdaterState.Downloading)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    l_pourcent.Content = $"Updating... 100%";
                    l_version.Content = "";
                    progress.Value = 100;
                    b_play.Cursor = Cursors.No;
                    Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/Resources/Images/bg.png", UriKind.RelativeOrAbsolute)));

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

        private void b_reduce_MouseDown(object sender, MouseButtonEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void b_close_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }

        private void b_news1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetNewsIndex(0);
        }

        private void b_news2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetNewsIndex(1);
        }

        private void b_news3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SetNewsIndex(2);
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

        private void b_play_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (File.Exists(_pathKryxivia) && _updaterState == UpdaterState.Playing)
            {
                Process.Start(_pathKryxivia);
            }else if(_updaterState == UpdaterState.Connecting)
            {
                _kryxiviaAPI.OpenWebSite();
            }
        }
        private void b_reapear_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
    }
}
