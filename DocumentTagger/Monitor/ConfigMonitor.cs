using System;
using System.Collections.Generic;
using System.IO;

namespace DocumentTagger
{
    public class ConfigMonitor
    {
        private readonly FileSystemWatcher _watcher;

        public event FileSystemEventHandler FileChanged
        {
            add { _watcher.Changed += value; }
            remove { _watcher.Changed -= value; }
        }

        public ConfigMonitor(string watchedConfig)
        {
            _watcher = new FileSystemWatcher();
            _watcher.Path = Path.GetDirectoryName(watchedConfig);
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Filter = Path.GetFileName(watchedConfig);
            //_watcher.Changed += FileChangedCallback;
            _watcher.IncludeSubdirectories = false;
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            //_watcher.Changed -= FileChangedCallback;
            _watcher.EnableRaisingEvents = false;
        }
    }
}
