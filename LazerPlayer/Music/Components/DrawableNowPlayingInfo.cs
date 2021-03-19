using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;

namespace LazerPlayer.Music.Components
{
    public class DrawableNowPlayingInfo : CompositeDrawable
    {
        public static readonly string NULL_TITLE = "Unknown Song";
        public static readonly string NULL_ARTIST = "Unknown Artist";
        public static readonly string NULL_SOURCE = " ";
        private readonly SpriteText title;
        public LocalisableString Title
        {
            get => title.Text;
            set
            {
                if (title != null)
                    title.Text = value;
            }
        }

        private readonly SpriteText artist;
        public LocalisableString Artist
        {
            get => artist.Text;
            set
            {
                if (artist != null)
                    artist.Text = value;
            }
        }

        private readonly SpriteText source;
        public LocalisableString Source
        {
            get => source.Text;
            set
            {
                if (string.IsNullOrEmpty(value.ToString()))
                {
                    source.Text = string.Empty;
                    Schedule(() => separator.Hide());
                    return;
                }

                if (source != null)
                    source.Text = value;

                Schedule(() => separator.Show());
            }
        }

        private readonly SpriteText currentTime;

        public double CurrentTime
        {
            set => currentTime.Text = ToTimeFormattedString(value);
        }

        private readonly SpriteText endTime;

        public double EndTime
        {
            set => endTime.Text = ToTimeFormattedString(value);
        }

        public static string ToTimeFormattedString(double ms)
        {
            var ts = TimeSpan.FromMilliseconds(ms);
            return $"{(ts < TimeSpan.Zero ? "-" : string.Empty)}{ts:mm\\:ss}";
        }

        private readonly MetadataSeparator separator;

        public DrawableNowPlayingInfo()
        {
            AddRangeInternal(new Drawable[]
            {
                new Container
                {
                    Padding = new MarginPadding(10),
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Children = new Drawable[]
                    {
                        currentTime = new SpriteText
                        {
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                        },
                        endTime = new SpriteText
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.TopRight,
                        }
                    }
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Direction = FillDirection.Vertical,
                    Spacing = new osuTK.Vector2(15),
                    Children = new Drawable[]
                    {
                        title = new MetadataInfo
                        {
                            Text = NULL_TITLE
                        },
                        new FillFlowContainer<SpriteText>
                        {
                            Direction = FillDirection.Horizontal,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Children = new[]
                            {
                                artist = new MetadataInfo
                                {
                                    Text = NULL_ARTIST
                                },
                                separator = new MetadataSeparator(),
                                source = new MetadataInfo
                                {
                                    Text = NULL_SOURCE
                                }
                            }
                        },
                    }
                }
            });
        }

        private class MetadataInfo : SpriteText
        {
            public MetadataInfo()
            {
                Origin = Anchor.Centre;
                Anchor = Anchor.Centre;
            }
        }

        private class MetadataSeparator : MetadataInfo
        {
            public MetadataSeparator()
            {
                Text = " - ";
            }
        }
    }
}
