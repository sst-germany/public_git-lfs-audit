namespace SST.GitLfsAudit.Services.ExtensionPresets
{
    /// <summary>
    /// Stellt eine einfache Implementierung von <see cref="IService"/> bereit,
    /// die vordefinierte Listen von Text- und Binärdateierweiterungen enthält.
    /// </summary>
    internal class Simple : IService
    {
        /// <summary>
        /// Gibt die Menge der als Textdateien klassifizierten Erweiterungen zurück.
        /// </summary>
        public HashSet<string> Text => _textExtensions;

        /// <summary>
        /// Interne Liste der Textdateierweiterungen (case-insensitive).
        /// </summary>
        private readonly HashSet<string> _textExtensions = new(StringComparer.OrdinalIgnoreCase)
            {
                ".txt",
                ".c",
                ".config",
                ".h",
                ".hpp",
                ".cpp",
                ".csproj",
                ".sln",
                ".bat",
                ".xsd",
                ".md",
                ".json",
                ".xml",
                ".gitignore",
                ".gitattributes",
                ".gitkeep",
            };

        /// <summary>
        /// Gibt die Menge der als Binärdateien klassifizierten Erweiterungen zurück.
        /// </summary>
        public HashSet<string> Binary => _binaryExtensions;

        /// <summary>
        /// Interne Liste der Binärdateierweiterungen (case-insensitive).
        /// </summary>
        private readonly HashSet<string> _binaryExtensions = new(StringComparer.OrdinalIgnoreCase)
            {
                ".exe",
                ".asset", // Häufig nur Text, aber eben auch BIN.
                ".assets", // Häufig nur Text, aber eben auch BIN.
                ".com",
                ".dll",
                ".pdb",
                ".lib",
                ".zip",
                ".7z",
                ".rar",
                ".nupkg",
                ".png",
                ".gif",
                ".tif",
                ".tga",
                ".ttf",
                ".tiff",
                ".bmp",
                ".ico",
                ".jpg",
                ".jpeg",
                ".dds",
                ".wav",
                ".mp3",
                ".mp4",
                ".mpg",
                ".mpeg",
                ".wmv",
                ".avi",
                ".ogg",
                ".fbx",
                ".bytes",
                ".xls",
                ".doc",
            };
    }
}
