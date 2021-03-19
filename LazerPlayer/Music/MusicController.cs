using System;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using osu.Game.Beatmaps;

namespace LazerPlayer.Music
{
    // Remake of this class was required because osu! side has DI'd mods and hooks for removed and added beatmaps.
    // We don't need any of those.
    // Reference: https://github.com/ppy/osu/blob/d54e9ab4810ef27dd9c3ef9c8c1a6c851cff8972/osu.Game/Overlays/MusicController.cs
    public class MusicController : CompositeDrawable
    {
        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; }
        private readonly BindableList<BeatmapSetInfo> beatmapSets = new BindableList<BeatmapSetInfo>();

        public IBindableList<BeatmapSetInfo> BeatmapSets
        {
            get
            {
                if (LoadState < LoadState.Ready)
                    throw new InvalidOperationException($"{nameof(BeatmapSets)} should not be accessed before the music controller is loaded.");

                return beatmapSets;
            }
        }

        [NotNull]
        public DrawableTrack CurrentTrack { get; private set; } = new DrawableTrack(new TrackVirtual(1000));

        public bool IsPlaying => CurrentTrack.IsRunning;
        public bool TrackLoaded => CurrentTrack.TrackLoaded;

        /// <summary>
        /// Fired when the global <see cref="WorkingBeatmap"/> has changed.
        /// Includes direction information for display purposes.
        /// </summary>
        public event Action<WorkingBeatmap, TrackChangeDirection> TrackChanged;


        [BackgroundDependencyLoader]
        private void load()
        {
            beatmapSets.AddRange(beatmaps.GetAllUsableBeatmapSets(IncludedDetails.Minimal, true).OrderBy(t => t.Metadata.Title));
            beatmap.BindValueChanged(beatmapChanged, true);
        }

        public bool Play(bool restart = false)
        {
            if (restart)
                CurrentTrack.Restart();
            else
                CurrentTrack.Start();

            return true;
        }
        public void Pause() => CurrentTrack.Stop();

        public bool Stop()
        {
            Pause();
            SeekTo(0);
            return true;
        }

        public bool TogglePlay()
        {
            if (CurrentTrack.IsRunning)
                Pause();
            else
                Play();

            return true;
        }
        private WorkingBeatmap current;
        private ScheduledDelegate seekDelegate;

        public void SeekTo(double pos)
        {
            seekDelegate?.Cancel();
            seekDelegate = Schedule(() => CurrentTrack.Seek(pos));
        }

        private void onTrackCompleted(WorkingBeatmap workingBeatmap)
        {
            if (current != workingBeatmap)
                return;

            if (!CurrentTrack.Looping)
                NextTrack();
        }

        private const int restart_cutoff_point = 5000;

        public void PreviousTrack(Action<PreviousTrackResult> onSuccess = null) => Schedule(() =>
        {
            PreviousTrackResult res = previousTrack();
            if (res != PreviousTrackResult.None)
                onSuccess?.Invoke(res);

        });

        private PreviousTrackResult previousTrack()
        {
            if (beatmap.Disabled)
                return PreviousTrackResult.None;

            var currentTrackPosition = CurrentTrack.CurrentTime;

            if (currentTrackPosition >= restart_cutoff_point)
            {
                SeekTo(0);
                return PreviousTrackResult.Restart;
            }

            queuedDirection = TrackChangeDirection.Previous;

            var playable = BeatmapSets.TakeWhile(i => i.ID != current.BeatmapSetInfo.ID).LastOrDefault() ?? BeatmapSets.LastOrDefault();

            if (playable != null)
            {
                changeBeatmap(beatmaps.GetWorkingBeatmap(playable.Beatmaps.First(), beatmap.Value));
                restartTrack();
                return PreviousTrackResult.Previous;
            }

            return PreviousTrackResult.None;
        }

        public void NextTrack(Action onSuccess = null) => Schedule(() =>
        {
            if (nextTrack())
                onSuccess?.Invoke();
        });

        private bool nextTrack(bool resetList = true)
        {
            var playable = BeatmapSets.SkipWhile(i => i.ID != current.BeatmapSetInfo.ID).ElementAtOrDefault(1);

            if (playable == null)
            {
                if (resetList)
                    playable = BeatmapSets.FirstOrDefault();
                else
                    return false;
            }
            changeBeatmap(beatmaps.GetWorkingBeatmap(playable.Beatmaps.First(), beatmap.Value));
            restartTrack();
            return true;

        }

        private void restartTrack() => Schedule(() => CurrentTrack.Restart());
        private TrackChangeDirection? queuedDirection;

        private void beatmapChanged(ValueChangedEvent<WorkingBeatmap> beatmap) => changeBeatmap(beatmap.NewValue);

        private void changeBeatmap(WorkingBeatmap newMap)
        {
            if (newMap == current)
                return;

            var lastWorkingMap = current;

            TrackChangeDirection direction = TrackChangeDirection.None;

            bool audioEquals = newMap?.BeatmapInfo.AudioEquals(current?.BeatmapInfo) ?? false;

            if (current != null)
            {
                if (audioEquals)
                    direction = TrackChangeDirection.None;
                else if (queuedDirection.HasValue)
                {
                    direction = queuedDirection.Value;
                    queuedDirection = null;
                }
                else
                {
                    // figure out the best direction based on order in playlist.
                    var last = BeatmapSets.TakeWhile(b => b.ID != current.BeatmapSetInfo?.ID).Count();
                    var next = newMap == null ? -1 : BeatmapSets.TakeWhile(b => b.ID != newMap.BeatmapSetInfo?.ID).Count();

                    direction = last > next ? TrackChangeDirection.Previous : TrackChangeDirection.Next;
                }
            }

            current = newMap;

            if (!audioEquals || CurrentTrack.IsDummyDevice)
            {
                changeTrack();
            }
            else
            {
                // transfer still valid track to new working beatmap
                current.TransferTrack(lastWorkingMap.Track);
            }

            TrackChanged?.Invoke(current, direction);

            queuedDirection = null;

            if (beatmap.Value != current && beatmap is Bindable<WorkingBeatmap> working)
                working.Value = current;
        }

        private void changeTrack()
        {
            var lastTrack = CurrentTrack;

            var queuedTrack = new DrawableTrack(current.LoadTrack());
            queuedTrack.Completed += () => onTrackCompleted(current);

            CurrentTrack = queuedTrack;

            Schedule(() =>
            {
                lastTrack.Expire();

                if (queuedTrack == CurrentTrack)
                    AddInternal(queuedTrack);
                else
                    queuedTrack.Dispose();
            });
        }
    }

    public enum PreviousTrackResult
    {
        None, Restart, Previous
    }

    public enum TrackChangeDirection
    {
        None, Next, Previous
    }

    public enum PlaylistState
    {
        Playing, EndOfList
    }

    public enum Repeat
    {
        None, List, Solo
    }
}
