using System.Collections.Generic;

namespace DocumentTagger
{
    public class WorkerOptions
    {
        public List<string> WatchedLocations { get; set; }
        public string ConfigPath { get; set; }
        public string DefaultProcessedSuccess { get; set; }
    }
}
