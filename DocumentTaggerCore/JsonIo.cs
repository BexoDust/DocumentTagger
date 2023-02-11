using Newtonsoft.Json;
using System.IO;

namespace DocumentTaggerCore
{
    public class JsonIo
    {
        public static T ReadObjectFromJsonFile<T>(string filename)
        {
            T result = default;

            // deserialize JSON directly from a file

            using (FileStream stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    result = (T)serializer.Deserialize(reader, typeof(T));
                }
            }

            return result;
        }

        public static void SaveObjectToJson<T>(T objectToSave, string filename)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(filename))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, objectToSave);
                }
            }
        }
    }
}
