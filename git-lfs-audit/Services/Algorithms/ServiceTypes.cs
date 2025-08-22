namespace SST.GitLfsAudit.Services.Algorithms
{
    /// <summary>
    /// Definiert die verfügbaren Algorithmus-Konfigurationen zur Erkennung von Textdateien.
    /// Wird verwendet, um zwischen verschiedenen Implementierungen zu wählen.
    /// </summary>
    public enum ServiceTypes
    {
        /// <summary>
        /// Nutzt den einfachen Algorithmus zur Textdatei-Erkennung (Extension- und Byte-Analyse).
        /// </summary>
        Simple,

        /// <summary>
        /// Nutzt den BOM-basierten Algorithmus zur Textdatei-Erkennung (Erkennung von Byte Order Marks und Heuristiken).
        /// </summary>
        BomBased,

        /// <summary>
        /// Standard-Einstellung: Verweist auf <see cref="BomBased"/>.
        /// </summary>
        DefaultSetting = BomBased,
    }
}
