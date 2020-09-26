using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IR_engine
{
    class MaxList
    {
        List<KeyValuePair<string, int>> list = new List<KeyValuePair<string, int>>(5);
        KeyValuePair<string, int> min;
        public KeyValuePair<string, int> this[int key]
        {
            get
            {
                return list[key];
            }
            set
            {
                list[key] = value;
            }
        }
        public int this[string key]
        {
            get
            {
                foreach (KeyValuePair<string, int> p in list)
                    if (p.Key.Equals(key))
                        return p.Value;
                throw new ArgumentOutOfRangeException("no such string found");
            }
            set { }
        }

        public MaxList()
        {
            for (int i = 0; i < 5; i++)
            {
                list.Add(new KeyValuePair<string, int>("", 0 - i));
            }
            min = new KeyValuePair<string, int>("", -4);
        }

        public void add(KeyValuePair<string, int> value)
        {
            if (value.Value < min.Value) return;
            //KeyValuePair<string, int> minv = new KeyValuePair<string, int>("", int.MaxValue);
            for (int i = 0; i < 5; i++)
            {
                if (list[i].Value == value.Value) break;
                if (list[i].Value < value.Value)
                {
                    list[i] = value;
                    break;
                }
            }
        }

        public KeyValuePair<string, int> getMin()
        {
            KeyValuePair<string, int> minv = new KeyValuePair<string, int>("", int.MaxValue);
            for (int i = 0; i < 5; i++)
            {
                if (list[i].Value <= minv.Value)
                    minv = list[i];
            }
            return minv;
        }

        public List<string> getStrings()
        {
            List<string> l = new List<string>();
            l.Add(list[0].Key + "Rank: "+list[0].Value + "\t");
            l.Add(list[1].Key + "Rank: " + list[1].Value + "\t");
            l.Add(list[2].Key + "Rank: " + list[2].Value + "\t");
            l.Add(list[3].Key + "Rank: " + list[3].Value + "\t");
            l.Add(list[4].Key + "Rank: " + list[4].Value + "\t");
            return l;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append(list[0].Key);
            s.Append(" ");
            s.Append(list[1].Key);
            s.Append(" ");
            s.Append(list[2].Key);
            s.Append(" ");
            s.Append(list[3].Key);
            s.Append(" ");
            s.Append(list[4].Key);
            return s.ToString(); ;
        }
    }
}
