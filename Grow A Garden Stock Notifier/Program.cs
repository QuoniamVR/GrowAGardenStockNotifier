using System;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Grow_A_Garden_Stock_Notifier
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Run the app with a custom ApplicationContext instead of a Form
            Application.Run(new NotifierContext());
        }
    }

    public class WeatherResponse
    {
        public string currentWeather { get; set; }
        public string effectDescription { get; set; }
    }

    public class SeedResponse
    {
        public List<string> gear { get; set; }
        public List<string> seeds { get; set; }
    }

    public class EggResponse
    {
        public List<string> egg { get; set; }
    }

    // Renamed from Notifier : Form to NotifierContext : ApplicationContext
    public class NotifierContext : ApplicationContext
    {
        private float UpdateLength = 5f;
        private float StartTimeFloat;

        private Timer updateTimer;

        private static readonly HttpClient client = new HttpClient();

        private NotifyIcon notifyIcon;

        private string lastWeather;
        private string LastSeeds;
        private string LastEggs;

        private Icon trayIcon;

        public NotifierContext()
        {
            trayIcon = new Icon("Icon.ico");

            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = trayIcon; // Or your custom icon
            notifyIcon.Visible = true;
            notifyIcon.Text = "Grow A Garden Stock";

            notifyIcon.MouseDoubleClick += (s, e) =>
            {
                Process.Start("cmd", $"/c start https://growagardenstock.com/");
            };

            // Add a context menu to allow exit
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, ExitClicked);
            notifyIcon.ContextMenuStrip = contextMenu;

            StartTimer();

            updateTimer = new Timer();
            updateTimer.Interval = (int)(UpdateLength * 1000f);
            updateTimer.Tick += (s, e) => RefreshTimer();
            updateTimer.Start();
        }

        private void ExitClicked(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            trayIcon.Dispose();
            Application.Exit();
        }

        private void Notify(string title, string text)
        {
            if (notifyIcon != null)
            {
                notifyIcon.BalloonTipTitle = title;
                notifyIcon.BalloonTipText = text;
                notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon.ShowBalloonTip(2500);
            }
        }

        private async Task FetchStockDataAsync()
        {
            try
            {
                string url_weather = "https://growagardenstock.com/api/stock/weather?ts=1749491972625&_=1749491972625";
                string url_seeds = "https://growagardenstock.com/api/stock?type=gear-seeds&ts=1749492068525";
                string url_egg = "https://growagardenstock.com/api/stock?type=egg&ts=1749492068526";

                string weatherResponse = await client.GetStringAsync(url_weather);
                string seedsResponse = await client.GetStringAsync(url_seeds);
                string eggsResponse = await client.GetStringAsync(url_egg);

                if (weatherResponse != lastWeather)
                {
                    lastWeather = weatherResponse;

                    var data = JsonConvert.DeserializeObject<WeatherResponse>(weatherResponse);

                    Notify(data.currentWeather, data.effectDescription);
                }

                if (seedsResponse != LastSeeds)
                {
                    LastSeeds = seedsResponse;

                    try
                    {
                        var data = JsonConvert.DeserializeObject<SeedResponse>(seedsResponse);

                        string SeedsText = "";
                        foreach (string key in data.seeds)
                        {
                            string cleaned = key.Replace("*", "");
                            SeedsText += cleaned + Environment.NewLine;
                        }

                        string GearText = "";
                        foreach (string key in data.gear)
                        {
                            string cleaned = key.Replace("*", "");
                            GearText += cleaned + Environment.NewLine;
                        }

                        Notify("Seeds Reset!", SeedsText);
                        Notify("Gear Reset!", GearText);
                    }
                    catch
                    {
                        // Handle or log exceptions if needed
                    }
                }

                if (eggsResponse != LastEggs)
                {
                    LastEggs = eggsResponse;

                    try
                    {
                        var data = JsonConvert.DeserializeObject<EggResponse>(eggsResponse);

                        string EggText = "";
                        foreach (string key in data.egg)
                        {
                            string cleaned = key.Replace("*", "");
                            EggText += cleaned + Environment.NewLine;
                        }

                        Notify("Eggs Reset!", EggText);
                    }
                    catch
                    {
                        // Handle or log exceptions if needed
                    }
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Request error: " + e.Message);
            }
        }

        private void StartTimer()
        {
            long startTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            StartTimeFloat = (float)startTime;
        }

        private void RefreshTimer()
        {
            Console.WriteLine("Reset!");
            FetchStockDataAsync();
        }
    }
}
