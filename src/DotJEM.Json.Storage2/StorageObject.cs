using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

//TODO: Can re decouple this from Newtonsoft.Json.Linq so users can select either that or System.Text.Json
public readonly record struct StorageObject(string ContentType, Guid Id, int Version, DateTime Created, DateTime Updated, JObject Data) {
    public override string ToString()
    {
        return $"{ContentType}/{Id} Version={Version}, Created={Created:O}, Updated={Updated:O}\n{Data.ToString(Formatting.Indented)}";
    }
}