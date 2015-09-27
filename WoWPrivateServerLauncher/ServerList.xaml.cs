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
                e.Result = null;

                if (e.Argument == null)
                    return;

                Expansion SelectedExpansion = (Expansion)e.Argument;

                if (SelectedExpansion == null)
                    return;

                List<ServerVersion> VersionsForThisExpansion = (from p in Data.VersionsAvailable.Versions where p.expansion == SelectedExpansion.name select p).OrderBy(p => p.version).ToList();

                e.Result = VersionsForThisExpansion;
            }
            catch(Exception ex)
            {
                e.Result = null;
            }
        }

        private void LoadVersionsWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                CMB_VERSIONS.ItemsSource = null;
                if (e.Result == null)
                {
                    CMB_VERSIONS.IsEnabled = false;
                    BUSY_INDICATOR.IsBusy = false;
                    return;
                }

                List<ServerVersion> Versions = (List<ServerVersion>)e.Result;

                if (Versions == null || Versions.Count() == 0)
                {
                    CMB_VERSIONS.IsEnabled = false;
                    BUSY_INDICATOR.IsBusy = false;
                    return;
                }

                CMB_VERSIONS.ItemsSource = null;
                CMB_VERSIONS.ItemsSource = Versions;

                CMB_VERSIONS.SelectedItem = (from p in Versions select p).FirstOrDefault();

                CMB_VERSIONS.UpdateLayout();
                CMB_VERSIONS.IsEnabled = true;

                BUSY_INDICATOR.IsBusy = false;
            }
            catch(Exception ex)
            {
                BUSY_INDICATOR.IsBusy = false;
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

        private void CMB_EXPANSIONS_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CMB_EXPANSIONS.SelectedIndex == -1)
                    return;

                if (CMB_EXPANSIONS.SelectedItem == null)
                    return;

                Expansion SelectedExpansion = (Expansion)CMB_EXPANSIONS.SelectedItem;

                if (SelectedExpansion == null)
                    return;

                BUSY_INDICATOR.BusyContent = "Loading...";
                BUSY_INDICATOR.IsBusy = true;
                LoadVersionsWorker.RunWorkerAsync(SelectedExpansion);
            }
            catch(Exception ex)
            {

            }
        }
    }
}
