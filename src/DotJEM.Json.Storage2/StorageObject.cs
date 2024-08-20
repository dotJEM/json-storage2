using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DotJEM.Json.Storage2;

//TODO: Can re decouple this from Newtonsoft.Json.Linq so users can select either that or System.Text.Json
/// <summary/>
public readonly record struct StorageObject(
    string ContentType, Guid Id, int Version,
    DateTime Created, DateTime Updated, string CreatedBy, string UpdatedBy,
    JObject Data) {

    /// <summary/>
    public override string ToString()
    {
        return $"{ContentType}/{Id} Version={Version}, Created={Created:O} By={CreatedBy}, Updated={Updated:O} By={UpdatedBy}" +
               $"\n{Data.ToString(Formatting.Indented)}";
    }

    /// <summary>
    /// Converts <see cref="StorageObject"/> into an <see cref="UpdateStorageObject"/>
    /// that can be used with <see cref="IStorageArea.UpdateAsync(UpdateStorageObject)"/>
    /// </summary>
    /// <remarks>
    /// When converting to an update object it only includes the <see cref="ContentType"/>, <see cref="Id"/> and <see cref="Data"/>
    /// properties, and leaves the rest for the <see cref="IStorageArea"/> to manage.
    /// </remarks>
    public static implicit operator UpdateStorageObject(StorageObject o)
        => new (o.ContentType, o.Id, o.Data);

    /// <summary>
    /// Converts <see cref="StorageObject"/> into an <see cref="InsertStorageObject"/>
    /// that can be used with <see cref="IStorageArea.InsertAsync(InsertStorageObject)"/>
    /// </summary>
    /// <remarks>
    /// When converting to an update object it only includes the <see cref="ContentType"/> and <see cref="Data"/>
    /// properties, and leaves the rest for the <see cref="IStorageArea"/> to manage.
    /// </remarks>
    public static implicit operator InsertStorageObject(StorageObject o)
        => new (o.ContentType, o.Data);
}

/// <summary>
/// 
/// </summary>
/// <param name="ContentType"></param>
/// <param name="Id"></param>
/// <param name="Data"></param>
/// <param name="Updated"></param>
/// <param name="UpdatedBy"></param>
public readonly record struct UpdateStorageObject(string ContentType, Guid Id, JObject Data, DateTime? Updated = null, string? UpdatedBy = null);

/// <summary>
/// 
/// </summary>
/// <param name="ContentType"></param>
/// <param name="Data"></param>
/// <param name="Created"></param>
/// <param name="CreatedBy"></param>
public readonly record struct InsertStorageObject(string ContentType, JObject Data, DateTime? Created = null, string? CreatedBy = null);

/// <summary>
/// 
/// </summary>
/// <param name="ContentType"></param>
/// <param name="Id"></param>
/// <param name="Data"></param>
/// <param name="Updated"></param>
/// <param name="UpdatedBy"></param>
public readonly record struct DeleteStorageObject(string ContentType, Guid Id, DateTime? Updated = null, string? UpdatedBy = null);
