using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Collections.Concurrent;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

namespace IR_engine
{
    class Model : INotifyPropertyChanged
    {
        //concurrent variables
        static int i = 0;                                                           //use to index the queues in the list
        public static List<Dictionary<string, term>> queueList = new List<Dictionary<string, term>>();        //list of Queues 
        public static List<Dictionary<string, Location>> locsList = new List<Dictionary<string, Location>>();
        public static Dictionary<Location, Location> megaLocList = new Dictionary<Location, Location>();
        public static Dictionary<term, term> terms2 = new Dictionary<term, term>(); //the dictionary
        public static ConcurrentDictionary<int, document> docs = new ConcurrentDictionary<int, document>(); //holds doc names and <max TF, distinct, location>
        public static int cores = Environment.ProcessorCount;
        public static int fileCount = 0;
        public static List<string> locationsList = new List<string>();
        public static ConcurrentDictionary<string, byte> cityIn = new ConcurrentDictionary<string, byte>();
        public static ConcurrentDictionary<string, Location> locations = new ConcurrentDictionary<string, Location>();
        public static long avg_doc_size = 0;
        public static double avgDocsSize = 0;
        //end concurrent variables
        public Dictionary<int, string> elements = new Dictionary<int, string>();
        public static bool isWorking = false;
        public bool isDictionaryStemmed;
        Indexer indexer;
        ReadFile readfo;
        string path;
        bool ToStem;
        string IndexPath;
        string outPath;
        public double counter;
        public Dictionary<string, indexTerm> indexList;
        public bool toStem { get => ToStem; set => ToStem = value; }
        public string Path { get { return path; }
            set
            {
                path = value;
                if (!IndexPath1.Equals(""))
                {
                    indexer = new Indexer(IndexPath1, path);
                }
            }
        }
        public string IndexPath1 { get => IndexPath;
            set
            {
                IndexPath = value;
                if (!Path.Equals(""))
                {
                    indexer = new Indexer(IndexPath, Path);
                }
            }
        }

        public double Progress {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged("progressBar");
            }
        }

        private double progress;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public Model(string path, bool Tostem, string ipath)
        {
            readfo = new ReadFile(path, Tostem, cores);
            this.path = path;
            this.IndexPath = ipath;
            this.toStem = Tostem;
            string outPath = null;
            //parser = new Parse(path, ToStem);
            if (toStem) { if (!File.Exists(IndexPath1 + "\\EnableStem")) { Directory.CreateDirectory(IndexPath1 + "\\EnableStem"); }outPath = IndexPath1 + "\\EnableStem"; }
            else {if (!File.Exists(IndexPath1 + "\\DisableStem")) { Directory.CreateDirectory(IndexPath1 + "\\DisableStem"); }outPath = IndexPath1 + "\\DisableStem";}
            indexer = new Indexer(outPath, path+ "\\Posting_and_indexes");
            indexList = new Dictionary<string, indexTerm>();
        }
        public Model()
        {
            indexList = new Dictionary<string, indexTerm>();
            toStem = false;
            indexer = null;
            IndexPath = "";
            path = "";
            counter = 0;
        }
        /// <summary>
        /// this fucntion is incharge of all the logic operations in the engine (the parser and the index)
        /// </summary>
        public void index()
        {
            if(Path.Equals("") || IndexPath1.Equals(""))
            {
                MessageBox.Show("Invalid path");
                return;
            }

            Memorydump();
            counter = 0;
            Progress = 0;
            isWorking = true;
            readfo = new ReadFile(path, toStem, cores);
            if (toStem) { if (!File.Exists(IndexPath1 + "\\EnableStem")) { Directory.CreateDirectory(IndexPath1 + "\\EnableStem"); } outPath = IndexPath1 + "\\EnableStem"; }
            else { if (!File.Exists(IndexPath1 + "\\DisableStem")) { Directory.CreateDirectory(IndexPath1 + "\\DisableStem"); } outPath = IndexPath1 + "\\DisableStem"; }
            if (File.Exists(outPath + @"\documents.txt"))
                File.Delete(outPath + @"\documents.txt");
            if (File.Exists(outPath + @"\city_dictionary.txt"))
                File.Delete(outPath + @"\city_dictionary.txt");
            indexer = new Indexer(outPath, path + "\\Posting_and_indexes");

            if (!Directory.Exists(path + "\\cityIndex")) { Directory.CreateDirectory(path + "\\cityIndex"); }
            isDictionaryStemmed = toStem;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            isWorking = true;
            int filesNum = readfo.returnSize();
            List<Task> t;
            List<string> files = readfo.allfiles;               //gets the files list
            int tasks = cores;                                  //get the number of logical proccesors
            for (int ch = 0; ch < tasks; ch++)
            {                  //initialize the queues
                queueList.Add(new Dictionary<string, term>());
                locsList.Add(new Dictionary<string, Location>());
            }
            int k = 0, chunk = 0, id = 0;
            t = new List<Task>();
            createCityDic(files);
            var list = locations.Keys.ToList();
            list.Sort();
            using (StreamWriter ct = new StreamWriter(outPath + "\\city_dictionary.txt"))
            {
                foreach (var key in list)
                {
                    ct.WriteLine(key + "\t" + locations[key].Country + "\t" + locations[key].Currency + "\t" + locations[key].Population);
                }
            }

            foreach (string file in files)
            {
                int tsk = i % tasks;
                t.Add(Task.Factory.StartNew(() =>
                {
                    readfo.readfile(file, tsk);
                }));
                //readfo.readfile(file, tsk);
                id++;
                i++;
                k++;
                Progress = ((double)k / (double)files.Count) * 100;
                if (k % tasks == 0)
                {
                    foreach (Task ts in t)
                        ts.Wait();
                    if (k % (tasks * 5) == 0)
                    {
                        manageResources(outPath);
                        using (StreamWriter sw = new StreamWriter(Path + "\\Posting_and_indexes\\index" + chunk + ".txt"))
                        {
                            foreach (KeyValuePair<term, term> entry in terms2)
                            {
                                sw.WriteLine(entry.Key.ToString());
                            }
                        }
                        using (StreamWriter sw2 = new StreamWriter(path + "\\cityIndex\\city"+ chunk + ".txt"))
                        {
                            foreach (KeyValuePair<Location, Location> loc in megaLocList)
                            {
                                sw2.WriteLine(loc.Key.ToString());
                            }
                        }
                        terms2.Clear();
                        chunk++;
                    }
                    t = new List<Task>();
                }
            }
            {
                foreach (Task ts in t)
                    ts.Wait();
                Progress = 100;
                manageResources(outPath);
                using (StreamWriter sw = new StreamWriter(Path + "\\Posting_and_indexes\\index" + chunk + ".txt"))
                {
                    foreach (KeyValuePair<term, term> entry in terms2)
                    {
                        sw.WriteLine(entry.Key.ToString());
                    }
                }
                using( StreamWriter sw2 = new StreamWriter(path+ "\\cityIndex\\city" + chunk + ".txt"))
                {
                    foreach(KeyValuePair<Location,Location> loc in megaLocList)
                    {
                        sw2.WriteLine(loc.Key.ToString());
                    }
                }
                terms2.Clear();
                Console.WriteLine("{0} tasks done, total done: {1}", tasks, id);
                t = null;
                chunk++;
            }
            //readfo.Close();
            Progress = 0;
            indexList = indexer.CreateIndex();
            Directory.Delete(Path + "\\Posting_and_indexes");
            indexer.MergeLocations(path + @"\cityIndex");
            Directory.Delete(path + @"\cityIndex");
            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            MessageBox.Show("Done indexing the curpus!\nProccessing time: "+double.Parse(elapsedMs.ToString())/1000.0/60.0+"min\n"+
                double.Parse(elapsedMs.ToString()) / 1000.0+" sec\n"+
                "Terms Parsed: " +indexList.Count+"\nDocuments Parsed: "+counter, "BarvazBarvazGo");
            avgDocsSize = ((double)avg_doc_size) / (counter);
            //File.Move(IndexPath1 + @"\documents.txt", outPath + @"\documents.txt");
            //File.Move(IndexPath1 + @"\city_dictionary.txt", outPath + @"\city_dictionary.txt");
            using (StreamWriter lng = new StreamWriter(outPath + @"\languages.txt"))
            {
                var lngs = ReadFile.Langs.Keys.ToList();
                for (int i = 0; i < lngs.Count; i++)
                    lng.WriteLine(lngs[i]);
            }
            isWorking = false;
            OnPropertyChanged("done");
            //sw.Flush();
            //sw.Close();
        }
        /// <summary>
        /// funtion to free up the ram space by writing the accumulated values to the Disk
        /// </summary>
        public void manageResources(string outPath)
        {
            for (int i = 0; i < queueList.Count; i++)
            {
                term t = null;
                // merging all dictionaries into one
                foreach (KeyValuePair<string, term> entry in queueList[i])
                {
                    t = entry.Value;
                    if (t == null) break;
                    if (terms2.ContainsKey(t))
                    {
                        terms2[t].AddToPosting(t.posting);
                    }
                    else
                    {
                        terms2.Add(t, t);
                    }

                }
                queueList[i].Clear();
            }
            for(int i = 0; i < locsList.Count; i++)
            {
                Location l = null;
                foreach(KeyValuePair<string, Location> entry in locsList[i])
                {
                    l = entry.Value;
                    if (l == null) break;
                    if (megaLocList.ContainsKey(l))
                    {
                        megaLocList[l].addOccurs(l.LocationsInDocs);
                    }
                    else
                    {
                        megaLocList.Add(l, l);
                    }
                }
                locsList[i].Clear();
            }
            using (StreamWriter sw = new StreamWriter(outPath + "\\documents.txt", true))
            {
                foreach (KeyValuePair<int, document> entry in docs)
                {
                    counter++;
                    sw.WriteLine(entry.Key+"\t"+entry.Value.ToString());
                }
            }
            docs.Clear();
        }
        /// <summary>
        /// a getter of the indexTerm dictionary
        /// </summary>
        /// <returns>the dictionary of the terms </returns>
        public Dictionary<string, indexTerm> getDictionary()
        {
            return indexList;
        }
        /// <summary>
        /// this function loads the dictionary from the given index to the ram
        /// </summary>
        /// <param name="indexPath">the path of the index int the disk</param>
        /// <returns>the index as a dictionary data structure</returns>
        public Dictionary<string, indexTerm> load_index(string indexPath)
        {
            indexList.Clear();
            indexList = Indexer.Load_Index(indexPath);
            return indexList;
        }

        public List<string> load_location(string path)
        {
            locationsList = Indexer.Load_locs(path, toStem);
            return locationsList;
        }

        public List<string> load_langs(string path)
        {
            return Indexer.load_langs(path, toStem);
        }

        public Dictionary<int, string> loadElements(string indexPath)
        {
            string path = indexPath + "\\elements.txt";
            elements = Indexer.load_elements(path);
            return elements;
        }
        /// <summary>
        /// creates a dictionary of cities based a list of found cities ' is the posting list
        /// </summary>
        /// <param name="files">list of temp city files</param>
        public static void createCityDic( List<string> files)
        {
            List<Task> lst = new List<Task>();
            int tasks = cores;
            foreach (string fileRaw in files)
            {
                {
                    int tsk = i % tasks;
                    lst.Add(Task.Factory.StartNew(() =>
                    {
                        int idx = 0;
                        string DAfile = File.ReadAllText(fileRaw);
                        int end = 0, st = 0;
                        while (true)
                        {
                            st = DAfile.IndexOf("<F P=104>", idx);
                            if (st != -1)
                            {
                                end = DAfile.IndexOf("</F>", st, 100);
                            }
                            else { break; }
                            string city = "";
                            if (st != -1 && end != -1) { city = (DAfile.Substring(st + 9, (end - st) - 9)); idx = end; }
                            else { break; }
                            string[] fullname = city.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (fullname.Length < 1) continue;
                            string F = ReadFile.rmvStr(fullname[0]).ToLower();
                            if (!hasNum(F))
                            {
                                if (!cityIn.ContainsKey(F))
                                {
                                    cityIn.TryAdd(F, 0);
                                    ReadFile.addLocation(F);
                                }
                            }
                        }
                    }));
                    i++;
                    if (tsk == 0)
                    {
                        foreach (Task ts in lst)
                            ts.Wait();
                        lst.Clear();
                    }
                }
            }
        }
        /// <summary>
        /// boolean value whether the string contains a digit
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        static bool hasNum(string word)
        {
            for (int i = 0; i < word.Length; i++)
            {
                if (Char.IsDigit(word[i]) || Char.IsDigit(word[word.Length - i - 1]))
                    return true;
            }
            return false;
        }
        /// <summary>
        /// cleans all the unnedded and un relevent chars from the string
        /// </summary>
        /// <param name="word">the string to edit</param>
        /// <returns>the new cleaner string</returns>
        public static string cleanAll(string word)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
               
                char c = word[i];
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                    sb.Append(c);
                if(c==' ' && sb.Length > 0)
                {
                    return sb.ToString(); ;
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// cleans all the dictionaries in the program
        /// </summary>
        public void Memorydump()
        {
            i = 0;
            queueList.Clear();
            locsList.Clear();
            megaLocList.Clear();
            terms2.Clear();
            docs.Clear();
            cityIn.Clear();
            ReadFile.Langs.Clear();
            locations.Clear();
            Searcher.Clear();
        }
    }
}


