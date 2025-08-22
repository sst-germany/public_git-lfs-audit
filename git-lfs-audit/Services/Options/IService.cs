namespace SST.GitLfsAudit.Services.Options
{
    /// <summary>
    /// Definiert eine Dienstschnittstelle mit Zugriff auf die zugehörigen Optionen.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Ruft die Optionen für den Dienst ab.
        /// </summary>
        Options Options { get; }
    }
}
