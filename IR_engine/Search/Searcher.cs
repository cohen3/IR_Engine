using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Word2vec.Tools;
using System.Collections.Concurrent;
using System.Threading;
using Word2Vec.Net;
using System.ComponentModel;

namespace IR_engine
{
    class Searcher : INotifyPropertyChanged
    {
        HashSet<string> stopwords = new HashSet<string>() {
            "a","an","and","also","all","are","as","at","be","been","by","but","for","from","-", "document", "issue", "issues", "required", "those",
            "have","has","had","he","in","is","it","its", "not","more","new","what", "where", "discuss", "discussing", "and/or", "other", "while",
            "which", "how", "who", "of","on","page","part","that","the","this", "regard", "regarding", "considered", "and/or,", "would", "should",
            "to","s","was","were","will","with", "documents", "i.e.", "i.e.,", ",", "?","etc", "information", "available", "there", "their", "associated"};
        private static Dictionary<string, KeyValuePair<int, term.Type>> currentKeywords = new Dictionary<string, KeyValuePair<int, term.Type>>();
        public static List<KeyValuePair<string, term.Type>> parsed = new List<KeyValuePair<string, term.Type>>();
        ConcurrentDictionary<int, string> titles = new ConcurrentDictionary<int, string>();
        public Dictionary<int, List<string>> rdocs = new Dictionary<int, List<string>>();
        Mutex m = new Mutex();
        public static Dictionary<int, string> Index2Doc = new Dictionary<int, string>();
        Dictionary<string, indexTerm> index;
        bool Semantics;
        static bool toStem;
        Ranker ranker;
        string indexPath;
        string queryPath;
        string modelPath;
        string outputFile;
        Vocabulary vocabulary;
        Word2Vec.Net.Distance distance;
        private double progress;

        public double Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged("query");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public Searcher(string indexPath, string queryPath, bool Semantics, string modelPath, bool toStemm, string outputFile)
        {
            this.outputFile = outputFile;
            this.indexPath = indexPath;
            this.queryPath = queryPath;
            this.Semantics = Semantics;
            this.modelPath = modelPath;
            this.ranker = new Ranker(indexPath, toStemm);
            try
            {
                vocabulary = new Word2VecBinaryReader().Read(modelPath);
                distance = new Word2Vec.Net.Distance(modelPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown model");
                vocabulary = null;
            }
            Index2Doc.Clear();
            using (StreamReader city = new StreamReader(indexPath + "\\documents.txt"))
            {                                   //locations list is a list of not desired locations
                string line = "";
                while ((line = city.ReadLine()) != null)
                {
                    string[] splitted = line.Split('\t');
                    Index2Doc.Add(int.Parse(splitted[0]), splitted[1]);
                }
            }
            toStem = toStemm;
        }

        public void loadIndex(Dictionary<string, indexTerm> index)
        {
            this.index = index;
        }

        public Searcher(bool semantics, string modelPath, string indexPath)
        {
            Semantics = semantics;
            this.indexPath = indexPath;
            this.modelPath = modelPath;
            this.queryPath = "";
        }

        /// <summary>
        /// will search and recover all docs that may be relevant to the queries
        /// </summary>
        /// <param name="toStem"></param>
        /// <returns></returns>
        public Dictionary<int, List<string>> Search(HashSet<string> locations, bool allLocs)
        {
            Clear();
            List<string> title = new List<string>();
            Dictionary<int, Dictionary<string, double>> weightsPerQuery = new Dictionary<int, Dictionary<string, double>>();
            // dictionary of <queryID, dictionary of <parsed query, <occrences, type>>>
            Dictionary<int, Dictionary<string, KeyValuePair<int, term.Type>>> parsedQueires = parseAllQueires(weightsPerQuery);
            // dictionary of <queryID, list of <document, it's rank>>
            ConcurrentDictionary<int, List<KeyValuePair<string, double>>> ranks =
                new ConcurrentDictionary<int, List<KeyValuePair<string, double>>>();
            HashSet<string> docs = new HashSet<string>();

            using (StreamReader city = new StreamReader(indexPath + "\\documents.txt"))
            {                                   //locations list is a list of not desired locations
                string line = "";
                while ((line = city.ReadLine()) != null)
                {
                    string[] splitted = line.Split('\t');
                    if (allLocs)
                        docs.Add(splitted[0]);  //change 0 to 1 if needed name and not index
                    else
                    {
                        if (!splitted[5].Equals("") && locations.Contains(splitted[5].ToLower()))
                            docs.Add(splitted[0]);  //change 0 to 1 if needed name and not index
                    }
                }
            }
            Parallel.ForEach(parsedQueires, (q) =>
            {
                // dictionary of terms and weights
                m.WaitOne();
                if (Semantics)
                {
                    var recWords = GetSimilarToSentence(titles[q.Key], weightsPerQuery[q.Key]);
                    foreach (KeyValuePair<string, int> w in recWords)
                    {
                        if (q.Value.ContainsKey(w.Key))
                        {
                            int num = q.Value[w.Key].Key;
                            term.Type t = q.Value[w.Key].Value;
                            q.Value[w.Key] = new KeyValuePair<int, term.Type>(w.Value + num, t);
                        }
                        else
                        {
                            foreach (KeyValuePair<string, term.Type> e in parsed)
                                if (e.Key.Equals(w.Key))
                                {
                                    q.Value.Add(w.Key, new KeyValuePair<int, term.Type>(w.Value, e.Value));
                                    parsed.Remove(e);
                                    break;
                                }
                        }
                    }
                }
                parsed.Clear();
                this.Progress += (0.5 / parsedQueires.Keys.Count);
                m.ReleaseMutex();
                // LocationName \t doc | loc1 | loc2 | , doc | loc1 | loc2 | , .....
                q.Value.Remove("relevant");
                ranks.TryAdd(q.Key, ranker.rank(q.Value, docs, weightsPerQuery[q.Key], !allLocs));
                m.WaitOne();
                this.Progress += (0.5 / parsedQueires.Keys.Count);
                m.ReleaseMutex();
            });
            var items = from pair in ranks
                        orderby pair.Key ascending
                        select pair;
            Dictionary<int, List<string>> RelevantDocs = new Dictionary<int, List<string>>();
            using (StreamWriter sw = new StreamWriter(this.outputFile))
            {
                foreach (KeyValuePair<int, List<KeyValuePair<string, double>>> ret in items)
                {
                    if (ret.Value == null) continue;
                    int num = 0;
                    foreach (KeyValuePair<string, double> DocRank in ret.Value)
                    {
                        if (num >= 50 || DocRank.Value == 0) break;
                        sw.WriteLine(ret.Key + " "
                                        + 0 + " "
                                        + Index2Doc[int.Parse(DocRank.Key)].Replace(" ", "") + " "
                                        + num + " "
                                        + DocRank.Value + " "
                                        + "run");
                        if (RelevantDocs.ContainsKey(ret.Key))
                        {
                            RelevantDocs[ret.Key].Add(DocRank.Key);
                        }
                        else
                        {
                            List<string> l = new List<string>();
                            l.Add(DocRank.Key);
                            RelevantDocs.Add(ret.Key, l);
                        }
                        num++;
                    }
                }
            }
            //using (StreamWriter sw = new StreamWriter(@"D:\curpus\actual\index\WordsPerQueryStemNoSemantics_2.txt"))
            //{
            //    foreach (var entry in parsedQueires)
            //    {
            //        sw.Write(entry.Key + "\t");
            //        foreach (var entryInQuery in entry.Value)
            //        {
            //            sw.Write(entryInQuery.Key + ",");
            //        }
            //        sw.Write("\r\n");
            //    }
            //}
            rdocs = RelevantDocs;
            OnPropertyChanged("done");
            return RelevantDocs;
        }

        private Dictionary<string, int> GetSimilarToSentence(string sentence, Dictionary<string, double> weights)
        {
            StringBuilder sb = new StringBuilder();
            Dictionary<string, int> vals = new Dictionary<string, int>();
            //for(int i = 0; i < sentence.Count; i++)
            //{
            //    if (i == sentence.Count - 1)
            //        sb.Append(sentence[i].ToLower());
            //    else
            //        sb.Append(sentence[i].ToLower()+" ");
            //}
            //var result = distance.Search(sb.ToString());
            var result = distance.Search(sentence);
            int j = 0;
            foreach (var bestWord in result.Where(x => !string.IsNullOrEmpty(x.Word)))
            {
                if (j == 5 || bestWord.Distance < 0.91) break;
                vals.Add(bestWord.Word, 1);
                if (weights.ContainsKey(bestWord.Word))
                {
                    weights[bestWord.Word] += 0.1;
                }
                else
                    weights.Add(bestWord.Word, bestWord.Distance);
                j++;
            }

            new Parse("", toStem).parseText(vals.Keys.ToArray(), -1);
            return vals;
        }

        private Dictionary<string, int> GetSimilar(Dictionary<string, KeyValuePair<int, term.Type>> words)
        {
            if (vocabulary == null) return null;
            Dictionary<string, int> recommendedWords = null;
            recommendedWords = new Dictionary<string, int>();
            HashSet<string> commonWords = new HashSet<string>();
            foreach (KeyValuePair<string, KeyValuePair<int, term.Type>> word in words)
            {
                //gets the vector (size 20) of the word
                var dist = vocabulary.Distance(word.Key, 20);
                //build vocabulary to find duplicates, duplicates gets inside the recommended list
                for (int i = 0; i < dist.Length; i++)
                {
                    string w = dist[i].Representation.WordOrNull;
                    if (commonWords.Contains(w))
                    {
                        if (!recommendedWords.ContainsKey(w))
                            recommendedWords.Add(w, 1);
                        else
                            recommendedWords[w]++;
                    }
                    else
                    {
                        commonWords.Add(w);
                    }
                }
                //adding first 5 (if any) terms with the highest probability according to the vector
                //willl search them anyway because they have the highest probability
                for (int i = 0; i < dist.Length; i++)
                {
                    if (i == 5) break;
                    if (!recommendedWords.ContainsKey(dist[i].Representation.WordOrNull))
                        recommendedWords.Add(dist[i].Representation.WordOrNull, 1);
                    else
                        recommendedWords[dist[i].Representation.WordOrNull]++;
                }
            }
            parsed.Clear();
            new Parse("", toStem).parseText(commonWords.ToArray(), -1);
            return recommendedWords;
        }

        /*
         *        
         *        distance between 2 vectors

            var vocabulary = new Word2VecBinaryReader().Read(outputFileName);
            while (true)
            {
                Console.WriteLine("distance of 2 word vector:");
                string word1 = Console.ReadLine();
                string word2 = Console.ReadLine();
                float[] vector1 = vocabulary[word1].NumericVector;
                float[] vector2 = vocabulary[word2].NumericVector;
                double dist = 0;
                for (int i = 0; i < vector2.Length; i++)
                    dist += Math.Pow(vector1[i] - vector2[i], 2);
                dist = Math.Sqrt(dist);
                Console.WriteLine("Distance is: " + dist);
            }
         */

        private Dictionary<int, Dictionary<string, KeyValuePair<int, term.Type>>> parseAllQueires(
            Dictionary<int, Dictionary<string, double>> weightsPerQuery
            )
        {
            Dictionary<int, Dictionary<string, KeyValuePair<int, term.Type>>> qs =
                new Dictionary<int, Dictionary<string, KeyValuePair<int, term.Type>>>();
            List<string> titleq = new List<string>();
            List<string> content = File.ReadAllLines(queryPath).ToList();
            int currentID = 0;
            string last = "";
            StringBuilder narr = new StringBuilder();
            foreach (var line in content)
            {
                // start of topic
                if (line.StartsWith("<num>"))
                {
                    currentID = int.Parse(line.Substring(14));
                    if (currentID == 351)
                        Console.WriteLine();
                    weightsPerQuery.Add(currentID, new Dictionary<string, double>());
                }
                else if (line.StartsWith("<title>"))
                {
                    ParseLine(line.Substring(8), "title", weightsPerQuery[currentID]);
                    if (Semantics)
                    {
                        StringBuilder titleString = new StringBuilder();
                        foreach (string word in currentKeywords.Keys)
                            titleString.Append(word + " ");
                        titles.TryAdd(currentID, titleString.ToString().TrimEnd(' '));
                    }

                }
                else if (line.StartsWith("<desc>"))
                {
                    last = "desc";
                }
                else if (line.StartsWith("<narr>"))
                {
                    last = "narr";
                }

                // end of topic
                else if (line.StartsWith("</top>"))
                {
                    string[] narrLines = narr.ToString().Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string l in narrLines)
                    {
                        //string l2 = l.Replace("not", "");
                        string[] rel = l.Split(new string[] { "not relevant", "non-relevant" }, StringSplitOptions.None);
                        if (rel.Length > 1)
                        {
                            foreach (string splitted in rel)
                            {
                                if (splitted.Contains("relevant"))
                                {
                                    ParseLine(splitted.Replace("relevant", ""), "narr", weightsPerQuery[currentID]);
                                }
                                else
                                {
                                    ParseLine(splitted.Replace("relevant", ""), "negative", weightsPerQuery[currentID]);
                                }

                            }
                        }
                        else
                            ParseLine(rel[0].Replace("relevant", ""), "narr", weightsPerQuery[currentID]);
                    }
                    //if (matching != null && Semantics)
                    //{
                    //    foreach (string s in matching)
                    //        ParseLine(s);
                    //}
                    qs.Add(currentID, currentKeywords);
                    currentID = 0;
                    currentKeywords = new Dictionary<string, KeyValuePair<int, term.Type>>();
                    narr.Clear();
                }
                else
                {
                    if (last.Equals("desc"))
                    {
                        ParseLine(line, "desc", weightsPerQuery[currentID]);
                    }
                    else if (last.Equals("narr"))
                    {
                        //ParseLine(line, toStem);
                        if (line.Contains("<top>")) continue;
                        narr.Append(ProccessLine(line) + " ");
                    }
                }
            }
            return qs;
        }

        private string ProccessLine(string line)
        {
            string[] words = line.Split(new string[] { " ", "(", ")", "," }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < words.Length; i++)
                sb.Append(ProcessWord(words[i]) + " ");
            return sb.ToString();
        }

        private void ParseLine(String line, string section, Dictionary<string, double> weights)
        {
            Parse p = new Parse("", toStem);
            p.parseText(line.Split(' '), -1);
            foreach (var pair in parsed)
            {
                string word = pair.Key;
                if (!string.IsNullOrWhiteSpace(word) && !(word[0] == '<'))
                {
                    String result = ProcessWord(word);
                    if (!result.Equals(""))
                    {
                        if (!weights.ContainsKey(result))
                        {
                            if (section.Equals("desc"))
                                weights.Add(result, 3.5);
                            else if (section.Equals("narr"))
                                weights.Add(result, 2);
                            else if (section.Equals("title"))
                                weights.Add(result, 10);
                            else if (section.Equals("negative"))
                                weights.Add(result, 0.5);
                        }
                        else
                        {
                            if (section.Equals("desc"))
                                weights[result] += 0.2;
                            else if (section.Equals("narr"))
                                weights[result] += 0.1;
                            else if (section.Equals("title"))
                                weights[result] += 1;
                            else if (section.Equals("negative"))
                                weights[result] -= 0.09;
                        }
                        if (currentKeywords.ContainsKey(result))
                        {
                            int occ = currentKeywords[result].Key;
                            currentKeywords[result] = new KeyValuePair<int, term.Type>(occ + 1, pair.Value);
                        }
                        else
                        {
                            currentKeywords.Add(result, new KeyValuePair<int, term.Type>(1, pair.Value));
                        }
                    }
                }
            }
            parsed.Clear();
        }

        private String ProcessWord(String word)
        {
            String workingCopy = word.ToLower();
            // check if stemmed is stop word, do not search complete list if not necessary
            if ((workingCopy.Length <= 4) || stopwords.Contains(workingCopy))
            {
                if (!workingCopy.Equals("not"))
                    return "";
            }

            // stem the word
            var stemmer = new IR_engine.stem.Stemmer();
            char[] temp = workingCopy.ToCharArray();
            stemmer.add(temp, temp.Length);
            if (toStem)
            {
                stemmer.stem();
            }
            else
            {
                stemmer.doNotStem();
            }
            return stemmer.ToString();
        }

        public static void Clear()
        {
            currentKeywords.Clear();
            parsed.Clear();
        }
        //private static bool IsStopword(String toCheck)
        //{
        //    foreach (var temp in _stopWords)
        //    {
        //        if (toCheck.Equals(temp))
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
    }
}
