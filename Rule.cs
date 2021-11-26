using System.Collections.Generic;
using System.Diagnostics;

namespace DocumentTagger
{
    public class Rule
    {
        public List<KeyWord> Keywords { get; set; }

        public List<string> Results { get; set; }

        public Rule()
        {
            Keywords = new List<KeyWord>();
            Results = new List<string>();
        }
         
    }
}
