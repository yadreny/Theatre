using MiniJSON;

namespace AlSo.Converter
{
    public class JsonMiniConverter : IJsonUtils
    {
        public static JsonMiniConverter Instance { get; private set; } = new JsonMiniConverter();
        private JsonMiniConverter() { }
        public string Serialize(object obj) => Json.Serialize(obj);
        public object Deserialize(string jsonString) => Json.Deserialize(jsonString);
    }
}