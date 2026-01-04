using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DocumentTaggerCore.Model
{
    public partial class Rule : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<KeyWord> _keywords;

        [ObservableProperty]
        private ObservableCollection<string> _results;

        public Rule()
        {
            Keywords = new ObservableCollection<KeyWord>();
            Results = new ObservableCollection<string>();
        }

        public override string ToString() => Keywords?.FirstOrDefault()?.Key ?? "<null>";

    }
}
