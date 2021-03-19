using System;
using System.Threading.Tasks;
using LazerPlayer.Music.Components;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;

namespace LazerPlayer.Music
{
    public class BottomMediaBar : Container
    {
        [Resolved]
        private MusicController musicController { get; set; }

        [Resolved]
        private Bindable<WorkingBeatmap> beatmap { get; set; }

        private PlaylistSection playlist;
        private DrawableNowPlayingInfo nowPlayingInfo;
        private IconButton playButton;
        private Background background;
        private Container richMetadata;

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new Container
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.1f,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.FromHex("252526")
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Width = 0.25f,
                            Children = new Drawable[]
                            {
                                new FillFlowContainer<IconButton>
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new osuTK.Vector2(5),
                                    Children = new[]
                                    {
                                        new IconButton
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Icon = FontAwesome.Solid.StepBackward,
                                            Action = () => musicController.PreviousTrack()
                                        },
                                        playButton = new IconButton
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Icon = FontAwesome.Solid.Play,
                                            Action = () => musicController.TogglePlay()

                                        },
                                        new IconButton
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Icon = FontAwesome.Solid.StepForward,
                                            Action = () => musicController.NextTrack()
                                        }
                                    }
                                }
                            }
                        },
                        richMetadata = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Width = 0.5f,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                nowPlayingInfo = new DrawableNowPlayingInfo
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre
                                }
                            }
                        },
                    }
                },
                playlist = new PlaylistSection
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Height = 0.9f
                }
            };

            playlist.BeatmapSets.BindTo(musicController.BeatmapSets);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            musicController.TrackChanged += trackChanged;
            trackChanged(beatmap.Value);

            richMetadata.Add(background = new Background());
            background.WorkingBeatmap.BindTo(beatmap);

            var blurredBg = new Background();
            blurredBg.WorkingBeatmap.BindTo(beatmap);
            Add(blurredBg);
        }

        protected override void Update()
        {
            base.Update();

            if (pendingBeatmapSwitch != null)
            {
                pendingBeatmapSwitch();
                pendingBeatmapSwitch = null;
            }

            var track = musicController.CurrentTrack;

            if (!track.IsDummyDevice)
            {
                playButton.Icon = track.IsRunning ? FontAwesome.Solid.Pause : FontAwesome.Solid.Play;
                nowPlayingInfo.CurrentTime = track.CurrentTime;
                nowPlayingInfo.EndTime = track.Length;
            }
            else
            {
                playButton.Icon = FontAwesome.Solid.Play;
            }
        }

        private Action pendingBeatmapSwitch;
        private void trackChanged(WorkingBeatmap beatmap, TrackChangeDirection direction = TrackChangeDirection.None)
        {
            pendingBeatmapSwitch = () =>
            {
                Task.Run(() =>
                {
                    if (beatmap?.Beatmap == null)
                    {
                        nowPlayingInfo.Title = DrawableNowPlayingInfo.NULL_TITLE;
                        nowPlayingInfo.Artist = DrawableNowPlayingInfo.NULL_TITLE;
                        nowPlayingInfo.Source = DrawableNowPlayingInfo.NULL_SOURCE;
                    }
                    else
                    {
                        BeatmapMetadata metadata = beatmap.Metadata;
                        nowPlayingInfo.Title = new RomanisableString(metadata.TitleUnicode, metadata.Title);
                        nowPlayingInfo.Artist = new RomanisableString(metadata.ArtistUnicode, metadata.Artist);
                        nowPlayingInfo.Source = new RomanisableString(metadata.Source, null);
                    }
                });
            };
        }

        private class Background : Container
        {
            public Bindable<WorkingBeatmap> WorkingBeatmap = new Bindable<WorkingBeatmap>();
            private Sprite background;
            private BufferedContainer bufferedContainer;

            public Background()
            {
                Depth = float.MaxValue;
                RelativeSizeAxes = Axes.Both;
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                bufferedContainer = new BufferedContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = background = new Sprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Texture = WorkingBeatmap.Value?.Background
                    }
                };

                Children = new Drawable[]
                {
                    bufferedContainer,
                    new BufferedContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        BlurSigma = new osuTK.Vector2(10),
                        Children = new Drawable[]
                        {
                            bufferedContainer.CreateView().With(d =>
                            {
                                d.RelativeSizeAxes = Axes.Both;
                                d.SynchronisedDrawQuad = true;
                            }),
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = Colour4.Black,
                                Alpha = 0.6f
                            }
                        }
                    }
                };

                WorkingBeatmap.ValueChanged += beatmapUpdated;
            }

            private void beatmapUpdated(ValueChangedEvent<WorkingBeatmap> map)
            {
                Drawable newBackground;

                if (map.NewValue.Background == null)
                {
                    newBackground = new Box
                    {
                        Alpha = 0,
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Colour4.FromHex("4A4A4C")
                    };
                }
                else
                {
                    newBackground = new Sprite
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Alpha = 0,
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Texture = map.NewValue.Background
                    };
                }

                bufferedContainer.Add(newBackground);
                background.FadeOut(1000, Easing.Out);
                newBackground.FadeIn(1000, Easing.In);
                background.Expire();
            }
        }
    }
}
