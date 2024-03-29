using osu.Framework.Platform;

namespace LazerPlayer.Desktop
{
    internal class LazerPlayerDesktop : LazerPlayer
    {
        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            switch (host.Window)
            {
                case SDL2DesktopWindow desktopWindow:
                    desktopWindow.Title = Name;
                    break;
            }
        }
    }
}
