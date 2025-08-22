namespace SST.GitLfsAudit.Services.ExtensionPresets
{
    /// <summary>
    /// Stellt die Standard-Erweiterungen für Text- und Binärdateien in Unity-Projekten bereit.
    /// </summary>
    internal class UnityProject : IService
    {
        /// <summary>
        /// Liste der als Textdateien behandelten Erweiterungen.
        /// </summary>
        public HashSet<string> Text => _textExtensions;

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
        /// Liste der als Binärdateien behandelten Erweiterungen.
        /// </summary>
        public HashSet<string> Binary => _binaryExtensions;

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
