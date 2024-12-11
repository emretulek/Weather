using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Widgets.Common;

namespace Weather
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window,IWidgetWindow
    {
        public readonly static string WidgetName = "Weather";
        public readonly static string SettingsFile = "settings.weather.json";
        private readonly Config config = new(SettingsFile);

        public WeatherViewModel ViewModel { get; set; }
        private WeatherViewModel.SettingsStruct Settings = WeatherViewModel.Default;
        private readonly DispatcherTimer _debounceTimer;

        public MainWindow()
        {
            InitializeComponent();

            LoadSettings();
            ViewModel = new()
            {
                Settings = Settings
            };
            DataContext = ViewModel;
            _ = ViewModel.Start();

            Logger.Info($"{WidgetName} is started");

            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _debounceTimer.Tick += DebounceTimer_Tick;
            ContentRendered += ResizeElements;
        }

        public void LoadSettings()
        { 
            try
            {
                Settings.Url = PropertyParser.ToString(config.GetValue("url"), Settings.Url);
                Settings.Pattern = PropertyParser.ToString(config.GetValue("pattern"), Settings.Pattern);
                Settings.ReloadTimeSecond = PropertyParser.ToInt(config.GetValue("reload_time"), Settings.ReloadTimeSecond);
                Settings.FontBig = PropertyParser.ToFloat(config.GetValue("font_big"), Settings.FontBig);
                Settings.FontMedium = PropertyParser.ToFloat(config.GetValue("font_medium"), Settings.FontMedium);
                Settings.FontSmall = PropertyParser.ToFloat(config.GetValue("font_small"), Settings.FontSmall);
                Settings.ColorLight = PropertyParser.ToColorBrush(config.GetValue("color_light"), Settings.ColorLight.ToString());
                Settings.ColorDark = PropertyParser.ToColorBrush(config.GetValue("color_dark"), Settings.ColorDark.ToString());
                Settings.DataMap = ((JObject)config.GetValue("data_map")).ToObject<Dictionary<string, int>>() ?? Settings.DataMap;
            }
            catch (Exception)
            {
                config.Add("reload_time", Settings.ReloadTimeSecond);
                config.Add("font_big", Settings.FontBig);
                config.Add("font_medium", Settings.FontMedium);
                config.Add("font_small", Settings.FontSmall);
                config.Add("color_light", Settings.ColorLight);
                config.Add("color_dark", Settings.ColorDark);
                config.Add("url", Settings.Url);
                config.Add("pattern", Settings.Pattern);
                config.Add("data_map", Settings.DataMap);
                config.Save();
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResizeElements(sender, e);
        }

        public void ResizeElements(object? sender, EventArgs e)
        {
            Settings.FontBig = (float)this.ActualWidth / 40;
            Settings.FontMedium = (float)this.ActualWidth / 30;
            Settings.FontBig = (float)this.ActualWidth / 10;

            ViewModel.Settings = Settings;
                
            Resources["MaxWidth"] = this.ActualWidth / 10;
            Resources["MinWidth"] = this.ActualWidth / 10;
            Resources["MaxWidthBig"] = this.ActualWidth / 5;
            Resources["MinWidthBig"] = this.ActualWidth / 5;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            ViewModel.Dispose();
            Logger.Info($"{WidgetName} is closed");
        }

        public WidgetWindow WidgetWindow()
        {
            return new WidgetWindow(this, WidgetDefaultStruct());
        }

        public static WidgetDefaultStruct WidgetDefaultStruct()
        {
            return new()
            {
                MinWidth = 400,
                MinHeight = 200,
                SizeToContent = SizeToContent.WidthAndHeight,
                Padding = new Thickness(10)
            };
        }

        private async void DebounceTimer_Tick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            if (SearchBox.Text != "")
            {
                SearchPanel.Visibility = Visibility.Visible;

                ViewModel.Areas.Add(new Area
                {
                    Address = "Searching...",
                    City = "",
                    Country = "",
                    Language = "",
                    LocID = "",
                    PlaceID = ""
                });
                await ViewModel.SearchRequest(SearchBox.Text);
            }
        }

        // SearchBox Focused
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (e.RoutedEvent == UIElement.GotFocusEvent)
            {
                SearchBox.Padding = new Thickness(15, 10, 15, 10);
                PlaceHolder.Padding = new Thickness(15, 10, 15, 10);
                SearchBox.Background = new SolidColorBrush(Color.FromArgb(128, Colors.Black.R, Colors.Black.G, Colors.Black.B));
            }
            else
            {
                SearchBox.Padding = new Thickness(0);
                PlaceHolder.Padding = new Thickness(0);
                SearchBox.Background = new SolidColorBrush(Colors.Transparent);
                FocusManager.SetFocusedElement(this, BigImage);
            }
        }

        // Search Crypto Symbol
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceTimer.Stop();

            if (SearchBox.Text == "")
            {
                PlaceHolder.Opacity = 0.5;
                SearchPanel.Visibility = Visibility.Collapsed;
                return;
            }

            PlaceHolder.Opacity = 0;

            _debounceTimer.Start();
        }

        // switch focus from searchbox to searchresults
        private void SearchBox_DownArrow(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && SearchResults.Items.Count > 0)
            {
                SearchResults.SelectedIndex = 0;
                SearchResults.Focus();
            }
        }

        // Select searchresult with mouse
        private void SearchResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (SearchResults.SelectedItem != null)
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                SearchBox.Text = "";

                var area = (Area)SearchResults.SelectedItem;

                Settings.Url = $"https://weather.com/{area.Language}/weather/today/l/{area.PlaceID}";
                ViewModel.Settings = Settings;
                _ = ViewModel.Start();
                config.Add("url", Settings.Url);
                config.Save();
            }
        }

        // Select searchresult with Enter
        private void SearchResults_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && SearchResults.SelectedItem != null)
            {
                SearchPanel.Visibility = Visibility.Collapsed;
                SearchBox.Text = "";

                var area = (Area)SearchResults.SelectedItem;

                Settings.Url = $"https://weather.com/{area.Language}/weather/today/l/{area.PlaceID}";
                ViewModel.Settings = Settings;
                _ = ViewModel.Start();
                config.Add("url", Settings.Url);
                config.Save();
            }
        }
    }
}