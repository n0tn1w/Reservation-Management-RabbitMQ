using System.Text.Json;
using System.Text;

namespace Utils;

public class JsonOperations
{
    public static T Deserialize<T>(byte[] byteArray)
    {
        string jsonString = Encoding.UTF8.GetString(byteArray);
        return JsonSerializer.Deserialize<T>(jsonString);
    }

    public static byte[] Serialize<T>(T request)
    {
        string jsonString = JsonSerializer.Serialize(request);
        return Encoding.UTF8.GetBytes(jsonString);
    }
}
