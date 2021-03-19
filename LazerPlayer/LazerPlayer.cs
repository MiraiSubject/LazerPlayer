using LazerPlayer.Music;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Overlays.Volume;

namespace LazerPlayer
{
    public class LazerPlayer : LazerPlayerBase
    {
        [Resolved]
        private FrameworkConfigManager configManager { get; set; }
        private FrameSync originalFps;
        private Bindable<FrameSync> fps;

        private WindowMode originalWindowMode;
        private Bindable<WindowMode> windowMode;

        private VolumeOverlay volume;

        [BackgroundDependencyLoader]
        private void load()
        {
            base.Content.AddRange(new Drawable[]
            {

                new VolumeControlReceptor
                {
                    RelativeSizeAxes = Axes.Both,
                    ActionRequested = action => volume.Adjust(action),
                    ScrollActionRequested = (action, amount, isPrecise) => volume.Adjust(action, amount, isPrecise),
                },
                new BottomMediaBar
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                },
                volume = new VolumeOverlay(),
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Force windowed mode and vsync and store original values
            // We're pretending to be osu!lazer after all.
            fps = configManager.GetBindable<FrameSync>(FrameworkSetting.FrameSync);
            windowMode = configManager.GetBindable<WindowMode>(FrameworkSetting.WindowMode);
            originalWindowMode = windowMode.Value;
            originalFps = fps.Value;
            windowMode.Value = WindowMode.Windowed;
            fps.Value = FrameSync.VSync;
        }

        protected override bool OnScroll(ScrollEvent e)
        {
            // forward any unhandled mouse scroll events to the volume control.
            volume.Adjust(GlobalAction.IncreaseVolume, e.ScrollDelta.Y, e.IsPrecise);
            return true;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // Restore original frame rate value that the user had set for osu!lazer.
            fps.Value = originalFps;
            windowMode.Value = originalWindowMode;
        }
    }
}
