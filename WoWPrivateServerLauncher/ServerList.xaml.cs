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
    /// Interaction logic for ServerList.xaml
    /// </summary>
    public partial class ServerList : Window
    {
        BackgroundWorker LoadVersionsWorker;
        public ServerList()
        {
            InitializeComponent();

            LoadVersionsWorker = new BackgroundWorker()
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            LoadVersionsWorker.ProgressChanged += LoadVersionsWorker_ProgressChanged;
            LoadVersionsWorker.RunWorkerCompleted += LoadVersionsWorker_RunWorkerCompleted;
            LoadVersionsWorker.DoWork += LoadVersionsWorker_DoWork;
        }

        private void LoadVersionsWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {

            }
            catch(Exception ex)
            {

            }
        }

        private void LoadVersionsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {

            }
            catch(Exception ex)
            {

            }
        }

        private void LoadVersionsWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            try
            {

            }
            catch (Exception ex)
            {

            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CMB_EXPANSIONS.ItemsSource = null;
            CMB_EXPANSIONS.ItemsSource = Data.AvailableExpansions.Expansions;
            CMB_EXPANSIONS.UpdateLayout();
        }

        private void BTN_CLOSE_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
