using CommunityToolkit.Mvvm.ComponentModel;

namespace DocumentTaggerCore.Model
{
    public partial class KeyWord : ObservableObject
    {

        public KeyWord(string key, string mod)
        {
            Key = key;
            Modifier = mod;
        }

        [ObservableProperty]
        private string _key;

        [ObservableProperty]
        private string _modifier;

        public override string ToString() => $"{Key} - {Modifier}"; 
    }
}
