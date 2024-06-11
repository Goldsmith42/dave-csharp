using DaveCsharp.Common.Configuration;
using Microsoft.Extensions.Configuration;

namespace DaveCsharp;

class Program
{
    static void Main(string[] args)
    {
        var originalExeLocation = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json", optional: false)
            .Build()
            .GetSection("OriginalExeLocation")
            .Get<OriginalExeLocation>();
        Game.Main.Start(originalExeLocation);
    }
}
