using System.Collections.Generic;
using System.Diagnostics;

namespace DocumentTagger
{
    [DebuggerDisplay("Name = {AddedFileWord}")]
    public class Tag
    {
        public List<KeyWord> Keywords { get; set; }

        public List<string> MoveLocations { get; set; }

        public string AddedFileWord { get; set; }

        public Tag()
        {
            Keywords = new List<KeyWord>();
            MoveLocations = new List<string>();
        }
         
    }
}
