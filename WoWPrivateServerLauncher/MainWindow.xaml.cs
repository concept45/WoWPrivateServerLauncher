using MonoTorrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.ComponentModel;
using System.Windows.Interop;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.BEncoding;
using System.Diagnostics;
using MonoTorrent.Common;
using MonoTorrent.Dht.Listeners;
using MonoTorrent.Dht;
using System.Net;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.Tracker;
using WoWPrivateServerLauncher.Classes;

namespace WoWPrivateServerLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker DownloaderWorker;
        static string dhtNodeFile;
        static string basePath;
        static string downloadsPath;
        static string fastResumeFile;
        static string torrentsPath;
        static ClientEngine engine;				// The engine used for downloading
        static List<TorrentManager> torrents;	// The list where all the torrentManagers will be stored that the engine gives us
        static Top10Listener listener;      // This is a subclass of TraceListener which remembers the last 20 statements sent to it

        #region torrent tracker
        string downloadspeed = string.Empty;
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            DownloaderWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };

            DownloaderWorker.ProgressChanged += DownloaderWorker_ProgressChanged;
            DownloaderWorker.RunWorkerCompleted += DownloaderWorker_RunWorkerCompleted;
            DownloaderWorker.DoWork += DownloaderWorker_DoWork;
        }

        #region monotorrent
        static void manager_PeersFound(object sender, PeersAddedEventArgs e)
        {
            lock (listener)
                listener.WriteLine(string.Format("Found {0} new peers and {1} existing peers", e.NewPeers, e.ExistingPeers));//throw new Exception("The method or operation is not implemented.");
        }

        #endregion

        private void DownloaderWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (e.Argument == null)
                    return;

                basePath = Environment.CurrentDirectory;
                dhtNodeFile = System.IO.Path.Combine(basePath, "DhtNodes");
                downloadsPath = System.IO.Path.Combine(basePath, "Downloads");
                torrentsPath = System.IO.Path.Combine(basePath, "Torrents");
                fastResumeFile = System.IO.Path.Combine(torrentsPath, "fastresume.data");
                torrents = new List<TorrentManager>();     // The list where all the torrentManagers will be stored that the engine gives us
                listener = new Top10Listener(10);

                string torrentpath = e.Argument.ToString();

                int port = 6969;
                Torrent torrent = null;

                // Create the settings which the engine will use
                // downloadsPath - this is the path where we will save all the files to
                // port - this is the port we listen for connections on
                EngineSettings engineSettings = new EngineSettings(downloadsPath, port);
                engineSettings.PreferEncryption = false;
                engineSettings.AllowedEncryption = EncryptionTypes.All;

                //engineSettings.GlobalMaxUploadSpeed = 30 * 1024;
                //engineSettings.GlobalMaxDownloadSpeed = 100 * 1024;
                //engineSettings.MaxReadRate = 1 * 1024 * 1024;


                // Create the default settings which a torrent will have.
                // 4 Upload slots - a good ratio is one slot per 5kB of upload speed
                // 50 open connections - should never really need to be changed
                // Unlimited download speed - valid range from 0 -> int.Max
                // Unlimited upload speed - valid range from 0 -> int.Max
                TorrentSettings torrentDefaults = new TorrentSettings(4, 150, 0, 0);

                // Create an instance of the engine.
                engine = new ClientEngine(engineSettings);
                engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, port));
                byte[] nodes = null;
                try
                {
                    nodes = File.ReadAllBytes(dhtNodeFile);
                }
                catch
                {
                    Console.WriteLine("No existing dht nodes could be loaded");
                }

                DhtListener dhtListner = new DhtListener(new IPEndPoint(IPAddress.Any, port));
                DhtEngine dht = new DhtEngine(dhtListner);
                engine.RegisterDht(dht);
                dhtListner.Start();
                engine.DhtEngine.Start(nodes);

                // If the SavePath does not exist, we want to create it.
                if (!Directory.Exists(engine.Settings.SavePath))
                    Directory.CreateDirectory(engine.Settings.SavePath);

                // If the torrentsPath does not exist, we want to create it
                if (!Directory.Exists(torrentsPath))
                    Directory.CreateDirectory(torrentsPath);

                BEncodedDictionary fastResume;
                try
                {
                    fastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(fastResumeFile));
                }
                catch
                {
                    fastResume = new BEncodedDictionary();
                }


                // Load the .torrent from the file into a Torrent instance
                // You can use this to do preprocessing should you need to
                torrent = Torrent.Load(torrentpath);

                // When any preprocessing has been completed, you create a TorrentManager
                // which you then register with the engine.
                TorrentManager manager = new TorrentManager(torrent, downloadsPath, torrentDefaults);
                //if (fastResume.ContainsKey(torrent.InfoHash.ToHex()))
                //    manager.LoadFastResume(new FastResume((BEncodedDictionary)fastResume[torrent.infoHash.ToHex()]));
                engine.Register(manager);

                // Store the torrent manager in our list so we can access it later
                torrents.Add(manager);
                manager.PeersFound += new EventHandler<PeersAddedEventArgs>(manager_PeersFound);


                // Every time a piece is hashed, this is fired.
                manager.PieceHashed += delegate (object o, PieceHashedEventArgs ec)
                {
                    lock (listener)
                        listener.WriteLine(string.Format("Piece Hashed: {0} - {1}", ec.PieceIndex, ec.HashPassed ? "Pass" : "Fail"));
                };

                // Every time the state changes (Stopped -> Seeding -> Downloading -> Hashing) this is fired
                manager.TorrentStateChanged += delegate (object o, TorrentStateChangedEventArgs ev)
                {
                    lock (listener)
                        listener.WriteLine("OldState: " + ev.OldState.ToString() + " NewState: " + ev.NewState.ToString());
                };

                // Every time the tracker's state changes, this is fired
                //foreach (TrackerTier tier in manager.TrackerManager)
                //{
                //    //foreach (MonoTorrent.Client.Tracker.Tracker t in tier.Trackers)
                //    //{
                //    //    t.AnnounceComplete += delegate (object sender, AnnounceResponseEventArgs e)
                //    //    {
                //    //        listener.WriteLine(string.Format("{0}: {1}", e.Successful, e.Tracker.ToString()));
                //    //    };
                //    //}
                //}
                // Start the torrentmanager. The file will then hash (if required) and begin downloading/seeding
                manager.Start();


                // While the torrents are still running, print out some stats to the screen.
                // Details for all the loaded torrent managers are shown.
                bool running = true;
                StringBuilder sb = new StringBuilder(1024);
                while (running)
                {
                    //if ((i++) % 10 == 0)
                    //{
                    sb.Remove(0, sb.Length);
                    running = torrents.Exists(delegate (TorrentManager m) { return m.State != TorrentState.Stopped; });

                    //AppendFormat(sb, "Total Download Rate: {0:0.00}kB/sec", engine.TotalDownloadSpeed / 1024.0);
                    downloadspeed = (engine.TotalDownloadSpeed / 1024.0).ToString();
                    //AppendFormat(sb, "Total Upload Rate:   {0:0.00}kB/sec", engine.TotalUploadSpeed / 1024.0);
                    //AppendFormat(sb, "Disk Read Rate:      {0:0.00} kB/s", engine.DiskManager.ReadRate / 1024.0);
                    //AppendFormat(sb, "Disk Write Rate:     {0:0.00} kB/s", engine.DiskManager.WriteRate / 1024.0);
                    //AppendFormat(sb, "Total Read:         {0:0.00} kB", engine.DiskManager.TotalRead / 1024.0);
                    //AppendFormat(sb, "Total Written:      {0:0.00} kB", engine.DiskManager.TotalWritten / 1024.0);
                    //AppendFormat(sb, "Open Connections:    {0}", engine.ConnectionManager.OpenConnections);


                    //AppendSeperator(sb);
                    //AppendFormat(sb, "State:           {0}", manager.State);
                    //AppendFormat(sb, "Name:            {0}", manager.Torrent == null ? "MetaDataMode" : manager.Torrent.Name);
                    //AppendFormat(sb, "Progress:           {0:0.00}", manager.Progress);
                    //progress = manager.Progress.ToString();
                    //AppendFormat(sb, "Download Speed:     {0:0.00} kB/s", manager.Monitor.DownloadSpeed / 1024.0);
                    //AppendFormat(sb, "Upload Speed:       {0:0.00} kB/s", manager.Monitor.UploadSpeed / 1024.0);
                    //AppendFormat(sb, "Total Downloaded:   {0:0.00} MB", manager.Monitor.DataBytesDownloaded / (1024.0 * 1024.0));
                    //AppendFormat(sb, "Total Uploaded:     {0:0.00} MB", manager.Monitor.DataBytesUploaded / (1024.0 * 1024.0));
                    MonoTorrent.Client.Tracker.Tracker tracker = manager.TrackerManager.CurrentTracker;
                    //AppendFormat(sb, "Tracker Status:     {0}", tracker == null ? "<no tracker>" : tracker.State.ToString());
                    //AppendFormat(sb, "Warning Message:    {0}", tracker == null ? "<no tracker>" : tracker.WarningMessage);
                    //AppendFormat(sb, "Failure Message:    {0}", tracker == null ? "<no tracker>" : tracker.FailureMessage);
                    //if (manager.PieceManager != null)
                    //    AppendFormat(sb, "Current Requests:   {0}", manager.PieceManager.CurrentRequestCount());

                    //foreach (PeerId p in manager.GetPeers())
                    //    AppendFormat(sb, "\t{2} - {1:0.00}/{3:0.00}kB/sec - {0}", p.Peer.ConnectionUri,
                    //                                                              p.Monitor.DownloadSpeed / 1024.0,
                    //                                                              p.AmRequestingPiecesCount,
                    //                                                              p.Monitor.UploadSpeed / 1024.0);

                    //AppendFormat(sb, "", null);
                    //if (manager.Torrent != null)
                    //    foreach (TorrentFile file in manager.Torrent.Files)
                    //        AppendFormat(sb, "{1:0.00}% - {0}", file.Path, file.BitField.PercentComplete);

                    //Console.Clear();
                    //Console.WriteLine(sb.ToString());
                    //listener.ExportTo(Console.Out);
                    //}
                    DownloaderWorker.ReportProgress(Convert.ToInt32(manager.Progress),manager.State.ToString());
                    System.Threading.Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void DownloaderWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        private void DownloaderWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                PRG_DOWNLOAD.Value = e.ProgressPercentage;
                if (e.ProgressPercentage < 35)
                {
                    PRG_DOWNLOAD.Foreground = Brushes.Red;
                }
                else if (e.ProgressPercentage >= 35 && e.ProgressPercentage < 75)
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.dbar_available.GetHbitmap(),
                                                      IntPtr.Zero,
                                                      Int32Rect.Empty,
                                                      BitmapSizeOptions.FromEmptyOptions());
                    GRID_PROGRESS.Background = new ImageBrush(bitmapSource);
                    PRG_DOWNLOAD.Foreground = Brushes.Yellow;
                }
                else if (e.ProgressPercentage >= 75)
                {
                    var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.dbar_playable.GetHbitmap(),
                                      IntPtr.Zero,
                                      Int32Rect.Empty,
                                      BitmapSizeOptions.FromEmptyOptions());
                    GRID_PROGRESS.Background = new ImageBrush(bitmapSource);
                    PRG_DOWNLOAD.Foreground = Brushes.Green;
                }

                TXTPERCENTAGE.Text = e.ProgressPercentage.ToString();
                TXTTORRENTSTATE.Text = e.UserState.ToString();
                TXTDOWNLOADSPEED.Text = downloadspeed;
            }
            catch (Exception ex)
            {

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WEB_BROWSER.Navigate(new Uri("http://launcher.wow-europe.com/2.0/CT/index.xml?locale=en-GB"));
        }

        private void BTN_MINIMIZE_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BTN_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void BTN_PLAY_Click(object sender, RoutedEventArgs e)
        {
            DownloaderWorker.RunWorkerAsync(System.IO.Path.Combine(Environment.CurrentDirectory, "Torrents/335.torrent"));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void BTN_OPTIONS_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BTN_INFO_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BTN_HELP_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BTN_SERVERLIST_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
