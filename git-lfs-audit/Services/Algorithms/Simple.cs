namespace SST.GitLfsAudit.Services.Algorithms
{
    /// <summary>
    /// Implementiert einen einfachen Algorithmus zur Erkennung von Textdateien.
    /// Die Erkennung erfolgt anhand vordefinierter Dateiendungen und einer Byte-Analyse.
    /// </summary>
    /// <remarks>
    /// Achtung! Dieser Algorithmus funktioniert nur bei ASCI und UTF8 Dateien.
    /// Bei UTF16 und UTF32 Dateien kann es zu Fehlentscheidungen kommen, da diese oft Nullbytes enthalten.
    /// </remarks>
    internal class Simple : IService
    {
        /// <summary>
        /// Puffer für die Byte-Analyse einer Datei.
        /// </summary>
        private readonly byte[] _buffer;

        /// <summary>
        /// Gibt an, ob die Optimierung mit vordefinierten Endungen aktiviert ist.
        /// </summary>
        private readonly bool _optimize;

        /// <summary>
        /// Vordefinierte Listen von Text- und Binär-Endungen.
        /// </summary>
        private readonly GitLfsAudit.Services.ExtensionPresets.IService _extensionPresets;

        /// <summary>
        /// Initialisiert eine neue Instanz der <see cref="Simple"/>-Klasse.
        /// Liest die Optionen und Endungsvorgaben aus dem IOC-Container.
        /// </summary>
        public Simple()
        {
            var service = Ioc.GetService<GitLfsAudit.Services.Options.IService>();

            _optimize = service.Options.Optimize;
            _buffer = new byte[service.Options.AnalyseBytes];
            _extensionPresets = Ioc.GetService<ExtensionPresets.IService>();
        }

        /// <summary>
        /// Prüft, ob die angegebene Datei als Textdatei eingestuft werden kann.
        /// Die Erkennung erfolgt zuerst anhand der Endung, dann anhand der Datei-Bytes.
        /// </summary>
        /// <param name="fileInfo">Die zu prüfende Datei.</param>
        /// <returns>
        /// <c>true</c>, wenn die Datei als Textdatei erkannt wird; andernfalls <c>false</c>.
        /// </returns>
        public bool IsTextFile(FileInfo fileInfo)
        {
            // Bestimmung anhand der Listen
            var ext = fileInfo.Extension;
            if (_optimize)
            {
                if (_extensionPresets.Text.Contains(ext))
                {
                    return true;
                }
                if (_extensionPresets.Binary.Contains(ext))
                {
                    return false;
                }
            }

            // Bestimmung anhand der Bytes.
            try
            {
                using (var stream = fileInfo.OpenRead())
                {
                    var bytesRead = stream.Read(_buffer, 0, _buffer.Length);
                    for (var i = 0; i < bytesRead; i++)
                    {
                        var b = _buffer[i];

                        // Erlaubte Textzeichen: 
                        // Tab (9), LF (10), CR (13), 32-126 (sichtbare ASCII-Zeichen)
                        // Optional: UTF-8 erlaubt auch Bytes über 127, deshalb kann man hier erweitern

                        if (b == 0) // Nullbyte ist meist binär
                        {
                            return false;
                        }

                        if (b < 7 || (b > 13 && b < 32)) // Steuerzeichen außerhalb üblichen Whitespaces
                        {
                            return false;
                        }
                    }

                    return true; // Wenn keine verbotenen Bytes gefunden wurden, als Textdatei werten
                }
            }
            catch (Exception)
            {
                // Im Fehlerfall lieber false zurückgeben
                return false;
            }
        }
    }
}
