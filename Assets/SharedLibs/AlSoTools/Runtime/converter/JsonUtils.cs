namespace AlSo.Converter
{
    public interface IJsonUtils
    {
        string Serialize(object obj);

        object Deserialize(string jsonString);
    }
}

