using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IR_engine
{
    public class Distance : IRule
    {
        public HashSet<string> distance = new HashSet<string>() {"meter","METER","Meter","CM","KM","cm","km","centimeter", "Centimeter", "CENTIMETER", "inch","Inch","INCH",
              "millimeter","Millimeter","mm","MM","MILLIMETER","Mile","mile","MILE","FEET","Feet","feet","yards","yard","Yard","YARD","Yards","YARDS","decimeter",
                "Decimeter","DECIMETER","meters","METERS","Meters","centimeters", "Centimeters", "CENTIMETERS", "inches","Inches","INCHES",
              "millimeters","Millimeters","mm","MM","MILLIMETERS","Miles","miles","MILES","FEETS","Feets","feets","decimeters",
                "Decimeters","DECIMETERS",};
        public bool CheckRule(bool isNum, string[] words, int idx)
        {
            if (!isNum) return false;
            if (idx + 1 < words.Length)
                if (distance.Contains(words[idx + 1]))
                    return true;
            return false;
        }

        public string CreatePhrase(string[] words, int idx, out int j, out term.Type type)
        {
            j = idx + 1;
            type = term.Type.distance;
            return words[idx] + " " + words[idx + 1];
        }
    }
}
