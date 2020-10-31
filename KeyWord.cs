namespace DocumentTagger
{
    public class KeyWord
    {

       public KeyWord(string key, string mod)
        {
            Key = key;
            Modifier = mod;
        }

        public string Key { get; set; }

        public string Modifier { get; set; }
    }
}
