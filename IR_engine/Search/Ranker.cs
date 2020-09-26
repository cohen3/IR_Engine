using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace IR_engine
{
    class Ranker
    {
        public double k1 = 1.2;
        public double k3 = 8;
        public double b = 0.75;
        public double delta = 2.0;   //TODO: change and check
        public string dataPath = null;
        public string postPath = null;
        public List<string> qry = null;
        public List<string> docs = null;
        Dictionary<string, int> docSize = new Dictionary<string, int>();


        public Ranker(string path, bool isStem)
        {
            dataPath = path;
            if (!isStem) { postPath = @"\DisableStem"; } else { postPath = @"EnableStem"; }
            using (StreamReader sr = new StreamReader(dataPath + "\\documents.txt"))
            {
                string line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    string[] splitted = line.Split('\t');
                    docSize.Add(splitted[0], int.Parse(splitted[6].Trim()));
                }
            }
        }

        // changed: Elad
        //
        // Dictionary<string, KeyValuePair<int, term.Type>>> = <the term from the query, <occurances in the query, term type>>
        // the dictionary contains all terms from the query
        //
        // List<string> = a list of all retrieved documents you'll need to rank
        //
        public List<KeyValuePair<string, double>> rank(
            Dictionary<string, KeyValuePair<int, term.Type>> qries,
            HashSet<string> docs,
            Dictionary<string, double> weights,
            bool isfiltered
            )
        {
            Dictionary<string, double> scoresBMOrigin = new Dictionary<string, double>();// ket is Document, value is score
            Dictionary<string, double> CosSim = new Dictionary<string, double>();// ket is Document, value is score
            double numOfDocs = 0;
            double avgDocLength = 0;
            string line;
            Dictionary<string, Dictionary<string, int>> terms = new Dictionary<string, Dictionary<string, int>>();//key=term, value=doc,tf
            Dictionary<string, List<string>> fin = new Dictionary<string, List<string>>(); // key = type, value=list of terms
            //Dictionary<string, int> docSize = new Dictionary<string, int>(); //key= docName value = doc size
            HashSet<string> relevent_cts = new HashSet<string>();

            /*
             * this part gets the size of each document and the avarege doc size
             */
            foreach(string docIndex in docs)
            {
                avgDocLength += docSize[docIndex];
            }
            int docamount = docs.Count;
            avgDocLength = avgDocLength / docamount;
            /*
             *  this part gets all the terms by type
             */
            foreach (string q in qries.Keys)
            {
                if (qries[q].Value == term.Type.word)
                {
                    char c = char.ToUpper(q[0]);
                    string file = c <= 'Z' && c >= 'A' ? c + "" : "other";
                    if (!fin.ContainsKey(file))
                    {
                        List<string> x = new List<string>();
                        x.Add(q);
                        fin.Add(file, x);
                    }
                    else { fin[file].Add(q); }
                }
                else
                {
                    if (!fin.ContainsKey(qries[q].Value + ""))
                    {
                        List<string> x = new List<string>();
                        x.Add(q);
                        fin.Add(qries[q].Value + "", x);
                    }
                    else { fin[qries[q].Value + ""].Add(q); }
                }
            }

            /*
             * this part fills the terms Dictionary foreach term with the docs it shows up in acoording to the docs list
             */
            foreach (string str in fin.Keys)
            {
                using (StreamReader st = new StreamReader(File.Open(dataPath + "\\" + str + "" + ".txt", FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    while ((line = st.ReadLine()) != null)
                    {
                        string term = line.Split('\t')[0];
                        List<string> docsforTerms = line.Split('\t')[1].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        int len = docsforTerms.Count;
                        if (qries.ContainsKey(term) || qries.ContainsKey(term.ToLower()))
                        {
                            term = term.ToLower();
                            Dictionary<string, int> tmp = new Dictionary<string, int>();
                            string doc = "";
                            for(int i = 0; i < len; i++)
                            {
                                doc = docsforTerms[i];
                                if (!relevent_cts.Contains(doc.Substring(0, doc.IndexOf('_')).Trim(' ')))
                                relevent_cts.Add(doc.Substring(0, doc.IndexOf('_')).Trim(' '));
                                if (terms.ContainsKey(term))
                                {
                                    terms[term].Add(doc.Substring(0, doc.IndexOf('_')).Trim(' '), int.Parse(doc.Substring(doc.IndexOf('_') + 1, doc.Length - 1 - doc.IndexOf('_'))));
                                }
                                else { terms.Add(term, new Dictionary<string, int>()); terms[term].Add(doc.Substring(0, doc.IndexOf('_')).Trim(' '), int.Parse(doc.Substring(doc.IndexOf('_') + 1, doc.Length - 1 - doc.IndexOf('_')))); }
                            }
                        }
                    }
                }
            }
            /*
             * this part calculates the different variables for the equation
             */

            foreach (string docu in docs)
            {
                if (!docs.Contains(docu)) continue;
                double docL = docSize[docu];
                double scoreTmp = 0;
                //double score22 = 0;
                //double score23 = 0;
                //double w1 = 0; //root of qf^2*tf^2
                //double w2 = 0; //qf*tf
                if (!relevent_cts.Contains(docu)) continue;
                foreach (string term in terms.Keys)
                {
                    double qf = qries[term].Key;
                    double nqi = terms[term].Count;
                    double N = docs.Count;
                    double IDF = Math.Log((N - nqi + 0.5) / (nqi) + 0.5);
                    Dictionary<string, int> x = terms[term];
                    double tf = 0;
                    if (!x.ContainsKey(docu)) { tf = 0; }
                    else { tf = x[docu]; }
                    double tf2 = tf / docL;
                    // double idf2 = Math.Log(docs.Count / nqi); NO USE!! bad bad stuff
                    //  w1 += Math.Sqrt(Math.Pow(qf, 2) * Math.Pow(tf2, 2));
                    // w2 += qf * tf2;
                    //score22 += qf * ((k1 + 1) * tf / (tf + k1 * (1 - b + b * docL / avgDocLength))) * IDF * weights[term];
                    scoreTmp += BM25rank(qf, nqi, N, IDF, tf, docL, avgDocLength, weights[term]);
                }
                if (scoreTmp <= 0) continue;
                scoresBMOrigin.Add(docu, scoreTmp);
               // CosSim.Add(docu, w2 / w1);
            }
            //double minV = scoresBMOrigin.Aggregate((l, r) => l.Value < r.Value ? l : r).Value;
            if (scoresBMOrigin.Count == 0)
                return null;
            double avgScore = scoresBMOrigin.Values.Average();
            var items = from pair in scoresBMOrigin
                        orderby pair.Value descending
                        select pair;
            List<KeyValuePair<string, double>> ans = new List<KeyValuePair<string, double>>();
            foreach (KeyValuePair<string, double> x in items)
            {
                if (avgScore <= x.Value)
                {
                    ans.Add(x);
                }
            }
            //var items2 = from pair in CosSim
            //             orderby pair.Value descending
            //             select pair;
            //List<KeyValuePair<string, double>> ans2 = new List<KeyValuePair<string, double>>();
            //foreach (KeyValuePair<string, double> x in items2)
            //{
            //    ans2.Add(x);
            //}
            return ans;
        }
        public double BM25rank(double qf, double nqi, double N, double IDF, double tf, double docL, double avgDocLength,double weight )
        {
            return (IDF * (tf * (k1 + 1.0) / (tf + k1 * (1.0 - b + b * docL / avgDocLength)))) * weight;
        }
    }
}


