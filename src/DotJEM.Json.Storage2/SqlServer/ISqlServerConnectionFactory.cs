using System.Data.Common;
using System.Data.SqlClient;

namespace DotJEM.Json.Storage2.SqlServer;

public interface ISqlServerConnectionFactory
{
    SqlConnection Create();
}