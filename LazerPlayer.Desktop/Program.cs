using System;
using System.Linq;
using osu.Framework;
using osu.Framework.Platform;

namespace LazerPlayer.Desktop
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            bool useOsuTK = args.Contains(@"--tk");

            // Hopefully a temporary thing until I find a better way to access osu!lazer's database.
            using (GameHost host = Host.GetSuitableHost(@"osu", useOsuTK: useOsuTK))
            using (Game game = new LazerPlayerDesktop())
                host.Run(game);
        }
    }
}
