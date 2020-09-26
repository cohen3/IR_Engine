using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace IR_engine
{/// <summary>
/// this class represents the Location object (city and relevent data)
/// </summary>
    public class Location
    {
        private string country;
        private ConcurrentDictionary<int, List<int>> locationsInDocs;
        private string population;
        private string currency;
        private string capital;
        private string city;

        public string Country { get => country; set => country = value; }
        public ConcurrentDictionary<int, List<int>> LocationsInDocs { get => locationsInDocs; set => locationsInDocs = value; }
        public string Population { get => population; set => population = value; }
        public string Currency { get => currency; set => currency = value; }
        public string Capital { get => capital; set => capital = value; }
        public string City { get => city; set => city = value; }

        public Location()
        {
            Country = "";
            population = "";
            currency = "";
            Capital = "";
            city = "";
            locationsInDocs = new ConcurrentDictionary<int, List<int>>();
        }
        /// <summary>
        /// this function update the location and adds a dictionary of doc names and location in docs
        /// </summary>
        /// <param name="n">the dictionary of the locationsInDocs to add</param>
        public void addOccurs(ConcurrentDictionary<int, List<int>> n)
        {
            foreach(KeyValuePair<int, List<int>> entry in n)
            {
                locationsInDocs.TryAdd(entry.Key,entry.Value);
            }
        }
        public Location(string city, string Country, string populationTemp,string currency,string Capital)
        {
            this.city = city;
            string popstr = populationTemp;
            double pop = double.Parse(populationTemp);
            if(pop>=1000 && pop< 1000000) { pop = pop / 1000; popstr = pop + "K"; }
            else if (pop >= 1000000 && pop< 1000000000){pop = pop / 1000000; popstr=pop+"M";}
            else if (pop>= 1000000000) { pop = pop / 1000000000; popstr = pop + "B"; }
            else { popstr = pop + ""; }
            this.Country = Country;
            this.population = popstr;
            this.currency = currency;
            this.Capital = Capital;
            locationsInDocs = new ConcurrentDictionary<int, List<int>>();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(city + "\t");
            foreach (KeyValuePair<int, List<int>> entry in locationsInDocs)
            {
                sb.Append(entry.Key+" ");
                for (int i = 0; i < entry.Value.Count; i++)
                {
                    if (i > 0)
                    {
                        if (entry.Value[i] <= entry.Value[i - 1])
                        {
                            continue; }
                    }
                    sb.Append(entry.Value[i] +"|");
                }
                sb.Append(',');
            }
            return sb.ToString();
        }
        public override bool Equals(object obj)
        {
            var term = obj as Location;
            return term != null &&
                   city == term.city;
        }
    }
}
