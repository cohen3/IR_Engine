using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IR_engine
{
    static class RuleFactory
    {
        public static List<IRule> Factory()
        {
            List<IRule> rules = new List<IRule>();
            rules.Add(new TimeRule());
            rules.Add(new Distance());
            return rules;
        }
    }
}
