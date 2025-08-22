namespace SST.GitLfsAudit.Services.Algorithms
{
    /// <summary>
    /// Implementiert einen Algorithmus zur Erkennung von Textdateien anhand von BOM (Byte Order Mark) und Heuristiken.
    /// Erkennt verschiedene Textformate wie UTF-8, UTF-16, UTF-32 sowie ASCII/ANSI.
    /// </summary>
    internal class BomBased : IService
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
        /// Initialisiert eine neue Instanz der <see cref="BomBased"/>-Klasse.
        /// Liest die Optionen und Endungsvorgaben aus dem IOC-Container.
        /// </summary>
        public BomBased()
        {
            var service = Ioc.GetService<GitLfsAudit.Services.Options.IService>();

            _optimize = service.Options.Optimize;
            _buffer = new byte[service.Options.AnalyseBytes];
            _extensionPresets = Ioc.GetService<ExtensionPresets.IService>();
        }


        /// <summary>
        /// Prüft, ob die angegebene Datei als Textdatei eingestuft werden kann.
        /// Die Erkennung erfolgt anhand von BOM, UTF-16-Heuristik, UTF-8-Validierung und ASCII-Prüfung.
        /// </summary>
        /// <param name="fileInfo">Die zu prüfende Datei.</param>
        /// <returns>
        /// <c>true</c>, wenn die Datei als Textdatei erkannt wird; andernfalls <c>false</c>.
        /// </returns>
        public bool IsTextFile(FileInfo fileInfo)
        {
            // Bestimmung anhand der Listen
            if (_optimize)
            {
                var ext = fileInfo.Extension;
                if (_extensionPresets.Text.Contains(ext))
                {
                    return true;
                }
                if (_extensionPresets.Binary.Contains(ext))
                {
                    return false;
                }
            }

            try
            {
                using (var stream = fileInfo.OpenRead())
                {
                    var bytesRead = stream.Read(_buffer, 0, _buffer.Length);

                    // 1. BOM-Erkennung
                    if (bytesRead >= 3 &&
                        _buffer[0] == 0xEF && _buffer[1] == 0xBB && _buffer[2] == 0xBF)
                    {
                        return true; // UTF-8 BOM
                    }

                    if (bytesRead >= 2 &&
                        _buffer[0] == 0xFF && _buffer[1] == 0xFE)
                    {
                        return true; // UTF-16 LE BOM
                    }

                    if (bytesRead >= 2 &&
                        _buffer[0] == 0xFE && _buffer[1] == 0xFF)
                    {
                        return true; // UTF-16 BE BOM
                    }

                    if (bytesRead >= 4 &&
                        _buffer[0] == 0xFF && _buffer[1] == 0xFE &&
                        _buffer[2] == 0x00 && _buffer[3] == 0x00)
                    {
                        return true; // UTF-32 LE BOM
                    }

                    if (bytesRead >= 4 &&
                        _buffer[0] == 0x00 && _buffer[1] == 0x00 &&
                        _buffer[2] == 0xFE && _buffer[3] == 0xFF)
                    {
                        return true; // UTF-32 BE BOM
                    }

                    // Die war leider recht fehleranfällig
                    //// 2. Heuristik für UTF-16 ohne BOM
                    //var nullCountEven = 0;
                    //var nullCountOdd = 0;
                    //for (var i = 0; i < bytesRead; i++)
                    //{
                    //    if (_buffer[i] == 0x00)
                    //    {
                    //        if (i % 2 == 0) nullCountEven++;
                    //        else nullCountOdd++;
                    //    }
                    //}
                    //// Wenn >30% der geraden oder ungeraden Bytes Null sind → vermutlich UTF-16
                    //if (nullCountEven > bytesRead * 0.3 || nullCountOdd > bytesRead * 0.3)
                    //    return true;

                    // 3. Prüfen ob gültiges UTF-8 (ohne BOM)
                    if (IsValidUtf8(_buffer, bytesRead))
                    {
                        return true;
                    }

                    // 4. Allgemeiner Binär-Check (ANSI/ASCII)
                    var countHigherCodes = 0;
                    for (var i = 0; i < bytesRead; i++)
                    {
                        var b = _buffer[i];
                        if (b == 9) // TAB (0x09)
                        {
                            continue;
                        }

                        if (b == 10) // LF (0x0A)
                        {
                            continue;
                        }

                        if (b == 13) // CR (0x0D)
                        {
                            continue;
                        }

                        if (b >= 32 && b <= 126) // (sichtbare ASCII-Zeichen)
                        {
                            continue;
                        }

                        if (b >= 128) // (Sonder-ASCII-Zeichen)
                        {
                            countHigherCodes++;
                            continue;
                        }

                        return false;
                    }
                    // Heuristic: Wenn mehr als 10% Sonderzeichen, dann ist hier etwas wahrscheinlich nicht in Ordnung.
                    if (countHigherCodes > bytesRead * 0.1)
                    {
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Prüft, ob die angegebenen Bytes eine gültige UTF-8-Codierung darstellen.
        /// </summary>
        /// <param name="buffer">Das Byte-Array mit den zu prüfenden Daten.</param>
        /// <param name="length">Die Anzahl der zu prüfenden Bytes.</param>
        /// <returns>
        /// <c>true</c>, wenn die Bytes eine gültige UTF-8-Codierung darstellen; andernfalls <c>false</c>.
        /// </returns>
        private static bool IsValidUtf8(byte[] buffer, int length)
        {
            var i = 0;
            var nullCount = 0;

            while (i < length)
            {
                var b = buffer[i];
                int remainingBytes;
                int codepoint;

                // ASCII (0xxxxxxx)
                if ((b & 0b1000_0000) == 0)
                {
                    codepoint = b;
                    if (codepoint == 0)
                    {
                        nullCount++;
                    }

                    i++;
                    continue;
                }
                // 2-byte (110xxxxx 10xxxxxx)
                else if ((b & 0b1110_0000) == 0b1100_0000)
                {
                    remainingBytes = 1;
                    if (i + remainingBytes >= length)
                    {
                        return false;
                    }

                    var b2 = buffer[i + 1];
                    if ((b2 & 0b1100_0000) != 0b1000_0000)
                    {
                        return false;
                    }

                    codepoint = ((b & 0b0001_1111) << 6) | (b2 & 0b0011_1111);
                    if (codepoint < 0x80)
                    {
                        return false; // Overlong
                    }

                    i += 2;
                }
                // 3-byte (1110xxxx 10xxxxxx 10xxxxxx)
                else if ((b & 0b1111_0000) == 0b1110_0000)
                {
                    remainingBytes = 2;
                    if (i + remainingBytes >= length)
                    {
                        return false;
                    }

                    var b2 = buffer[i + 1];
                    var b3 = buffer[i + 2];
                    if ((b2 & 0b1100_0000) != 0b1000_0000 ||
                        (b3 & 0b1100_0000) != 0b1000_0000)
                    {
                        return false;
                    }

                    codepoint = ((b & 0b0000_1111) << 12) |
                                ((b2 & 0b0011_1111) << 6) |
                                (b3 & 0b0011_1111);
                    if (codepoint < 0x800)
                    {
                        return false; // Overlong
                    }

                    if (codepoint >= 0xD800 && codepoint <= 0xDFFF)
                    {
                        return false; // Surrogates
                    }

                    i += 3;
                }
                // 4-byte (11110xxx 10xxxxxx 10xxxxxx 10xxxxxx)
                else if ((b & 0b1111_1000) == 0b1111_0000)
                {
                    remainingBytes = 3;
                    if (i + remainingBytes >= length)
                    {
                        return false;
                    }

                    var b2 = buffer[i + 1];
                    var b3 = buffer[i + 2];
                    var b4 = buffer[i + 3];
                    if ((b2 & 0b1100_0000) != 0b1000_0000 ||
                        (b3 & 0b1100_0000) != 0b1000_0000 ||
                        (b4 & 0b1100_0000) != 0b1000_0000)
                    {
                        return false;
                    }

                    codepoint = ((b & 0b0000_0111) << 18) |
                                ((b2 & 0b0011_1111) << 12) |
                                ((b3 & 0b0011_1111) << 6) |
                                (b4 & 0b0011_1111);
                    if (codepoint < 0x10000)
                    {
                        return false; // Overlong
                    }

                    if (codepoint > 0x10FFFF)
                    {
                        return false; // Max Unicode
                    }

                    i += 4;
                }
                else
                {
                    return false; // Ungültiger Startbyte
                }
            }

            // Heuristik: wenn die Datei hauptsächlich aus Nullbytes besteht → eher binär
            if (nullCount > length * 0.2) // mehr als 20% Nullbytes
            {
                return false;
            }

            return true;
        }
    }
}
