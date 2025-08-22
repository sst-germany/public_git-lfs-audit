namespace SST.GitLfsAudit.Services.ExtensionPresets
{
    /// <summary>
    /// Schnittstelle für Dienstklassen, die Text- und Binärdateierweiterungen bereitstellen.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Enthält die Dateierweiterungen, die als Textdateien behandelt werden.
        /// </summary>
        HashSet<string> Text { get; }

        /// <summary>
        /// Enthält die Dateierweiterungen, die als Binärdateien behandelt werden.
        /// </summary>
        HashSet<string> Binary { get; }
    }
}
