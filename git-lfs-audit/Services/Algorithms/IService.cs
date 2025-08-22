namespace SST.GitLfsAudit.Services.Algorithms
{
    /// <summary>
    /// Definiert die Schnittstelle für Algorithmen zur Erkennung von Textdateien.
    /// Implementierungen dieser Schnittstelle bieten unterschiedliche Strategien,
    /// um zu bestimmen, ob eine Datei als Textdatei eingestuft werden kann.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Prüft, ob die angegebene Datei als Textdatei erkannt wird.
        /// </summary>
        /// <param name="fileInfo">Die zu prüfende Datei.</param>
        /// <returns>
        /// <c>true</c>, wenn die Datei als Textdatei erkannt wird; andernfalls <c>false</c>.
        /// </returns>
        bool IsTextFile(FileInfo fileInfo);
    }
}
