# Weather Widget Plugin

This project was developed as a weather widget for windows widgets.

### Internal Settings

**reload_time** time unit seconds

**data_map** You can use different weather services by typing regex groups matching pattern into fields matching data_map. 


```json
{
  "reload_time": 600,//seconds
  "font_big": 24.0,
  "font_medium": 16.0,
  "font_small": 12.0,
  "color_light": "#FFFFFFFF",
  "color_dark": "#FF808080",
  "url": "", //registered url
  "pattern": "", //regex
  "data_map": {
    "Area": 0,
    "LastUpdate": 1,
    "Current": 2,
    "Current_Image": 3,
    "Day1": 4,
    "Day1_Today": 5,
    "Day1_Tonight": 6,
    "Day1_Image": 7,
    "Day1_Rain": 8,
    "Day2": 9,
    "Day2_Today": 10,
    "Day2_Tonight": 11,
    "Day2_Image": 12,
    "Day2_Rain": 13,
    "Day3": 14,
    "Day3_Today": 15,
    "Day3_Tonight": 16,
    "Day3_Image": 17,
    "Day3_Rain": 18,
    "Day4": 19,
    "Day4_Today": 20,
    "Day4_Tonight": 21,
    "Day4_Image": 22,
    "Day4_Rain": 23,
    "Day5": 24,
    "Day5_Today": 25,
    "Day5_Tonight": 26,
    "Day5_Image": 27,
    "Day5_Rain": 28
  }
}
```

### Screenshots

![weather](https://raw.githubusercontent.com/emretulek/Weather/refs/heads/master/sc_weather/weather_1.jpg)


![weather search](https://raw.githubusercontent.com/emretulek/Weather/refs/heads/master/sc_weather/weather_2.jpg)


![weather skin](https://raw.githubusercontent.com/emretulek/Weather/refs/heads/master/sc_weather/weather_3.jpg)
