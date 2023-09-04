using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotJEM.Json.Storage2.Cache;
using DotJEM.Json.Storage2.SqlServer.Initialization;

namespace DotJEM.Json.Storage2.SqlServer;

public class SqlServerStorageContext : IStorageContext
{
    public static async Task<SqlServerStorageContext> Create(string connectionString, string schema = "dbo")
    {
        SqlServerConnectionFactory connectionFactory = new SqlServerConnectionFactory(connectionString);
        SqlServerStorageAreaFactory areaFactory = await Temp.Create(schema, connectionFactory);
        return new SqlServerStorageContext(connectionFactory, areaFactory);
    } 

    private readonly AsyncCache<SqlServerStorageArea> areas = new();
    private readonly SqlServerStorageAreaFactory areaFactory;

    public ISqlServerConnectionFactory ConnectionFactory { get; }
    public ISqlServerCommandBuilderFactory CommandBuilder { get; }

    private SqlServerStorageContext(SqlServerConnectionFactory connectionFactory, SqlServerStorageAreaFactory areaFactory)
    {
        this.areaFactory = areaFactory;
        ConnectionFactory = connectionFactory;
        CommandBuilder = new SqlServerCommandBuilderFactory(connectionFactory);
    }

    public async Task<IStorageArea> AreaAsync(string name)
    {
        return await areas.GetOrAdd(name, key => areaFactory.Create(key, this));
    }

    public bool Release(string name)
    {
        return areas.Release(name);
    }

    public SqlConnection CreateConnection() => ConnectionFactory.Create();
}

public class SqlServerConnectionFactory : ISqlServerConnectionFactory
{
    private readonly string connectionString;

    public SqlServerConnectionFactory(string connectionString)
    {
        this.connectionString = connectionString;
    }

    public SqlConnection Create()
    {
        return new SqlConnection(connectionString);
    }
}

public interface ISqlServerCommandBuilderFactory
{
    ISqlServerCommandBuilder From(string resource, string section = "default");

}

public class SqlServerCommandBuilderFactory : ISqlServerCommandBuilderFactory
{
    private readonly ISqlServerConnectionFactory connectionFactory;

    public SqlServerCommandBuilderFactory(ISqlServerConnectionFactory connectionFactory)
    {
        this.connectionFactory = connectionFactory;
    }

    public ISqlServerCommandBuilder From(string resource, string section =  "default")
    {
        return new SqlServerCommandBuilder(connectionFactory, resource, section);
    }
}

public interface ISqlServerCommandBuilder
{
    ISqlServerCommandBuilder Replace(params (string key, string value)[] replace);
    ISqlServerCommandBuilder Parameters(params Parameter[] parameters);
    ISqlServerCommand Build();
}

public class SqlServerCommandBuilder : ISqlServerCommandBuilder
{
    private readonly ISqlServerConnectionFactory connectionFactory;
    private readonly string resource;
    private readonly string section;

    private readonly Dictionary<string, string> replacements = new Dictionary<string, string>();
    private readonly List<Parameter> parameters = new List<Parameter>();

    public SqlServerCommandBuilder(ISqlServerConnectionFactory connectionFactory, string resource, string section)
    {
        this.connectionFactory = connectionFactory;
        this.resource = resource;
        this.section = section;
    }


    public ISqlServerCommandBuilder Replace(params (string key, string value)[] replace)
    {
        foreach ((string key, string value) in replace)
            replacements[key] = value;
        return this;
    }

    public ISqlServerCommandBuilder Parameters(params Parameter[] parameters)
    {
        this.parameters.AddRange(parameters);
        return this;
    }

    public ISqlServerCommand Build()
    {
        string commandText = SqlServerStatements.Load(resource, section, replacements);

        SqlConnection connection = connectionFactory.Create();
        SqlCommand command = new SqlCommand(commandText, connection);
        foreach ((string name, SqlDbType sqlDbType, object value) in parameters)
            command.Parameters.Add(name, sqlDbType).Value = value;

        return new SqlServerCommand(connection, command);
    }
}

public interface ISqlServerCommand : IDisposable
{
    Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken);
    Task<ISqlServerDataReader<T>> ExecuteReaderAsync<T>(string[] columns, Func<object[], T> factory, CancellationToken cancellationToken);
}

public class SqlServerCommand : ISqlServerCommand
{
    // ReSharper disable once NotAccessedField.Local
    private readonly SqlConnection connection;
    private readonly SqlCommand command;

    public SqlServerCommand(SqlConnection connection, SqlCommand command)
    {
        this.connection = connection;
        this.command = command;
    }

    public async Task<T> ExecuteScalarAsync<T>(CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

#if NETSTANDARD2_0
        using SqlTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        T value =  (T)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException());
        transaction.Commit();
#else
        await using SqlTransaction transaction = connection.BeginTransaction();
        command.Transaction = transaction;
        T value = (T)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException());
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
#endif
        return value;
    }

    public async Task<ISqlServerDataReader<T>> ExecuteReaderAsync<T>(string[] columns, Func<object[], T> factory, CancellationToken cancellationToken)
    {
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        SqlDataReader? reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        return new SqlServerDataReader<T>(columns, factory, reader);
    }

    public void Dispose()
    {
        connection.Dispose();
        command.Dispose();
    }
}


public readonly record struct Parameter(string Name, SqlDbType Type, object value)
{
    public static implicit operator Parameter((string name, int value) tuple)
        => new(tuple.name, SqlDbType.Int, tuple.value);

    public static implicit operator Parameter((string name, long value) tuple)
        => new(tuple.name, SqlDbType.BigInt, tuple.value);

    public static implicit operator Parameter((string name, string value) tuple)
        => new(tuple.name, SqlDbType.NVarChar, tuple.value);

    public static implicit operator Parameter((string name, DateTime value) tuple)
        => new(tuple.name, SqlDbType.DateTime, tuple.value);

    public static implicit operator Parameter((string name, Guid value) tuple)
        => new(tuple.name, SqlDbType.UniqueIdentifier, tuple.value);

    public static implicit operator Parameter((string name, byte[] value) tuple)
        => new(tuple.name, SqlDbType.VarBinary, tuple.value);

}


public interface ISqlServerDataReader<out T> : IDisposable, IEnumerable<T>
{
}

public class SqlServerDataReader<T> : ISqlServerDataReader<T>
{
    private readonly string[] columns;
    private readonly Func<object[], T> factory;
    private readonly SqlDataReader reader;

    public SqlServerDataReader(string[] columns, Func<object[], T> factory, SqlDataReader reader)
    {
        this.columns = columns;
        this.factory = factory;
        this.reader = reader;
    }

    public IEnumerator<T> GetEnumerator()
    {
        int[] c = columns.Select(name => reader.GetOrdinal(name)).ToArray();
        while (reader.Read())
        {
            //TODO: Better way of handling column specs as this is surely horrible performance, but function first!.
            object[] values = new object[c.Length];
            for (int i = 0; i < c.Length; i++)
                values[i] = reader.GetValue(c[i]);
            yield return factory(values);
        }
    }


    /// <inheritdoc />
    public void Dispose() => reader.Dispose();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}