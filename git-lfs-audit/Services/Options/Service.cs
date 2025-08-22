namespace SST.GitLfsAudit.Services.Options
{
    /// <summary>
    /// Implementiert die IService-Schnittstelle und verwaltet die Options-Instanz.
    /// </summary>
    internal class Service : IService
    {
        private Options _options;

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="Service"/>-Klasse mit den angegebenen Optionen.
        /// </summary>
        /// <param name="options">Die zu verwendenden Optionen.</param>
        public Service(Options options)
        {
            _options = options;
        }

        /// <summary>
        /// Gibt die aktuell verwendeten Optionen zurück.
        /// </summary>
        public Options Options => _options;
    }
}
