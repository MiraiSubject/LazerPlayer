using LazerPlayer.Music;
using osu.Framework.Allocation;
using osu.Framework.Graphics;

namespace LazerPlayer.Tests.Visual.Screens
{
    public class TestSceneMusic : LazerPlayerTestScene
    {
        [Cached]
        private MusicController musicController = new MusicController();

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRange(new Drawable[]
            {
                musicController,
                new BottomMediaBar
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre
                }
            });
        }
    }
}
