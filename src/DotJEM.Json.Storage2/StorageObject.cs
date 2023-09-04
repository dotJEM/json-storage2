using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.CompilerServices;

namespace DotJEM.Json.Storage2;

//TODO: Can re decouple this from Newtonsoft.Json.Linq so users can select either that or System.Text.Json
/// <summary>
/// 
/// </summary>
/// <param name="ContentType"></param>
/// <param name="Id"></param>
/// <param name="Version"></param>
/// <param name="Created"></param>
/// <param name="Updated"></param>
/// <param name="Data"></param>
public readonly record struct StorageObject(string ContentType, Guid Id, int Version, DateTime Created, DateTime Updated, JObject Data) {
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{ContentType}/{Id} Version={Version}, Created={Created:O}, Updated={Updated:O}\n{Data.ToString(Formatting.Indented)}";
    }
}
