using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace IR_engine
{
    /// <summary>
    /// this is the indexer class, it is responsible to index the terms after generating them in the Parser class
    /// </summary>
    class Indexer
    {
        string ipath;
        string path;

        ConcurrentQueue<string> q = new ConcurrentQueue<string>();
        Dictionary<string, MaxList> elements = new Dictionary<string, MaxList>(); //dictionary of doc and 5 strongest elements
        Semaphore q_list = new Semaphore(0, 1000000);

        public Indexer(string ipath, string path)
        {
            this.ipath = ipath;             //path to the desired directory where the index will be stored
            this.path = path;               //path to the unsorted posting files (as Parser outputted)
        }

        /// <summary>
        /// this method creates an index file in the given directory and returns 
        /// a dictionary of terms and file pointer
        /// </summary>
   
        /// <returns> a dictionary of terms </returns>
        public Dictionary<string, indexTerm> CreateIndex()
        {
            SortPostings();
            return LoadIndex();
        }
        /// <summary>
        /// this class is incharge of returning a dictionary that represents the  index lodad from the index file
        /// </summary>
        /// <returns>the index in a dictionary data structure </returns>
        public Dictionary<string, indexTerm> LoadIndex()
        {
            return LoadIndex(ipath);
        }
        /// <summary>
        /// this class is incharge of returning a dictionary that represents the  index lodad from the index file
        /// given a specific path
        /// </summary>
        /// <param name="IndexPath">the path of the index file</param>
        /// <returns>the index in a dictionary data structure </returns>
        public Dictionary<string, indexTerm> LoadIndex(string IndexPath)
        {
            return Load_Index(IndexPath);
        }

        /// <summary>
        /// this method initiates the indexing process, manages the sorting algorithm and tasks
        /// and the merge
        /// </summary>
        private void SortPostings()
        {
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            List<Task> t = new List<Task>();
            for(int i = 0; i < 3; i++)
            {
                int k = i;
                t.Add(Task.Factory.StartNew(() => sort(k, files, path)));
            }
            foreach (Task tsk in t)
                tsk.Wait();
            for (int i = 0; i < files.Length; i++)
                File.Delete(files[i]);
            if (!Directory.Exists(ipath))
                Directory.CreateDirectory(ipath);
            Task t1 = new Task(()=>merge(files.Length, files));
            t1.Start();
            Task t2 = new Task(BuildElements);
            t2.Start();
            t1.Wait();
            q_list.Release(1);
            t2.Wait();
            using (StreamWriter sw = new StreamWriter(ipath + "\\elements.txt"))
            {
                foreach(KeyValuePair<string, MaxList> doc in elements)
                {
                    sw.WriteLine(doc.Key + '\t' + doc.Value.ToString());
                }
            }
        }

        /// <summary>
        /// this method sorts the temporal files by an ascending lexicographic order of the phrases
        /// </summary>
        /// <param name="offset">the serial number of the task</param>
        /// <param name="files">the files array</param>
        /// <param name="pathToPosting">the path to the temportal files</param>
        private void sort(int offset, string[] files, string pathToPosting)
        {
            for (int i = offset; i < files.Length; i += (3))
            {
                List<string> fileContent = File.ReadAllText(files[i]).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                fileContent = fileContent.OrderBy(s => s.Split(new string[] { "\t" }, StringSplitOptions.None)[0]).ToList();
                using (StreamWriter sr = new StreamWriter(pathToPosting + "\\index" + i + "sorted.txt"))
                {
                    foreach (string s in fileContent)
                        sr.WriteLine(s);
                }
                Console.WriteLine(i + " files sorted");
            }
        }
        
        /// <summary>
        /// this method efficiently merge the newly created sorted files into posting files and
        /// creates the index in the desired index path
        /// </summary>
        /// <param name="fileCount">number of files</param>
        /// <param name="files">files array</param>
        private void merge(int fileCount, string[] files)
        {
            bool MoreToRead = false;
            string[] firstLines = new string[fileCount];
            string[] sortedFirstLines;
            StreamReader[] sr = new StreamReader[fileCount];
            Dictionary<string, StreamWriter> writers = new Dictionary<string, StreamWriter>();
            for (int i = 0; i < fileCount; i++)
            {
                sr[i] = new StreamReader(path + "\\index" + i + "sorted.txt");
            }
            for (char c = 'A'; c <= 'Z'; c++)
            {
                writers.Add(c + "", new StreamWriter(ipath + "\\" + c + ".txt"));
            }
            foreach (term.Type t in Enum.GetValues(typeof(term.Type)))
            {
                if (t == term.Type.word) continue;
                writers.Add(t.ToString(), new StreamWriter(ipath + "\\" + t.ToString() + ".txt"));
            }
            writers.Add("other", new StreamWriter(ipath + "\\other.txt"));
            writers.Add("index", new StreamWriter(ipath + "\\index.txt"));
            for (int i = 0; i < fileCount; i++)
            {
                firstLines[i] = sr[i].ReadLine();
                if (firstLines[i] == null)
                    firstLines[i] = "\0";
                if (firstLines[i] == "")
                    i--;
                if (firstLines[i] != null)
                    MoreToRead = true;
            }
            int lastIndex = 0;
            StringBuilder minLine = new StringBuilder();
            StringBuilder minPhrase = new StringBuilder();
            while (lastIndex < firstLines.Length)
            {
                minPhrase.Clear();
                minLine.Clear();
                int i = lastIndex;
                sortedFirstLines = firstLines.OrderBy(s => s.Split(new string[] { "\t" }, StringSplitOptions.None)[0]).ToArray();
                for (i = 0; i < sortedFirstLines.Length; i++)
                {
                    if (sortedFirstLines[i].Equals("\0")) continue;
                    else break;
                }
                if (i >= sortedFirstLines.Length) break;
                minPhrase.Append(GetPhrase(sortedFirstLines[i]));
                bool Cap = true;
                double icf = 0, idf = 0;
                term.Type type = GetType(sortedFirstLines[i]);
                for (i = 0; i < sortedFirstLines.Length; i++)
                {
                    if (sortedFirstLines[i].Equals("\0")) continue;
                    if (string.Compare(minPhrase.ToString(), GetPhrase(sortedFirstLines[i]), true) == 0)
                    {
                        string[] splitted = sortedFirstLines[i].Split('\t');
                        if (type != (term.Type)Enum.Parse(typeof(term.Type), splitted[2], true)) continue;
                        Cap &= splitted[1].Equals("T") ? true : false;
                        minLine.Append(splitted[3]);
                        icf += double.Parse(splitted[4]);
                        idf += double.Parse(splitted[5]);
                    }
                    else break;
                }
                string termPhrase = "";
                if (Cap) termPhrase = minPhrase.ToString().ToUpper();
                else termPhrase = minPhrase.ToString().ToLower();
                if (type == term.Type.word)
                {
                    if (char.IsLetter(minPhrase[0]))
                    {
                        writers[Char.ToUpper(minPhrase[0]).ToString()].WriteLine(termPhrase + "\t" + minLine.ToString());
                        if(Cap)
                        {
                            q.Enqueue(termPhrase + "\t" + minLine.ToString());
                            q_list.Release(1);
                        }
                    }
                    else
                    {
                        writers["other"].WriteLine(termPhrase + "\t" + minLine.ToString());
                    }
                }
                else
                {
                    writers[type.ToString()].WriteLine(termPhrase + "\t" + minLine.ToString());
                }
                for (i = 0; i < fileCount; i++)
                {
                    if (string.Compare(minPhrase.ToString(), GetPhrase(firstLines[i]), true) == 0)
                    {
                        if (type != GetType(firstLines[i])) continue;
                        if (firstLines[i].Equals("\0")) continue;
                        firstLines[i] = sr[i].ReadLine();
                        if (firstLines[i] == null)
                            firstLines[i] = "\0";
                        while (firstLines[i] == "")
                        {
                            firstLines[i] = sr[i].ReadLine();
                            if (firstLines[i] == null)
                                firstLines[i] = "\0";
                        }
                    }
                }
                writers["index"].WriteLine(termPhrase + "\t" + (int)type + "\t" + icf + "\t" + idf);
            }
            for (int i = 0; i < fileCount; i++)
            {
                sr[i].Close();
                File.Delete(path + "\\index" + i + "sorted.txt");
            }
            foreach (KeyValuePair<string, StreamWriter> entry in writers)
            {
                entry.Value.Close();
            }
        }

        private void BuildElements()
        {
            int i = 0;
            while(true)
            {
                if (i == 1000000) break;
                q_list.WaitOne();
                //Thread.Sleep(10);
                string element = "";
                try
                {
                    q.TryDequeue(out element);
                }
                catch(Exception e) { break; }
                if (element == null) break;
                string[] splitted = element.Split('\t');
                string[] posting = splitted[1].Split(',');
                for(int post = 0; post < posting.Length-1; post++)
                {
                    string[] doc_tf = posting[post].Split('_');
                    if(!elements.ContainsKey(doc_tf[0]))
                    {
                        MaxList m = new MaxList();
                        m.add(new KeyValuePair<string, int>(splitted[0], int.Parse(doc_tf[1])));
                        elements.Add(doc_tf[0], m);
                    }
                    else
                    {
                        elements[doc_tf[0]].add(new KeyValuePair<string, int>(splitted[0], int.Parse(doc_tf[1])));
                    }
                }
                i++;
            }
        }
        /// <summary>
        /// function as a getter to the phrase of the term
        /// </summary>
        /// <param name="line">the line of the term toString</param>
        /// <returns>the phrase</returns>
        private string GetPhrase(string line)
        {
            return line.Split('\t')[0];
        }
        /// <summary>
        /// function as a getter to the type of the term
        /// </summary>
        /// <param name="line">the line of the term toString</param>
        /// <returns>the type</returns>
        private term.Type GetType(string line)
        {
            string type = line.Split('\t')[2];
            term.Type e = (term.Type)Enum.Parse(typeof(term.Type), type, true);
            return e;
        }
        /// <summary>
        /// this function return the index as a dictionary data structure from the file
        /// </summary>
        /// <param name="path">the path where the index is saved</param>
        /// <returns>the index as a dictionary data structure</returns>
        public static Dictionary<string, indexTerm> Load_Index(string path)
        {
            Dictionary<string, indexTerm> index = new Dictionary<string, indexTerm>();
            List<string> termsList = new List<string>();
            termsList = File.ReadAllText(path + "\\index.txt").Split('\n').ToList();
            for (int i = 0; i < termsList.Count - 1; i++)
            {
                indexTerm t;
                string[] entry = termsList[i].Split('\t');
                t = new indexTerm(entry[0], (term.Type)Enum.Parse(typeof(term.Type), entry[1], true));
                if (entry.Length < 2) continue;
                if (!index.ContainsKey(entry[0] + entry[1]))
                {
                    index.Add(entry[0] + entry[1], t);
                }
            }
            return index;
        }

        public static List<string> Load_locs(string path, bool isStem)
        {
            string line;
            List<string> locations = new List<string>();
            using (StreamReader sr = new StreamReader(path + @"\city_dictionary.txt"))
            {
                while ((line = sr.ReadLine()) != null)
                { locations.Add(line.Split('\t')[0]); }
            }
            return locations;
        }

        public static List<string> load_langs(string path, bool isStem)
        {
            string line;
            List<string> locations = new List<string>();
            using (StreamReader sr = new StreamReader(path + @"\languages.txt"))
            {
                while ((line = sr.ReadLine()) != null)
                { locations.Add(line.Split('\t')[0]); }
            }
            return locations;
        }

        public static Dictionary<int, string> load_elements(string path)
        {
            string line = "";
            Dictionary<int, string> elements = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(path))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Equals(" ")) continue;
                    string[] splitted = line.Split('\t');
                    elements.Add(int.Parse(splitted[0]), splitted[1]);
                }
            }
            return elements;
        }
        /// <summary>
        /// sorts an city temp index acoording to the lexicographic order
        /// </summary>
        /// <param name="offset">the row which is sorted</param>
        /// <param name="files">files to be sorted</param>
        /// <param name="pathToPosting">where to save the new posting list</param>
        private void sortCity(int offset, string[] files, string pathToPosting)
        {
            for (int i = offset; i < files.Length; i += (3))
            {
                List<string> fileContent = File.ReadAllText(files[i]).Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
                fileContent = fileContent.OrderBy(s => s.Split(new string[] { "\t" }, StringSplitOptions.None)[0]).ToList();
                using (StreamWriter sr = new StreamWriter(pathToPosting + "\\City" + i + "sorted.txt"))
                {
                    foreach (string s in fileContent)
                        sr.WriteLine(s);
                }
            }
        }
        /// <summary>
        /// this function mergrs the location temp files
        /// </summary>
        /// <param name="path">the path of files to merge</param>
        public void MergeLocations(string path)
        {

            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            int fileCount = files.Length;
            List<Task> t = new List<Task>();
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                t.Add(Task.Factory.StartNew(() => sortCity(k, files, path)));
            }
            foreach (Task tsk in t)
                tsk.Wait();
            for (int i = 0; i < files.Length; i++)
                File.Delete(files[i]);

            string[] firstLines = new string[fileCount];
            string[] sortedFirstLines;
            StreamReader[] sr = new StreamReader[fileCount];
            StreamWriter sw = new StreamWriter(ipath + "\\City.txt");
            for (int i = 0; i < fileCount; i++)
            {
                sr[i] = new StreamReader(path + "\\City" + i + "sorted.txt");
            }
            for (int i = 0; i < fileCount; i++)
            {
                firstLines[i] = sr[i].ReadLine();
                if (firstLines[i] == null)
                    firstLines[i] = "\0";
                if (firstLines[i] == "")
                    i--;
            }
            int lastIndex = 0;
            StringBuilder minLine = new StringBuilder();
            StringBuilder minPhrase = new StringBuilder();
            while (lastIndex < firstLines.Length)
            {
                minPhrase.Clear();
                minLine.Clear();
                int i = lastIndex;
                sortedFirstLines = firstLines.OrderBy(s => s.Split(new string[] { "\t" }, StringSplitOptions.None)[0]).ToArray();
                for (i = 0; i < sortedFirstLines.Length; i++)
                {
                    if (sortedFirstLines[i].Equals("\0")) continue;
                    else break;
                }
                if (i >= sortedFirstLines.Length) break;
                minPhrase.Append(GetPhrase(sortedFirstLines[i]));
                for (i = 0; i < sortedFirstLines.Length; i++)
                {
                    if (string.Compare(minPhrase.ToString(), GetPhrase(sortedFirstLines[i]), true) == 0)
                    {
                        string[] splitted = sortedFirstLines[i].Split('\t');
                        minLine.Append(splitted[1]);
                    }
                    else break;
                }
                sw.WriteLine(minPhrase.ToString() + '\t' + minLine.ToString());
                for (i = 0; i < fileCount; i++)
                {
                    if (string.Compare(minPhrase.ToString(), GetPhrase(firstLines[i]), true) == 0)
                    {
                        if (firstLines[i].Equals("\0")) continue;
                        firstLines[i] = sr[i].ReadLine();
                        if (firstLines[i] == null)
                            firstLines[i] = "\0";
                        while (firstLines[i] == "")
                        {
                            firstLines[i] = sr[i].ReadLine();
                            if (firstLines[i] == null)
                                firstLines[i] = "\0";
                        }
                    }
                }
            }
            for (int i = 0; i < fileCount; i++)
            {
                sr[i].Close();
                File.Delete(path + "\\City" + i + "sorted.txt");
            }
            sw.Flush();
            sw.Close();
        }
    }
}
