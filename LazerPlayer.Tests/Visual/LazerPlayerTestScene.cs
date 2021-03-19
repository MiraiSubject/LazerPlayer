using System;
using System.IO;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Database;
using LazerPlayer.Music;

namespace LazerPlayer.Tests.Visual
{
    [ExcludeFromDynamicCompile]
    public abstract class LazerPlayerTestScene : TestScene
    {
        private Lazy<Storage> localStorage;
        protected Storage LocalStorage => localStorage.Value;

        private Lazy<DatabaseContextFactory> contextFactory;
        protected DatabaseContextFactory ContextFactory => contextFactory.Value;
        protected virtual bool UseFreshStoragePerRun => false;

        [Resolved]
        protected MusicController MusicController { get; private set; }

        /// <summary>
        /// When running headless, there is an opportunity to use the host storage rather than creating a second isolated one.
        /// This is because the host is recycled per TestScene execution in headless at an nunit level.
        /// </summary>
        private Storage isolatedHostStorage;
        private DependencyContainer dependencies;
        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            if (!UseFreshStoragePerRun)
                isolatedHostStorage = (parent.Get<GameHost>() as HeadlessGameHost)?.Storage;

            contextFactory = new Lazy<DatabaseContextFactory>(() =>
            {
                var factory = new DatabaseContextFactory(LocalStorage);

                // only reset the database if not using the host storage.
                // if we reset the host storage, it will delete global key bindings.
                if (isolatedHostStorage == null)
                    factory.ResetDatabase();

                using (var usage = factory.Get())
                    usage.Migrate();
                return factory;
            });

            RecycleLocalStorage();

            return dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
        }

        public virtual void RecycleLocalStorage()
        {
            if (localStorage?.IsValueCreated == true)
            {
                try
                {
                    localStorage.Value.DeleteDirectory(".");
                }
                catch
                {
                    // we don't really care if this fails; it will just leave folders lying around from test runs.
                }
            }

            localStorage =
                new Lazy<Storage>(() => isolatedHostStorage ?? new NativeStorage(Path.Combine(RuntimeInfo.StartupDirectory, $"{GetType().Name}-{Guid.NewGuid()}")));
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (MusicController?.TrackLoaded == true)
                MusicController.Stop();
            
            if (contextFactory?.IsValueCreated == true)
                contextFactory.Value.ResetDatabase();
            
            RecycleLocalStorage();
        }
    }
}