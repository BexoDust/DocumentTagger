using Newtonsoft.Json;
using System.IO;

namespace DocumentTaggerCore
{
    public class JsonIo
    {
        public static T? ReadObjectFromJsonFile<T>(string filename)
        {
            T? result = default;

            // deserialize JSON directly from a file

            using (var stream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using var reader = new StreamReader(stream);
                var serializer = new JsonSerializer();
                result = (T?)serializer.Deserialize(reader, typeof(T));
            }

            return result;
        }

        public static void SaveObjectToJson<T>(T objectToSave, string filename)
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };

            using var sw = new StreamWriter(filename);
            using var writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, objectToSave);
        }
    }
}
