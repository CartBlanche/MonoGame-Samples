using System.Runtime.Versioning;
using System.Threading.Tasks;
using CatapultGame;
using MonoGame.Runtime.Browser.WebGL2;

namespace CatapultGame.WebGL;

[SupportedOSPlatform("browser")]
internal static class Program
{
    private static async Task Main()
    {
        await BrowserGame.RunAsync(new CatapultGame());
    }
}
