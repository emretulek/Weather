using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using Widgets.Common;


namespace Weather
{
    public class WeatherViewModel:INotifyPropertyChanged,IDisposable
    {
        private readonly static string _searchUrl = "https://weather.com/api/v1/p/redux-dal";

        private readonly Schedule schedule = new();
        private string scheduleID = "";

        private readonly static Dictionary<string, int> dataMap = new()
        {
            ["Area"] = 0,
            ["LastUpdate"] = 1,
            ["Current"] = 2,
            ["Current_Image"] = 3,
            ["Day1"] = 4,
            ["Day1_Today"] = 5,
            ["Day1_Tonight"] = 6,
            ["Day1_Image"] = 7,
            ["Day1_Rain"] = 8,
            ["Day2"] = 9,
            ["Day2_Today"] = 10,
            ["Day2_Tonight"] = 11,
            ["Day2_Image"] = 12,
            ["Day2_Rain"] = 13,
            ["Day3"] = 14,
            ["Day3_Today"] = 15,
            ["Day3_Tonight"] = 16,
            ["Day3_Image"] = 17,
            ["Day3_Rain"] = 18,
            ["Day4"] = 19,
            ["Day4_Today"] = 20,
            ["Day4_Tonight"] = 21,
            ["Day4_Image"] = 22,
            ["Day4_Rain"] = 23,
            ["Day5"] = 24,
            ["Day5_Today"] = 25,
            ["Day5_Tonight"] = 26,
            ["Day5_Image"] = 27,
            ["Day5_Rain"] = 28
        };


        public struct SettingsStruct
        {
            public string Url {  get; set; }
            public string Pattern {  get; set; }
            public Dictionary<string, int> DataMap { get; set; }
            public int ReloadTimeSecond { get; set; }
            public float FontBig { get; set; }
            public float FontMedium { get; set; }
            public float FontSmall { get; set; }
            public SolidColorBrush ColorLight { get; set; }
            public SolidColorBrush ColorDark { get; set; }
        }

        public static SettingsStruct Default => new()
        {
            Url = "",
            Pattern = Pattern(),
            DataMap = dataMap,
            ReloadTimeSecond = 600,
            FontBig = 24,
            FontMedium = 16,
            FontSmall = 12,
            ColorLight = new SolidColorBrush(Colors.White),
            ColorDark = new SolidColorBrush(Colors.Gray),
        };

        private Dictionary<string, object>? _dataItems;

        public Dictionary<string, object>? DataItems
        {
            get { return _dataItems; }
            set { 
                _dataItems = value;
                OnPropertyChanged(nameof(DataItems));
            }
        }

        private SettingsStruct _settings = Default;
        public required SettingsStruct Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        private ObservableCollection<Area> _areas = [];
        public ObservableCollection<Area> Areas
        {
            get => _areas;
            set
            {
                _areas = value;
                OnPropertyChanged(nameof(Areas));
            }
        }


        public async Task Start()
        {
            if (Settings.Url == "")
            {
                return;
            }

            await UpdateWeather();

            if (scheduleID == "")
            {
                scheduleID = schedule.Secondly(async () => await UpdateWeather(), Settings.ReloadTimeSecond);
            }
        }

        /// <summary>
        /// Update Weather UI
        /// </summary>
        /// <returns></returns>
        private async Task UpdateWeather()
        {
            var results = await Request();

            if (results.Count == 0)
            {
                return;
            }
          
            string implotes = string.Join(", ", results);
            Logger.Info(implotes);

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    DataItems = new()
                    {
                        { "Area", results[Settings.DataMap["Area"]] },
                        { "LastUpdate", results[Settings.DataMap["LastUpdate"]] },
                        { "Current", results[Settings.DataMap["Current"]] + " °C" },
                        { "CurrentImg", IconPath(results[Settings.DataMap["Current_Image"]]) },
                        { "Day1", results[Settings.DataMap["Day1"]] },
                        { "Degree1", results[Settings.DataMap["Day1_Today"]] + " / " + results[Settings.DataMap["Day1_Tonight"]] },
                        { "Image1", IconPath(results[Settings.DataMap["Day1_Image"]]) },
                        { "Rain1", results[Settings.DataMap["Day1_Rain"]] },
                        { "Day2", results[Settings.DataMap["Day2"]] },
                        { "Degree2", results[Settings.DataMap["Day2_Today"]] + " / " + results[Settings.DataMap["Day2_Tonight"]] },
                        { "Image2", IconPath(results[Settings.DataMap["Day2_Image"]]) },
                        { "Rain2", results[Settings.DataMap["Day2_Rain"]] },
                        { "Day3", results[Settings.DataMap["Day3"]] },
                        { "Degree3", results[Settings.DataMap["Day3_Today"]] + " / " + results[Settings.DataMap["Day3_Tonight"]] },
                        { "Image3", IconPath(results[Settings.DataMap["Day3_Image"]]) },
                        { "Rain3", results[Settings.DataMap["Day3_Rain"]] },
                        { "Day4", results[Settings.DataMap["Day4"]] },
                        { "Degree4", results[Settings.DataMap["Day4_Today"]] + " / " + results[Settings.DataMap["Day4_Tonight"]] },
                        { "Image4", IconPath(results[Settings.DataMap["Day4_Image"]]) },
                        { "Rain4", results[Settings.DataMap["Day4_Rain"]] },
                        { "Day5", results[Settings.DataMap["Day5"]] },
                        { "Degree5", results[Settings.DataMap["Day5_Today"]] + " / " + results[Settings.DataMap["Day5_Tonight"]] },
                        { "Image5", IconPath(results[Settings.DataMap["Day5_Image"]]) },
                        { "Rain5", results[Settings.DataMap["Day5_Rain"]] }
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }); 
        }


        public async Task SearchRequest(string _query)
        {
            try
            {
                using HttpClient client = new();
                object jsonCotnent = new[]
                {
                    new
                    {
                        name = "getSunV3LocationSearchUrlConfig",
                        @params = new
                        {
                            query = _query,
                            language = CultureInfo.CurrentCulture.Name,
                            locationType = "locale"
                        }
                    }
                };

                string jsonString = JsonConvert.SerializeObject(jsonCotnent);
                StringContent httpContent = new(jsonString, Encoding.UTF8, "application/json"); ;
                var response = await client.PostAsync(_searchUrl, httpContent);

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine(responseBody);
                    Areas.Clear();
                    var root = JObject.Parse(responseBody);

                    if (root["dal"]?["getSunV3LocationSearchUrlConfig"] is not JObject config)
                    {
                        return;
                    }

                    foreach (var property in config.Properties())
                    {
                        var data = property.Value["data"];
                        var placeIds = data?["location"]?["placeId"];

                        if (placeIds == null)
                        {
                            return;
                        }

                        for (int i = 0; placeIds.Count() > i; i++)
                        {
                            Areas.Add(new Area
                            {
                                Address = string.Join(",", data?["location"]?["address"]?[i]?.ToString()),
                                City = data?["location"]?["city"]?[i]?.ToString() ?? "",
                                Country = data?["location"]?["country"]?[i]?.ToString() ?? "",
                                LocID = data?["location"]?["locId"]?[i]?.ToString() ?? "",
                                PlaceID = data?["location"]?["placeId"]?[i]?.ToString() ?? "",
                                Language = CultureInfo.CurrentCulture.Name,
                            });
                        }
                    }
                }
                else
                {
                    Logger.Error($"{response.StatusCode}");
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// Request
        /// </summary>
        /// <returns></returns>
        private async Task<List<string>> Request()
        {
            List<string> groupsList = [];

            try
            {
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer()
                };
                
                var cookie = new Cookie("unitOfMeasurement", "m")
                {
                    Domain = new Uri(Settings.Url).Host,
                    Path = "/",
                    Secure = true,
                    HttpOnly = true
                };
                handler.CookieContainer.Add(cookie);

                using HttpClient client = new(handler);
                string htmlContent = await client.GetStringAsync(Settings.Url);

                Match match = Regex.Match(htmlContent, Settings.Pattern, RegexOptions.Singleline, TimeSpan.FromSeconds(2));

                if (match.Success)
                {
                    groupsList = match.Groups.Cast<Group>()
                            .Skip(1)
                            .Select(g => g.Value)
                            .ToList();
                }
                else
                {
                    Logger.Warning("Regex not matching.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
            

            return groupsList;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="IconID"></param>
        /// <returns></returns>
        private static string IconPath(string IconID)
        {
            string dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            return Path.Combine(dllDirectory, "icons", IconID+".png");
        }
        /// <summary>
        /// düzenli görnüş için burda
        /// </summary>
        /// <returns></returns>
        private static string Pattern()
        {
            string pattern = @"<div.*? data-testid=""CurrentConditionsContainer"".*?<h1.*?>(.*?)</h1>";
            pattern += @"<span.*?>(.*?)</span>";
            pattern += @".*?<div.*?><span.*?>(.*?)<span.*?>";
            pattern += @".*?<svg.*? skycode=""(.*?)"".*?>.*?</svg></div>";

            pattern += @".*?<section.*? data-testid=""DailyWeatherModule"".*?>";

            //day1
            pattern += @".*?<a.*?href=""(?:/\w{2}-\w{2}|)/weather/tenday/l/[a-f0-9]+"".*?><h3.*?><span.*?>(.*?)</span></h3>";
            pattern += @"<div data-testid=""SegmentHighTemp"".*?><span data-testid=""TemperatureValue"".*?>(.*?)(?:<span>|</span>).*?<span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?</div>";
            pattern += @".*?<svg.*? skycode=""(.*?)"".*?>.*?</svg></div>";
            pattern += @".*?<div data-testid=""SegmentPrecipPercentage"".*?>.*?<span.*?>.*?</span>(.*?)</span></div>";
            //day2
            pattern += @".*?<a.*?href=""(?:/\w{2}-\w{2}|)/weather/tenday/l/[a-f0-9]+"".*?><span.*?>(.*?)</span></h3>";
            pattern += @"<div data-testid=""SegmentHighTemp"".*?><span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?<span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?</div>";
            pattern += @".*?<svg.*? skycode=""(.*?)"".*?>.*?</svg></div>";
            pattern += @".*?<div data-testid=""SegmentPrecipPercentage"".*?>.*?<span.*?>.*?</span>(.*?)</span></div>";
            //day3
            pattern += @".*?<a.*?href=""(?:/\w{2}-\w{2}|)/weather/tenday/l/[a-f0-9]+"".*?><h3.*?><span.*?>(.*?)</span></h3>";
            pattern += @"<div data-testid=""SegmentHighTemp"".*?><span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?<span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?</div>";
            pattern += @".*?<svg.*? skycode=""(.*?)"".*?>.*?</svg></div>";
            pattern += @".*?<div data-testid=""SegmentPrecipPercentage"".*?>.*?<span.*?>.*?</span>(.*?)</span></div>";
            //day4
            pattern += @".*?<a.*?href=""(?:/\w{2}-\w{2}|)/weather/tenday/l/[a-f0-9]+"".*?><h3.*?><span.*?>(.*?)</span></h3>";
            pattern += @"<div data-testid=""SegmentHighTemp"".*?><span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?<span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?</div>";
            pattern += @".*?<svg.*? skycode=""(.*?)"".*?>.*?</svg></div>";
            pattern += @".*?<div data-testid=""SegmentPrecipPercentage"".*?>.*?<span.*?>.*?</span>(.*?)</span></div>";
            //day5
            pattern += @".*?<a.*?href=""(?:/\w{2}-\w{2}|)/weather/tenday/l/[a-f0-9]+"".*?><h3.*?><span.*?>(.*?)</span></h3>";
            pattern += @"<div data-testid=""SegmentHighTemp"".*?><span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?<span data-testid=""TemperatureValue"".*?>(.*?)<span>.*?</div>";
            pattern += @".*?<svg.*? skycode=""(.*?)"".*?>.*?</svg></div>";
            pattern += @".*?<div data-testid=""SegmentPrecipPercentage"".*?>.*?<span.*?>.*?</span>(.*?)</span></div>";

            return pattern;
        }


        public void Dispose()
        {
            schedule.Stop(scheduleID);
            GC.SuppressFinalize(this);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
