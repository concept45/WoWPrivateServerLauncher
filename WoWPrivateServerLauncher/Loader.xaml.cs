using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WoWPrivateServerLauncher.Classes;

namespace WoWPrivateServerLauncher
{
    /// <summary>
    /// Interaction logic for Loader.xaml
    /// </summary>
    public partial class Loader : Window
    {
        BackgroundWorker LoadWorker;
        public Loader()
        {
            InitializeComponent();

            LoadWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true,
            };
            LoadWorker.ProgressChanged += LoadWorker_ProgressChanged;
            LoadWorker.DoWork += LoadWorker_DoWork;
            LoadWorker.RunWorkerCompleted += LoadWorker_RunWorkerCompleted;
        }

        private void LoadWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try {
                this.Hide();
                MainWindow Main = new MainWindow();
                Main.ShowDialog();
            }
            catch(Exception ex)
            {

            }
        }

        private void LoadWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                LoadWorker.ReportProgress(0, "Loading Versions...");

                Data.VersionsAvailable = WebService.GetVersions();

                LoadWorker.ReportProgress(35, "Loading Expansions...");

                Data.AvailableExpansions = WebService.GetExpansions();

                LoadWorker.ReportProgress(75, "Loading Servers...");

                Data.AvailableServers = WebService.GetServers();

                LoadWorker.ReportProgress(100, "Done.");
            }
            catch(Exception ex)
            {

            }
        }

        private void LoadWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {
                TXTPROGRESSINFO.Text = e.UserState.ToString();
                TXTPROGRESSVALUE.Text = e.ProgressPercentage.ToString() + "%";
                PRG_LOADER.Value = e.ProgressPercentage;
            }
            catch(Exception ex)
            {

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadWorker.RunWorkerAsync();
        }
    }
}
