using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Overlays.Music;

namespace LazerPlayer.Music
{
    public class PlaylistSection : Container
    {
        public IBindableList<BeatmapSetInfo> BeatmapSets => beatmapSets;
        private readonly BindableList<BeatmapSetInfo> beatmapSets = new BindableList<BeatmapSetInfo>();

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        private readonly Bindable<WorkingBeatmap> beatmap = new Bindable<WorkingBeatmap>();

        private Playlist playlist;
        private FilterControl filter;

        [BackgroundDependencyLoader]
        private void load(Bindable<WorkingBeatmap> diBeatmap)
        {
            beatmap.BindTo(diBeatmap);
            RelativeSizeAxes = Axes.Both;

            Children = new Drawable[]
            {
                playlist = new Playlist
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 95, Bottom = 10, Right = 10 },
                    RequestSelection = itemSelected
                },
                filter = new FilterControl
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    FilterChanged = criteria => playlist.Filter(criteria),
                    Padding = new MarginPadding(10)
                }
            };

            // filter.Search.OnCommit += (sender, newText) =>
            // {
            //     BeatmapInfo toSelect = playlist.FirstVisibleSet?.Beatmaps?.FirstOrDefault();

            //     if (toSelect != null)
            //     {
            //         beatmap.Value = beatmaps.GetWorkingBeatmap(toSelect);
            //         beatmap.Value.Track.Restart();
            //     }
            // };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            playlist.Items.BindTo(beatmapSets);
            beatmap.BindValueChanged(working => playlist.SelectedSet.Value = working.NewValue.BeatmapSetInfo, true);
        }

        private void itemSelected(BeatmapSetInfo set)
        {
            if (set.ID == (beatmap.Value?.BeatmapSetInfo?.ID ?? -1))
            {
                beatmap.Value?.Track.Seek(0);
                return;
            }

            beatmap.Value = beatmaps.GetWorkingBeatmap(set.Beatmaps.First());
            beatmap.Value.Track.Restart();
        }
    }
}
