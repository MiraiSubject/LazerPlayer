using LazerPlayer.Music;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.IO;
using osu.Game.Resources;
using osu.Game.Rulesets;

namespace LazerPlayer
{
    public class LazerPlayerBase : Game
    {
        protected override Container<Drawable> Content { get; }
        protected Storage Storage { get; set; }
        protected RulesetStore RulesetStore;
        protected OsuConfigManager LocalConfig;
        protected MusicController MusicController;
        public LazerPlayerBase()
        {
            Name = "Music Player for osu!lazer";
            base.Content.Add(Content = new DrawSizePreservingFillContainer());
        }
        private DependencyContainer dependencies;
        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        private DatabaseContextFactory contextFactory;
        public BeatmapManager BeatmapManager;
        protected Bindable<WorkingBeatmap> Beatmap { get; private set; } // cached via load() method

        [BackgroundDependencyLoader]
        private void load()
        {
            Resources.AddStore(new DllResourceStore(OsuResources.ResourceAssembly));
            var kwResources = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(LazerPlayerBase).Assembly), @"Resources");
            Resources.AddStore(kwResources);
            AddFont(Resources, @"Fonts/OpenSans/OpenSans-Regular");
            AddFont(Resources, @"Fonts/Noto-Basic");
            AddFont(Resources, @"Fonts/Noto-Hangul");
            AddFont(Resources, @"Fonts/Noto-CJK-Basic");
            AddFont(Resources, @"Fonts/Noto-CJK-Compatibility");

            dependencies.Cache(contextFactory = new DatabaseContextFactory(Storage));
            dependencies.Cache(Storage);

            var defaultBeatmap = new DummyWorkingBeatmap(Audio, Textures);
            dependencies.Cache(BeatmapManager = new BeatmapManager(Storage, contextFactory, RulesetStore, null, Audio, Host, defaultBeatmap, false));
            dependencies.Cache(RulesetStore = new RulesetStore(contextFactory, Storage));

            Beatmap = new NonNullableBindable<WorkingBeatmap>(defaultBeatmap);
            dependencies.CacheAs<IBindable<WorkingBeatmap>>(Beatmap);
            dependencies.CacheAs(Beatmap);

            AddInternal(MusicController = new MusicController());
            dependencies.Cache(MusicController);
            dependencies.Cache(new OsuColour());

            dependencies.Cache(new SessionStatics());

            PreviewTrackManager previewTrackManager;
            dependencies.Cache(previewTrackManager = new PreviewTrackManager());
            Add(previewTrackManager);
        }

        protected override Storage CreateStorage(GameHost host, Storage defaultStorage) => new OsuStorage(host, defaultStorage);

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);
            Storage ??= host.Storage;

            LocalConfig ??= new DevelopmentOsuConfigManager(Storage);
        }
    }
}
