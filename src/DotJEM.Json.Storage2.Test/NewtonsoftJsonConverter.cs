using DotJEM.Json.Storage2.SqlServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2.Test;

public class NewtonsoftJsonConverter : IJsonConverter<JObject>
{
    public JObject Parse(string json) => JObject.Parse(json);

    public string ToString(JObject document, bool indent = false) => document.ToString(indent ? Formatting.Indented : Formatting.None);
}