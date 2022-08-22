using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace getWeather
{
    class Program


    {
        static void Main(string[] args)
        {
            int count = 0;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            SqlConnectionStringBuilder scsb;
            string myDBConnectionString = "";
            scsb = new SqlConnectionStringBuilder();
            scsb.DataSource = @".";
            scsb.InitialCatalog = "Travel";
            scsb.IntegratedSecurity = true;
            //myDBConnectionString = scsb.ToString();
            myDBConnectionString = "Server=tcp:justgo.database.windows.net,1433;Initial Catalog=Travel;Persist Security Info=False;User ID=DB;Password=P@ssw0rd-iii;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            string[,] id = new string[,] {
                {"宜蘭縣", "F-D0047-003" },
                {"桃園市", "F-D0047-007" },
                {"新竹縣", "F-D0047-011" },
                {"苗栗縣", "F-D0047-015" },
                {"彰化縣", "F-D0047-019" },
                {"南投縣", "F-D0047-023" },
                {"雲林縣", "F-D0047-027" },
                {"嘉義縣", "F-D0047-031" },
                {"屏東縣", "F-D0047-035" },
                {"臺東縣", "F-D0047-039" },
                {"花蓮縣", "F-D0047-043" },
                {"澎湖縣", "F-D0047-047" },
                {"基隆市", "F-D0047-051" },
                {"新竹市", "F-D0047-055" },
                {"嘉義市", "F-D0047-059" },
                {"臺北市", "F-D0047-063" },
                {"高雄市", "F-D0047-067" },
                {"新北市", "F-D0047-071" },
                {"臺中市", "F-D0047-075" },
                {"臺南市", "F-D0047-079" },
                {"連江縣", "F-D0047-083" },
                {"金門縣", "F-D0047-087" }
            };
            string key = "CWB-CF718E9C-6C7A-45A2-8147-A377F41E4199";

            List<weatherRawData> dataList = new List<weatherRawData>();

            for (int j = 0; j < id.Length / 2; j++)
            {
                string url = string.Format("https://opendata.cwb.gov.tw/api/v1/rest/datastore/{0}?Authorization={1}", id[j, 1], key);
                //呼叫API
                //id[,] 各縣市api
                //Authorization api key
                JArray jsondata = getJson(url);

                foreach (JObject data in jsondata)
                {
                    string locationname = (string)data["locationName"];//鄉鎮市名
                    int uvi = 0;
                    for (int i = 0; i < (int)data["weatherElement"][0]["time"].Count(); i++)//count 時段數量
                    {
                        // new
                        weatherRawData rawData = new weatherRawData();
                        rawData.location = id[j, 0];
                        rawData.locationsName = (string)data["locationName"];
                        rawData.startTime = (DateTime)data["weatherElement"][0]["time"][i]["startTime"];
                        if ((rawData.startTime.Day-rawData.startTime.AddHours(8).Day)<0)
                        {
                            continue;
                        }
                        rawData.startTime.AddHours(-8);
                        rawData.endTime = ((DateTime)data["weatherElement"][0]["time"][i]["endTime"]).AddHours(-8);                       
                        if ((string)data["weatherElement"][0]["time"][i]["elementValue"][0]["value"] != " ")
                        {
                            rawData.pop12h = (int)data["weatherElement"][0]["time"][i]["elementValue"][0]["value"];
                        }
                        rawData.minT = (int)data["weatherElement"][8]["time"][i]["elementValue"][0]["value"];
                        rawData.maxT = (int)data["weatherElement"][12]["time"][i]["elementValue"][0]["value"];

                        if ((DateTime)data["weatherElement"][0]["time"][i]["startTime"] == (DateTime)data["weatherElement"][9]["time"][uvi]["startTime"])
                        {
                            rawData.uvi = (int)data["weatherElement"][9]["time"][uvi]["elementValue"][0]["value"];

                            if (uvi < (int)data["weatherElement"][9]["time"].Count() - 1)
                            {
                                uvi++;
                            }
                        }

                        rawData.wx = (string)data["weatherElement"][6]["time"][i]["elementValue"][0]["value"];
                        //rawData.weatherDescription = (string)data["weatherElement"][10]["time"][i]["elementValue"][0]["value"];
                        dataList.Add(rawData);
                    }
                }
                Console.WriteLine(string.Format("載入{0}資料...({1}/{2})", id[j, 0], j + 1, id.Length / 2));
            }


            SqlConnection con = new SqlConnection(myDBConnectionString);
            con.Open();
            string strSQL = "Delete weather";
            SqlCommand cmd = new SqlCommand(strSQL, con);

            cmd.ExecuteNonQuery();
            strSQL = "DBCC CHECKIDENT( weather, RESEED, 0)";
            cmd = new SqlCommand(strSQL, con);
            cmd.ExecuteNonQuery();


            foreach (weatherRawData item in dataList)
            {
                strSQL = "INSERT INTO weather (location,locationsName,startTime,endTime,pop12h,minT,maxT,uvi,wx) VALUES(@location,@locationsName,@startTime,@endTime,@pop12h,@minT,@maxT,@uvi,@wx);";
                cmd = new SqlCommand(strSQL, con);
                cmd.Parameters.AddWithValue("@location", item.location);
                cmd.Parameters.AddWithValue("@locationsName", item.locationsName);
                cmd.Parameters.AddWithValue("@startTime", item.startTime);
                cmd.Parameters.AddWithValue("@endTime", item.endTime);
                cmd.Parameters.AddWithValue("@pop12h", item.pop12h);
                cmd.Parameters.AddWithValue("@minT", item.minT);
                cmd.Parameters.AddWithValue("@maxT", item.maxT);
                cmd.Parameters.AddWithValue("@uvi", item.uvi);
                cmd.Parameters.AddWithValue("@wx", item.wx);
                //cmd.Parameters.AddWithValue("@weatherDescription", item.weatherDescription);
                cmd.ExecuteNonQuery();
                count++;
            }

            con.Close();
            stopwatch.Stop();
            Console.WriteLine("執行完畢，共新增" + count + "筆資料,耗時" + stopwatch.ElapsedMilliseconds / 1000 + "秒");
            Console.ReadLine();
        }

        static public JArray getJson(string uri)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri); //request api
            req.Timeout = 10000;
            req.Method = "GET";
            HttpWebResponse respone = (HttpWebResponse)req.GetResponse(); //get respone
            StreamReader streamReader = new StreamReader(respone.GetResponseStream(), Encoding.UTF8); //read
            string result = streamReader.ReadToEnd(); //read end
            respone.Close();
            streamReader.Close();
            JObject jsondata = JsonConvert.DeserializeObject<JObject>(result); //data > Jobject

            return (JArray)jsondata["records"]["locations"][0]["location"]; //回傳陣列位址
        }
    }
}
