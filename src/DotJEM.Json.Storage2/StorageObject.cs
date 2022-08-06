using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

public readonly record struct StorageObject(string ConcentType, Guid Id, int Version, DateTime Created, DateTime Updated, JObject Data);