using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IR_engine
{
    public class TimeRule : IRule
    {
        public HashSet<string> times = new HashSet<string>() {"second", "Second", "SECOND","sec","Sec","SEC", "millisecond", "Millisecond", "MILLISECOND",
        "minute","minutes","Minute","Minutes","MINUTE","MINUTES","hour","hours","Hour","Hours","HOUR","HOURS","day","Day","days","Days","DAY","DAYS",
        "month","Month","MONTH","monthes","Months","MONTHS","year","Year","YEAR","years","Years","YEARS","semeter","SEMETSER","Semetser",
        "semeters","SEMETSERS","Semetsers","week","weeks","Week","Weeks","WEEK","WEEKS","Millennium","millennium","MILLENNIUM","Millenniums","millenniums","MILLENNIUMS"};
        static public HashSet<string> allAmounts = new HashSet<string>() { "M", "m", "Million", "MILLION", "million", "Thousand", "thousand", "THOUSAND", "trillion", "Trillion", "TRILLION", "BN", "bn", "Billion", "billion", "BILLION" };

        public bool CheckRule(bool isNum, string[] words, int idx)
        {
            if (!isNum) return false;
            if (idx + 1 < words.Length)
                if (times.Contains(words[idx + 1]))
                    return true;
            if (idx + 2 < words.Length)
                if (allAmounts.Contains(words[idx + 1]) && times.Contains(words[idx + 2])) return true;
            return false;
        }

        public string CreatePhrase(string[] words, int idx, out int j, out term.Type type)
        {
            type = term.Type.time;
            string output = null;
            if (idx + 1 < words.Length)
            {
                if (times.Contains(words[idx + 1]))
                {
                    output = words[idx] + " " + words[idx + 1];
                    j = idx + 1;
                    return output;
                }
            }
            j = idx + 2;
            output = words[idx] + " " + words[idx + 1] + " " + words[idx + 2];
            return output;
        }
    }
}
