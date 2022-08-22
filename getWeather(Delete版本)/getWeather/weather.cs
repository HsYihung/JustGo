using System;
using System.Collections.Generic;
using System.Text;

namespace getWeather
{
    class weatherRawData
    {
        public string location = "";
        public string locationsName = "";
        public DateTime startTime;
        public DateTime endTime;
        public int pop12h = 0;
        public int rh = 0;
        public string wx = "";
        public int minT = 0;
        public int maxT = 0;
        public int uvi = 0;
        public string uviState = "";
        public string weatherDescription = "";
    }
}
