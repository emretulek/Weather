using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Windows.Media;
using Widgets.Common;


namespace Weather
{
    public class WeatherViewModel:INotifyPropertyChanged,IDisposable
    {
        private readonly static string _searchUrl = "https://weather.com/api/v1/p/redux-dal";

        private readonly Schedule schedule = new();
        private string scheduleID = "";

        public struct SettingsStruct
        {
            public string Address { get; set; }
            public string GeoCode { get; set; }
            public int ReloadTimeSecond { get; set; }
            public float FontBig { get; set; }
            public float FontMedium { get; set; }
            public float FontSmall { get; set; }
            public SolidColorBrush ColorLight { get; set; }
            public SolidColorBrush ColorDark { get; set; }
        }

        public static SettingsStruct Default => new()
        {
            Address = "",
            GeoCode = "",
            ReloadTimeSecond = 600,
            FontBig = 24,
            FontMedium = 16,
            FontSmall = 12,
            ColorLight = new SolidColorBrush(Colors.White),
            ColorDark = new SolidColorBrush(Colors.Gray),
        };

        private Dictionary<string, object?>? _dataItems;

        public Dictionary<string, object?>? DataItems
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
            if (Settings.GeoCode == "")
            {
                return;
            }

            await Request();

            if (scheduleID == "")
            {
                scheduleID = schedule.Secondly(async () => await Request(), Settings.ReloadTimeSecond);
            }
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
                                GeoCode = $"{data?["location"]?["latitude"]?[i]?.ToString()},{data?["location"]?["longitude"]?[i]?.ToString()}" ?? "",
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
        private async Task Request()
        {
            try
            {
                using HttpClient client = new HttpClient();

                object jsonCotnent = new[] {
                    new {
                        name = "getSunV3CurrentObservationsUrlConfig",
                        @params = new
                        {
                            geocode = Settings.GeoCode,
                            units = "m",
                            language = "",
                            duration = "",
                        }
                    },
                    new {
                        name = "getSunV3DailyForecastWithHeadersUrlConfig",
                        @params = new
                        {
                            geocode = Settings.GeoCode,
                            units = "m",
                            language = CultureInfo.CurrentCulture.Name,
                            duration = "5day"
                        }
                    }
                };

                string jsonString = JsonConvert.SerializeObject(jsonCotnent);
                StringContent httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json"); ;
                var response = await client.PostAsync(_searchUrl, httpContent);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                var root = JObject.Parse(responseBody);
                var current = root["dal"]?["getSunV3CurrentObservationsUrlConfig"] as JObject;
                var daily = root["dal"]?["getSunV3DailyForecastWithHeadersUrlConfig"] as JObject;

                if (daily == null || current == null)
                {
                    return;
                }

                var current_property = current.Properties().First();

                if (current_property == null)
                {
                    return;
                }

                var current_data = current_property.Value["data"];
                var current_iconCode = current_data?["iconCode"];
                var current_temperature = current_data?["temperature"];
                var current_temperatureMax = current_data?["temperatureMax24Hour"];
                var current_temperatureMin = current_data?["temperatureMin24Hour"];
                var current_precip = current_data?["precip24Hour"];

                var property = daily.Properties().First();

                if (property == null)
                {
                    return;
                }

                var data = property.Value["data"];
                var lastUpdate = data?["responseHeaders"]?["date"] ?? "";
                var dayOfWeek = data?["dayOfWeek"];
                var daypart = data?["daypart"]?[0];
                var temperature = daypart?["temperature"];
                var precipChance = daypart?["precipChance"];
                var iconCode = daypart?["iconCode"];

                var dayDegree1_1 = string.IsNullOrEmpty(temperature?[0]?.ToString()) ? current_temperatureMax : temperature?[0];
                var dayDegree1_2 = string.IsNullOrEmpty(temperature?[1]?.ToString()) ? current_temperatureMin : temperature?[1];
                var dayImage1 = string.IsNullOrEmpty(iconCode?[0]?.ToString()) ? current_iconCode : iconCode?[0];
                var dayprecipChance1 = string.IsNullOrEmpty(precipChance?[0]?.ToString()) ? precipChance?[1] : precipChance?[0];

                DataItems = new()
                {
                    { "Area", Settings.Address },
                    { "LastUpdate", lastUpdate },
                    { "Current", current_temperature + " °C" },
                    { "CurrentImg", IconPath(current_iconCode) },
                    { "Day1", dayOfWeek?[0]},
                    { "Degree1", dayDegree1_1 + " / " + dayDegree1_2 },
                    { "Image1", IconPath(dayImage1) },
                    { "Rain1", dayprecipChance1 + "%"},
                    { "Day2", dayOfWeek?[1] },
                    { "Degree2", temperature?[2] + " / " + temperature?[3] },
                    { "Image2", IconPath(iconCode?[2]) },
                    { "Rain2", precipChance?[2] + "%"},
                    { "Day3", dayOfWeek?[2] },
                    { "Degree3", temperature?[4] + " / " + temperature?[5] },
                    { "Image3", IconPath(iconCode?[4]) },
                    { "Rain3", precipChance?[4] + "%"},
                    { "Day4", dayOfWeek?[3] },
                    { "Degree4", temperature?[6] + " / " + temperature?[7] },
                    { "Image4", IconPath(iconCode?[6]) },
                    { "Rain4", precipChance?[6] + "%"},
                    { "Day5", dayOfWeek?[4] },
                    { "Degree5", temperature?[8] + " / " + temperature?[9] },
                    { "Image5", IconPath(iconCode?[8]) },
                    { "Rain5", precipChance?[8] + "%"}
                };

                Logger.Info("Weather is updated.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="IconID"></param>
        /// <returns></returns>
        private static string IconPath(object? IconID)
        {
            string iconName = IconID?.ToString() ?? "na";
            string dllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            return Path.Combine(dllDirectory, "icons", iconName + ".png");
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
