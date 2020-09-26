using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IR_engine;
using System.Net.Http;
using System.Web;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Diagnostics;


namespace IR_engine
{
    /// <summary>
    /// this is the ReadFile class, this class is incharge of reading the documents
    /// from the given corpus path and sends it to the Parser class
    /// </summary>
    public class ReadFile
    {
        public List<string> allfiles = null;
        bool ToStem;
        int index = 0;
        static HttpClient http = new HttpClient();
        int allfilesSize = 0;
        string path;
        Parse[] parser;
        int cores;
        public static HashSet<char> fixHash = new HashSet<char>() { '\"', ']', '}', '[', '{','(',')',' '};
        public static ConcurrentDictionary<string, byte> Langs = new ConcurrentDictionary<string, byte>();
        private static Mutex m = new Mutex();
        private static int DocIndex = 0;

        public ReadFile(string path, bool ToStem, int cores)
        {
            this.cores = cores;
            this.path = path;
            this.ToStem = ToStem;
            initiate();
        }

        private void initiate()
        {
            allfiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Where(x => Path.GetExtension(x) != ".txt").ToList();
            allfilesSize = allfiles.Count;
            parser = new Parse[cores];
            for (int i = 0; i < cores; i++)
                parser[i] = new Parse(path, ToStem);
            string root = path + @"\Posting_and_indexes";
            // If directory does not exist, don't even try 
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }
        }
        /// <summary>
        /// this function create an document type objects from the string file, and sends it to the parser
        /// 
        /// </summary>
        /// <param name="file"> the string that constains all the data in the file</param>
        public void readfile(string file2, int queue)
        {
            string file = File.ReadAllText(file2);
            string[] docs = file.Split(new string[] { "<DOC>" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string doc in docs)
            {
                int st1 = 0, st2 = 0, st3 = 0, st4 = 0, st5 = 0, end1 = 0, end2 = 0, end3 = 0, end4 = 0, end5 = 0,st6 = 0,end6 = 0;
                if (doc.Equals("")) continue;
                string docNo = "";
                string date = "";
                string head = "";
                string data = "";
                if (doc != "\n")
                {
                    st1 = doc.IndexOf("<DOCNO>");
                    if (st1 != -1)
                        end1 = doc.IndexOf("</DOCNO>", st1);
                    if (st1 != -1 && end1 != -1) { docNo = doc.Substring(st1 + 7, (end1 - st1) - 7); }
                    st2 = doc.IndexOf("<DATE1>");
                    if (st2 != -1)
                        end2 = doc.IndexOf("</DATE1>", st2, 50);
                    if (st2 != -1 && end2 != -1) { date = doc.Substring(st2 + 7, (end2 - st2) - 7); }
                    st3 = doc.IndexOf("<TI>");
                    if (st3 != -1) end3 = doc.IndexOf("</TI>", st3, 100);
                    if (st3 != -1 && end3 != -1) { head = doc.Substring(st3 + 4, (end3 - st3) - 3); }
                    st4 = doc.IndexOf("<TEXT>");
                    if (st4 != -1) end4 = doc.IndexOf("</TEXT>", st4);
                    if (st4 != -1 && end4 != -1) { data = doc.Substring(st4 + 6, (end4 - st4) - 6); }
                    st5 = doc.IndexOf("<F P=104>");
                    if (st5 != -1)
                    { 
                        end5 = doc.IndexOf("</F>", st5, 100);
                    }
                    string city = "";
                    if (st5 != -1 && end5 != -1) { city = (doc.Substring(st5 + 9, (end5 - st5) - 9)); }
                    st6 = doc.IndexOf("<F P=105>");
                    if(st6 != -1)
                    {
                        end6 = doc.IndexOf("</F>", st6, 100);
                    }
                    string language = "";
                    if(st6!=-1 && end6 != -1) { language = doc.Substring(st6 + 9, (end6 - st6) - 9); }
                    Langs.TryAdd(language, 0);
                    string[] fullname = city.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    document d = null;
                    if (fullname.Length < 1)
                        d = new document(data, docNo, date, head, "");
                    else
                    {
                        d = new document(data, docNo, date, head, fullname[0]);
                    }
                    m.WaitOne();
                    d.DocIndex1 = DocIndex++;
                    m.ReleaseMutex();
                    Model.docs.TryAdd(d.DocIndex1, d);
                    parser[queue].Text2list(d, queue);
                }
            }
        }

        /// <summary>
        /// creates a location object and adds the location to the dictionary
        /// </summary>
        /// <param name="city">the name of the location city</param>
        public static void addLocation(string city)
        {
            string city2= city;

            if (city.Equals("the")) return;
            if (Parse.hasChar(city, '/'))
            {
                city2 = city.Substring(0, city.IndexOf('/'));
            }
            string country = null; string pop = null; string cap = null; string curr = null;
            string Firsturl = "https://restcountries.eu/rest/v2/capital/" + city2 + "?fields=name;capital;population;currencies";
            HttpResponseMessage response1 = http.GetAsync(new Uri(Firsturl)).Result;
            string responseBody1 = response1.Content.ReadAsStringAsync().Result;
            if (responseBody1.Equals("{\"status\":404,\"message\":\"Not Found\"}"))
            {
                var webreq = WebRequest.Create("http://getcitydetails.geobytes.com/GetCityDetails?fqcn=" + city2);
                if (webreq != null)
                {
                    using (var s = webreq.GetResponse().GetResponseStream())
                    {
                        using (var sr = new StreamReader(s))
                        {
                            var Nson = sr.ReadToEnd();
                            JObject obj = JObject.Parse(Nson);
                            curr = (string)obj.SelectToken("geobytescurrency");
                            country = (string)obj.SelectToken("geobytescountry");
                            pop = (string)obj.SelectToken("geobytespopulation");
                            cap = (string)obj.SelectToken("geobytescapital");
                        }
                    }
                }
                if (curr == "" && country == "" && pop == "" && cap == "")
                {
                    Location l0 = new Location(city, "*", "0", "*", "*");
                    Model.locations.TryAdd(city, l0);
                }
                else
                {
                    Location l = new Location(city, country, pop, curr, cap);
                    Model.locations.TryAdd(city, l);
                }

            }
            else
            {
                string[] arr1 = responseBody1.Remove(responseBody1.Length - 1).Split(']');
                string datas = rmvStr(arr1[1]);
                string[] data1 = datas.Split(',');

                for (int i =0; i < data1.Length; i++)
                {
                    string str = data1[i];
                    if (str.Contains("name"))
                    {
                        country = str.Substring(str.IndexOf(':') + 1);
                    }
                    else if (str.Contains("population"))
                    {
                        pop = str.Substring(str.IndexOf(':') + 1);
                    }
                    else if (str.Contains("capital"))
                    {
                        cap = str.Substring(str.IndexOf(':') + 1);
                                }
                    }
                    datas = rmvStr(arr1[0]);
                  data1 = datas.Split(','); 
                    for (int i = 0; i < data1.Length; i++)
                    {
                    if (data1[i].Contains("name"))
                        {
                            curr = data1[i].Substring(data1[i].IndexOf(':') + 1);
                            break;
                        }
                    }
                    Location l = new Location(city, country, pop, curr, cap);
                    Model.locations.TryAdd(city, l);
                }
            }
        /// <summary>
        /// getter to the number of files
        /// </summary>
        /// <returns></returns>
        public int returnSize()
        {
            return allfilesSize;
        }
        /// <summary>
        /// cleans a city according to a specifit delimiters
        /// </summary>
        /// <param name="input">the string to clean</param>
        /// <returns>the cleand string</returns>
        public static string rmvStr(string input)
        {
            StringBuilder stb = new StringBuilder();
            for(int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (!fixHash.Contains(c))
                {
                    stb.Append(c);
                }
            }
            return stb.ToString();
        }
        /// <summary>
        /// clears all the local dictionaries
        /// </summary>
        public void Clear()
        {
            allfiles = null;
            index = 0;
            http = new HttpClient();
            allfilesSize = 0;
            parser = null;
            Langs = new ConcurrentDictionary<string, byte>();
        }
    }
}
