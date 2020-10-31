using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentTagger
{
    public class WorkerOptions
    {
        public List<string> WatchedLocations { get; set; }
        public string ConfigPath { get; set; }
        public string DefaultProcessedSucces { get; set; }
        public string DefaultProcessedFail { get; set; }

    }
}
