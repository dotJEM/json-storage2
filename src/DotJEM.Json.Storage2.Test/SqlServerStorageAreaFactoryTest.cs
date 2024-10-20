﻿using DotJEM.Json.Storage2.SqlServer;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DotJEM.Json.Storage2.Test;

public class SqlServerStorageContextIntegrationTest
{
    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTable()
    {
        SqlServerStorageContext context = await SqlServerStorageContext.Create(TestSqlConnectionFactory.ConnectionString);
        IStorageArea area = await context.AreaAsync("test");
        await area.InsertAsync("na", new JObject());



    }

    [Test]
    public async Task EnsureLogTable_NoTableExists_ShouldCreateTables()
    {
        SqlServerStorageContext context = await SqlServerStorageContext
            .Create(TestSqlConnectionFactory.ConnectionString, "fox");
        IStorageArea area = await context.AreaAsync("test");
        StorageObject so = await area.InsertAsync("na",new JObject());

        Console.WriteLine(so);
        
        StorageObject? so2 = await area.GetAsync(so.Id);
        Console.WriteLine(so2);

        StorageObject so3 = await area.UpdateAsync(so.Id, JObject.FromObject(new { foo = "Fax" }));
        Console.WriteLine(so3);

        StorageObject so4 = await area.UpdateAsync(so.Id, JObject.FromObject(new { foo = "Foo" }));
        Console.WriteLine(so4);

        StorageObject? so5 = await area.GetAsync(so.Id);
        Console.WriteLine(so5);
    }

    [Test]
    public async Task GetAsync_NoTableExists_ShouldCreateTables()
    {
        SqlServerStorageContext context = await SqlServerStorageContext
            .Create(TestSqlConnectionFactory.ConnectionString, "fox");
        IStorageArea area = await context.AreaAsync("test");
        
        await area.InsertAsync("na", JObject.FromObject(new { track="T-01"}));
        await area.InsertAsync("na", JObject.FromObject(new { track= "T-02" }));
        await area.InsertAsync("na", JObject.FromObject(new { track= "T-03" }));
        await foreach (StorageObject obj in area.GetAsync())
        {
            Console.WriteLine(obj);
        }
    }

    [Test]
    public async Task InsertAsync_Record_ShouldAddToTable()
    {
        SqlServerStorageContext context = await SqlServerStorageContext.Create(TestSqlConnectionFactory.ConnectionString, "fox");
        IStorageArea area = await context.AreaAsync("test");

        StorageObject obj = await area.InsertAsync("na", JObject.FromObject(new { track = "T-01" }));
        StorageObject? obj2 = await area.GetAsync(obj.Id);

        Assert.That(obj2, Is.Not.Null & Has.Property(nameof(StorageObject.UpdatedBy)).EqualTo(obj.UpdatedBy));

    }

}