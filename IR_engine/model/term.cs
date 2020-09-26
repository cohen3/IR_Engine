using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace IR_engine
{
     public class term
     {
        public enum Type { number, date, expression, distance, percentage, price, word, range, time };

        string phrase; // the phrase itself
        public int idf = 0; // the number of docs this term is in
        public int icf = 0;
        Type type;

        //used for global information, global occurances of the term in the curpus and if upper
        bool isUpperInCurpus;
        //end global variables

        public ConcurrentDictionary<int, short> posting; //string = doc index, int = occurances


        public term()
        {
            IsUpperInCurpus = true;
            phrase = "";
            posting = new ConcurrentDictionary<int, short>();
            idf = icf = 0;
        }

        public term(string phrase)
        {
            Phrase = phrase;
            IsUpperInCurpus = true;
            idf = icf = 0;
            posting = new ConcurrentDictionary<int, short>();
        }

        public string Phrase
        {
            set { phrase = value; }
            get { return phrase; }
        }
        public int Icf
        {
            set { Icf = value; }
            get { return Icf; }
        }

        public bool IsUpperInCurpus { get => isUpperInCurpus; set => isUpperInCurpus = value; }
        public Type Type1 { get => type; set => type = value; }

        /// <summary>
        /// this method check equality between this object and a given object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var term = obj as term;
            return term != null &&
                   phrase == term.Phrase;
        }
        /// <summary>
        /// updates the terms posting details
        /// </summary>
        /// <param name="doc">the doc where the term was seen</param>
        /// <param name="tf">the tf in the doc</param>
        public void AddToPosting(int doc, short tf)
        {
            this.icf += tf;
            posting.AddOrUpdate(doc, tf, (key, value) => {
                value += tf;
                return value;
            });
        }
        /// <summary>
        /// merges an dictionary to the terms addposting dictionary
        /// </summary>
        /// <param name="dictionary">the dictionary to merge</param>
        public void AddToPosting(ConcurrentDictionary<int, short> dictionary)
        {
            foreach(KeyValuePair<int, short> entry in dictionary)
            {
                AddToPosting(entry.Key, entry.Value);
            }
            dictionary.Clear();
        }
        /// <summary>
        /// toString for the posting dictionary of the term
        /// </summary>
        /// <returns>the string of the posting list</returns>
        public string printPosting()
        {
            StringBuilder res = new StringBuilder();
            foreach(KeyValuePair<int, short> entry in posting)
            {
                res.Append(entry.Key + "_" + entry.Value + ",");
            }
            return res.ToString();
        }
        public override string ToString()
        {
            char b = isUpperInCurpus ? 'T' : 'F';
            StringBuilder sb = new StringBuilder();
            sb.Append(Phrase);
            sb.Append("\t");
            sb.Append(b);
            sb.Append("\t");
            sb.Append((int)type);
            sb.Append("\t");
            sb.Append(printPosting());
            sb.Append("\t");
            sb.Append(icf);
            sb.Append("\t");
            sb.Append(posting.Count);
            return sb.ToString();
        }
        /// <summary>
        /// returns the term's tf value in a specifit doc
        /// </summary>
        /// <param name="docname">the doc</param>
        /// <returns>the tf number of the term in the dc</returns>
        public short getTFinDoc(int docname)
        {
            return posting[docname];
        }

        public override int GetHashCode()
        {
            var hashCode = -842790187;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(phrase);
            hashCode = hashCode * -1521134295 + type.GetHashCode();
            return hashCode;
        }
    }
}
