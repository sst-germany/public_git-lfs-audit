using Microsoft.Extensions.DependencyInjection;

namespace SST.GitLfsAudit.Services
{

    /// <summary>
    /// Simple and lightweight implementation of Inversion of Control (IoC) for dependency injection.
    /// </summary>
    public static class Ioc
    {
        private static ServiceProvider? _provider = null!;

        /// <summary>
        /// Der globale ServiceProvider.
        /// </summary>
        public static IServiceProvider Default
        {
            get
            {
                if (_provider == null)
                {
                    throw new InvalidOperationException("Ioc wurde noch nicht konfiguriert.");
                }

                return _provider;
            }
        }

        /// <summary>
        /// Initialisiert den ServiceProvider mit den angegebenen Registrierungen.
        /// </summary>
        public static void ConfigureServices(Action<IServiceCollection> configure)
        {
            if (_provider != null)
            {
                throw new InvalidOperationException("Ioc wurde bereits konfiguriert.");
            }

            var services = new ServiceCollection();
            configure(services);
            _provider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Shortcut für typsichere Serviceauflösung.
        /// </summary>
        public static T GetService<T>() where T : notnull
        {
            return Default.GetRequiredService<T>();
        }

        /// <summary>
        /// Shortcut für optionale Serviceauflösung (kann null zurückgeben).
        /// </summary>
        public static T? TryGetService<T>()
        {
            return Default.GetService<T>();
        }

        /// <summary>
        /// Beendet den ServiceProvider und gibt alle verwalteten Ressourcen frei.
        /// </summary>
        public static void Terminate()
        {
            var p = _provider;
            _provider = null;
            p?.Dispose();
        }
    }
}
