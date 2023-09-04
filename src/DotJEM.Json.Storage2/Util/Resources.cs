using System.IO;
using System.Reflection;

namespace DotJEM.Json.Storage2.Util;

public interface IAssemblyResourceLoader
{
    string LoadString(string resourceName);
}

public class AssemblyResourceLoader : IAssemblyResourceLoader
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

    public string LoadString(string resourceName)
    {
        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
            throw new FileNotFoundException();

        using StreamReader reader = new(resourceStream);
        return reader.ReadToEnd();
    }
}