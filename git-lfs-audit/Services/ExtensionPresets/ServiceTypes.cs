namespace SST.GitLfsAudit.Services.ExtensionPresets
{
    /// <summary>
    /// Stellt die verfügbaren Implementierungen für Erweiterungsvoreinstellungen dar.
    /// </summary>
    public enum ServiceTypes
    {
        /// <summary>
        /// Einfache Konfiguration.
        /// </summary>
        Simple,

        /// <summary>
        /// Konfiguration für Unity-Projekte.
        /// </summary>
        UnityProject,

        /// <summary>
        /// Standardkonfiguration (entspricht <see cref="Simple"/>).
        /// </summary>
        DefaultSetting = Simple,
    }
}
