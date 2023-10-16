using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Configuration;
using System.Data.SQLite;
using NAudio.Wave;
using System.Net;
using pfcode_stations.classes;
using System.Threading;

namespace pfcode_stations
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WaveOutEvent waveOut;
        private CancellationTokenSource cancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Run your code here after the MainWindow has loaded completely
            LoadStations();

            // Additional code to execute after the MainWindow is fully loaded
            stationName.SelectionChanged += stationName_SelectionChanged;
        }

        public void LoadStations()
        {

            SQLiteConnection connection = DbConnector.Connect();

            SQLiteCommand command = new SQLiteCommand("SELECT name FROM stations", connection);

            SQLiteDataReader reader = command.ExecuteReader();

            List<string> stations = new List<string>();

            while (reader.Read())
            {
                string stationName = reader.GetString(0);
                stations.Add(stationName);
            }

            foreach (string station in stations)
            {
                stationName.Items.Add(station);
            }

            //stationName.SelectedIndex = 0;
            stationName.SelectedIndex = -1;

            reader.Close();
        }

        private async void stationName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            waveOut?.Stop();
            waveOut?.Dispose();

            if (stationName.SelectedItem != null)
            {
                
                stopButton.Visibility = Visibility.Visible;

                try
                {
                    string mp3Url = await GetMp3UrlAsync(stationName.SelectedItem.ToString());
                    await PlayAudioStreamAsync(mp3Url);
                }
                catch (NAudio.MmException ex)
                {
                    // Log the exception or display an error message to assist with debugging
                    MessageBox.Show($"NAudio.MmException: {ex.Message}");
                }
                catch (Exception ex)
                {
                    // Handle other exceptions if needed
                    MessageBox.Show($"Exception: {ex.Message}");
                }
            }
            else
            {
                stopButton.Visibility = Visibility.Hidden;
            }
        }

        private async Task PlayAudioStreamAsync(string mp3Url, int maxRetries = 3, TimeSpan retryDelay = default)
        {
            int retryCount = 2;
            bool success = false;

            while (!success && retryCount < maxRetries)
            {
                try
                {
                    cancellationTokenSource = new CancellationTokenSource();
                    using (var reader = new MediaFoundationReader(mp3Url))
                    {
                        waveOut = new WaveOutEvent();
                        waveOut.Init(reader);
                        waveOut.Play();

                        while (waveOut.PlaybackState == PlaybackState.Playing)
                        {
                            if (cancellationTokenSource.IsCancellationRequested)
                                break;

                            await Task.Delay(300);
                        }

                        waveOut.Stop();
                        waveOut.Dispose();

                        success = true;
                    }
                }
                catch (NAudio.MmException ex)
                {
                    // Log the exception or display an error message for debugging purposes
                    MessageBox.Show($"NAudio.MmException: {ex.Message}");

                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        // Delay before retrying
                        await Task.Delay(retryDelay);
                    }
                }
                catch (Exception ex)
                {
                    // Handle other exceptions if needed
                    MessageBox.Show($"Exception: {ex.Message}");
                }
            }

            if (!success)
            {
                // Handle the failure after reaching the maximum number of retries
                MessageBox.Show("Failed to play audio stream after multiple retries.");
            }
        }

        private async Task<string> GetMp3UrlAsync(string stationName)
        {
            using (SQLiteConnection connection = DbConnector.Connect())
            {
                StringBuilder getUrl = new StringBuilder();
                getUrl.Append("SELECT url FROM stations WHERE name = @name");

                using (SQLiteCommand command = new SQLiteCommand(getUrl.ToString(), connection))
                {
                    command.Parameters.AddWithValue("@name", stationName);
                    return await command.ExecuteScalarAsync() as string;
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {

            // Stop the audio stream and do any cleanup required
            cancellationTokenSource?.Cancel();
            waveOut?.Stop();
            waveOut?.Dispose();

            stopButton.Visibility = Visibility.Collapsed;
            stationName.SelectedIndex = -1;
        }

    }
}
