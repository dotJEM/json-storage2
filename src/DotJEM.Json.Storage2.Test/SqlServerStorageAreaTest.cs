using DotJEM.Json.Storage2.SqlServer;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Storage2.Test;

public class SqlServerStorageAreaTest
{
    [Test]
    public async Task GetAndDeleteAsync_AreaDoesNotExist_ShouldReturnNoObjects()
    {
        IStorageContext<JObject> context = await new SqlServerStorageContextBuilder<JObject>(
                TestSqlConnectionFactory.ConnectionString, new NewtonsoftJsonConverter())
            .Build();
        IStorageArea<JObject> area = await context.AreaAsync($"area_{Guid.NewGuid():N}");

        List<StorageObject<JObject>> objects = await CollectAsync(area.GetAsync());

        Assert.That(objects, Is.Empty);
        Assert.That(await area.GetAsync(Guid.NewGuid()), Is.Null);
        Assert.That(await area.DeleteAsync(Guid.NewGuid()), Is.Null);
        Assert.That(await area.DeleteAsync(Guid.NewGuid(), CancellationToken.None), Is.Null);
    }

    [Test]
    public async Task InsertAsync_DataAndMetadataOverloads_ShouldPersistObjects()
    {
        IStorageContext<JObject> context = await new SqlServerStorageContextBuilder<JObject>(
                TestSqlConnectionFactory.ConnectionString, new NewtonsoftJsonConverter())
            .Build();
        IStorageArea<JObject> area = await context.AreaAsync($"area_{Guid.NewGuid():N}");
        JObject firstData = JObject.FromObject(new { value = "data" });
        JObject firstMeta = JObject.FromObject(new { source = "first" });
        JObject secondData = JObject.FromObject(new { value = "data-with-meta" });
        JObject secondMeta = JObject.FromObject(new { source = "second" });

        StorageObject<JObject> first = await area.InsertAsync("application/json", firstData);
        StorageObject<JObject> second = await area.InsertAsync("application/json", secondData, secondMeta);
        StorageObject<JObject> third = await area.InsertAsync(
            "application/json", JObject.FromObject(new { value = "data-with-token" }), CancellationToken.None);
        StorageObject<JObject> fourth = await area.InsertAsync(
            "application/json", JObject.FromObject(new { value = "data-meta-token" }), firstMeta, CancellationToken.None);

        foreach (StorageObject<JObject> inserted in new[] { first, second, third, fourth })
        {
            StorageObject<JObject>? stored = await area.GetAsync(inserted.Id, CancellationToken.None);

            Assert.That(stored, Is.Not.Null);
            Assert.That(stored!.ContentType, Is.EqualTo("application/json"));
            Assert.That(JToken.DeepEquals(stored.Data, inserted.Data), Is.True);
            Assert.That(JToken.DeepEquals(stored.Meta, inserted.Meta ?? new JObject()), Is.True);
            Assert.That(stored.Version, Is.Zero);
        }

        Assert.That(JToken.DeepEquals((await area.GetAsync(first.Id))!.Meta, new JObject()), Is.True);
        Assert.That(JToken.DeepEquals((await area.GetAsync(second.Id))!.Meta, secondMeta), Is.True);
    }

    [Test]
    public async Task InsertAsync_InsertObjectOverloads_ShouldHonorSuppliedAuditValues()
    {
        IStorageContext<JObject> context = await new SqlServerStorageContextBuilder<JObject>(
                TestSqlConnectionFactory.ConnectionString, new NewtonsoftJsonConverter())
            .Build();
        IStorageArea<JObject> area = await context.AreaAsync($"area_{Guid.NewGuid():N}");
        DateTime created = new(2024, 3, 5, 6, 7, 8, DateTimeKind.Utc);
        JObject data = JObject.FromObject(new { value = "record" });
        JObject meta = JObject.FromObject(new { source = "record" });
        InsertStorageObject<JObject> insert = new("application/json", data, meta, created, "record-author");

        StorageObject<JObject> inserted = await area.InsertAsync(insert);
        StorageObject<JObject> insertedWithToken = await area.InsertAsync(
            new InsertStorageObject<JObject>("application/json", data, meta, created.AddSeconds(1), "token-author"),
            CancellationToken.None);
        StorageObject<JObject>? stored = await area.GetAsync(inserted.Id);
        StorageObject<JObject>? storedWithToken = await area.GetAsync(insertedWithToken.Id);

        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Created, Is.EqualTo(created));
        Assert.That(stored.Updated, Is.EqualTo(created));
        Assert.That(stored.CreatedBy, Is.EqualTo("record-author"));
        Assert.That(stored.UpdatedBy, Is.EqualTo("record-author"));
        Assert.That(JToken.DeepEquals(stored.Data, data), Is.True);
        Assert.That(JToken.DeepEquals(stored.Meta, meta), Is.True);
        Assert.That(storedWithToken, Is.Not.Null);
        Assert.That(storedWithToken!.Created, Is.EqualTo(created.AddSeconds(1)));
        Assert.That(storedWithToken.CreatedBy, Is.EqualTo("token-author"));
        Assert.That(JToken.DeepEquals(storedWithToken.Data, data), Is.True);
        Assert.That(JToken.DeepEquals(storedWithToken.Meta, meta), Is.True);
        Assert.That(insertedWithToken.CreatedBy, Is.EqualTo("token-author"));
        Assert.That(insertedWithToken.Created, Is.EqualTo(created.AddSeconds(1)));
    }

    [Test]
    public async Task GetAsync_PagingOverloads_ShouldReturnExpectedPages()
    {
        IStorageContext<JObject> context = await new SqlServerStorageContextBuilder<JObject>(
                TestSqlConnectionFactory.ConnectionString, new NewtonsoftJsonConverter())
            .Build();
        IStorageArea<JObject> area = await context.AreaAsync($"area_{Guid.NewGuid():N}");
        DateTime created = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        StorageObject<JObject>[] inserted = new StorageObject<JObject>[3];

        for (int i = 0; i < inserted.Length; i++)
        {
            inserted[i] = await area.InsertAsync(new InsertStorageObject<JObject>(
                "application/json", JObject.FromObject(new { index = i }), Created: created.AddDays(i)));
        }

        Assert.That((await CollectAsync(area.GetAsync())).Select(obj => obj.Id), Is.EqualTo(inserted.Select(obj => obj.Id)));
        Assert.That((await CollectAsync(area.GetAsync(CancellationToken.None))).Select(obj => obj.Id), Is.EqualTo(inserted.Select(obj => obj.Id)));
        Assert.That((await CollectAsync(area.GetAsync(1))).Select(obj => obj.Id), Is.EqualTo(inserted.Skip(1).Select(obj => obj.Id)));
        Assert.That((await CollectAsync(area.GetAsync(1, CancellationToken.None))).Select(obj => obj.Id), Is.EqualTo(inserted.Skip(1).Select(obj => obj.Id)));
        Assert.That((await CollectAsync(area.GetAsync(1, 1, CancellationToken.None))).Select(obj => obj.Id), Is.EqualTo(new[] { inserted[1].Id }));
    }

    [Test]
    public async Task UpdateAsync_Overloads_ShouldPersistDataMetadataAndVersions()
    {
        IStorageContext<JObject> context = await new SqlServerStorageContextBuilder<JObject>(
                TestSqlConnectionFactory.ConnectionString, new NewtonsoftJsonConverter())
            .Build();
        IStorageArea<JObject> area = await context.AreaAsync($"area_{Guid.NewGuid():N}");
        StorageObject<JObject> inserted = await area.InsertAsync(
            "application/json", JObject.FromObject(new { version = 0 }), JObject.FromObject(new { retained = true }));

        StorageObject<JObject> first = await area.UpdateAsync(inserted.Id, JObject.FromObject(new { version = 1 }));
        StorageObject<JObject> second = await area.UpdateAsync(
            inserted.Id, JObject.FromObject(new { version = 2 }), JObject.FromObject(new { changed = 2 }));
        StorageObject<JObject> third = await area.UpdateAsync(
            inserted.Id, JObject.FromObject(new { version = 3 }), CancellationToken.None);
        StorageObject<JObject> fourth = await area.UpdateAsync(
            inserted.Id, JObject.FromObject(new { version = 4 }), JObject.FromObject(new { changed = 4 }), CancellationToken.None);
        StorageObject<JObject> fifth = await area.UpdateAsync(
            new UpdateStorageObject<JObject>("application/json", inserted.Id, JObject.FromObject(new { version = 5 }), JObject.FromObject(new { changed = 5 })));
        DateTime updated = new(2024, 4, 5, 6, 7, 8, DateTimeKind.Utc);
        StorageObject<JObject> sixth = await area.UpdateAsync(
            new UpdateStorageObject<JObject>(
                "application/json", inserted.Id, JObject.FromObject(new { version = 6 }),
                JObject.FromObject(new { changed = 6 }), updated, "record-editor"),
            CancellationToken.None);
        StorageObject<JObject>? stored = await area.GetAsync(inserted.Id);

        Assert.That(new[] { first, second, third, fourth, fifth, sixth }.Select(obj => obj.Version), Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6 }));
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.Version, Is.EqualTo(6));
        Assert.That(stored.ContentType, Is.EqualTo("application/json"));
        Assert.That(stored.Updated, Is.EqualTo(updated));
        Assert.That(stored.UpdatedBy, Is.EqualTo("record-editor"));
        Assert.That(JToken.DeepEquals(stored.Data, JObject.FromObject(new { version = 6 })), Is.True);
        Assert.That(JToken.DeepEquals(stored.Meta, JObject.FromObject(new { changed = 6 })), Is.True);
    }

    [Test]
    public async Task DeleteAsync_ExistingObjects_ShouldReturnDeletedObjectAndRemoveIt()
    {
        IStorageContext<JObject> context = await new SqlServerStorageContextBuilder<JObject>(
                TestSqlConnectionFactory.ConnectionString, new NewtonsoftJsonConverter())
            .Build();
        IStorageArea<JObject> area = await context.AreaAsync($"area_{Guid.NewGuid():N}");
        StorageObject<JObject> first = await area.InsertAsync("application/json", JObject.FromObject(new { id = 1 }));
        StorageObject<JObject> second = await area.InsertAsync("application/json", JObject.FromObject(new { id = 2 }));

        StorageObject<JObject>? deleted = await area.DeleteAsync(first.Id);
        StorageObject<JObject>? deletedWithToken = await area.DeleteAsync(second.Id, CancellationToken.None);

        Assert.That(deleted, Is.Not.Null);
        Assert.That(deleted!.Id, Is.EqualTo(first.Id));
        Assert.That(JToken.DeepEquals(deleted.Data, first.Data), Is.True);
        Assert.That(deletedWithToken, Is.Not.Null);
        Assert.That(deletedWithToken!.Id, Is.EqualTo(second.Id));
        Assert.That(await area.GetAsync(first.Id), Is.Null);
        Assert.That(await area.GetAsync(second.Id), Is.Null);
    }

    private static async Task<List<T>> CollectAsync<T>(IAsyncEnumerable<T> items)
    {
        List<T> result = [];
        await foreach (T item in items)
            result.Add(item);
        return result;
    }
}
