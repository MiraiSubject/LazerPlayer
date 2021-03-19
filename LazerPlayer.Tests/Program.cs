using osu.Framework;
using osu.Framework.Platform;

namespace LazerPlayer.Tests
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableHost("osu"))
            using (var game = new LazerPlayerTestBrowser())
                host.Run(game);
        }
    }
}
