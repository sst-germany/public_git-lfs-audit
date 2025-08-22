using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SST.GitLfsAudit
{

    /// <summary>
    /// Diese Klasse beinhaltet die Konfiguration der gefundenen .gitattributes Datei.
    /// Änderungen über diese Klasse können mit der Methode <see cref="Save"/> zurück auf die Festplatte geschrieben werden.
    /// </summary>
    public class LfsDictionary
    {
        #region nLog instance (s_log)
        private static NLog.Logger s_log { get; } = NLog.LogManager.GetCurrentClassLogger();
        #endregion

        #region public enum States
        public enum States
        {
            Unknown,
            LFS,
            Text,
            Comment,
        }
        #endregion

        #region public class Entry
        public class Entry
        {
            public States State { get; }
            public string Line { get; }
            public string Pattern { get; }


            public Entry(States state, string line, string pattern)
            {
                State = state;
                Line = line;
                Pattern = pattern;
            }


            public bool IsMatch(string relativePath)
            {
                if (State == States.Unknown || State == States.Comment)
                {
                    return false;
                }

                // Normalisiere Slashes
                var normalizedPath = relativePath.
                    Replace(Path.DirectorySeparatorChar, '/').
                    Replace(Path.AltDirectorySeparatorChar, '/');
                var pattern = Pattern.
                    Replace(Path.DirectorySeparatorChar, '/').
                    Replace(Path.AltDirectorySeparatorChar, '/');

                // Falls Pattern keinen Slash hat → nur auf Dateiname matchen
                var target = pattern.Contains("/") ? normalizedPath : Path.GetFileName(normalizedPath);

                // Regex bauen
                var regexPattern = "^" + Regex.Escape(pattern).
                    Replace(@"\*\*", ".*").     // ** = rekursives Match
                    Replace(@"\*", @"[^/]*").   // *  = alles außer Slash
                    Replace(@"\?", @"[^/]")    // ?  = genau ein Zeichen außer Slash
                    + "$";

                var result = Regex.IsMatch(target, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                return result;
            }
            public bool TreatAsText => State == States.Text;
            public bool TreatAsLFS => State == States.LFS;

            public override string ToString()
            {
                return $"{Pattern} | {State} | {Line}";
            }
        }
        #endregion

        public readonly List<Entry> _entries;
        private readonly string? _repoBaseDir;
        private bool _modified = false;
        private readonly object _syncObject = new();


        public LfsDictionary(string? repoBaseDir)
        {
            _entries = new List<Entry>();
            _repoBaseDir = repoBaseDir;

            if (!string.IsNullOrWhiteSpace(_repoBaseDir))
            {
                if (!Directory.Exists(_repoBaseDir))
                {
                    s_log.Error("Directory is not specified.");
                    return;
                }

                var filename = Path.Combine(_repoBaseDir, ".gitattributes");
                if (!File.Exists(filename))
                {
                    s_log.Warn("File .gitattributes not found: {0}", _repoBaseDir);
                    return;
                }

                // Try to read .gitattributes
                try
                {
                    // Alle Zeilen lesen
                    var lines = File.ReadAllLines(filename);

                    // (file)(filter++)(comment?)
                    var regexLFS = new Regex(@"(.*)(?i)(filter\s*=\s*lfs\s+diff\s*=\s*lfs\s+merge\s*=\s*lfs\s+-text)(.*)");
                    var regexTXT = new Regex(@"(.*)(?i)(\s+text)(.*)");

                    // Nun alle Zeilen analysieren
                    foreach (var line in lines)
                    {
                        static bool getPatternTXT(string? line, Regex regex, out Entry? entry)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                entry = null;
                                return false;
                            }

                            var match = regex.Match(line);
                            if (!match.Success)
                            {
                                entry = null;
                                return false;
                            }

                            // Group0 ~ "*.pdf text"
                            // Group1 ~ "*.pdf "
                            // Group2 ~ "text"
                            // Group3 ~ ""

                            // Der RegEx erwartet 4 Gruppen
                            if (match.Groups.Count != 4)
                            {
                                entry = null;
                                return false;
                            }

                            // Gruppe3 muss erfolgreich sein.
                            if (!match.Groups[3].Success)
                            {
                                entry = null;
                                return false;
                            }

                            // Gruppe3 muss leer sein, oder ein Kommentar
                            var group3 = match.Groups[3].Value.Trim();
                            if (group3 != string.Empty && !group3.StartsWith('#'))
                            {
                                entry = null;
                                return false;
                            }

                            // Gruppe2 interessiert uns nicht wirklich, nur wenn es einen Fehler gab.
                            if (!match.Groups[2].Success)
                            {
                                entry = null;
                                return false;
                            }

                            // In Gruppe1 steht das gesuchte Pattern, es könnte aber auch noch ein Kommentar sein.
                            var text = match.Groups[1].Value.Trim();
                            if (text.StartsWith('#'))
                            {
                                entry = new Entry(States.Comment, line, string.Empty);
                                return true;
                            }

                            entry = new Entry(States.Text, line, text);
                            return true;
                        }
                        if (getPatternTXT(line, regex: regexTXT, out var entryTXT) && (entryTXT is not null))
                        {
                            _entries.Add(entryTXT);
                        }
                        else
                        {
                            static void getPatternLFS(string? line, Regex regex, out Entry entry)
                            {
                                if (string.IsNullOrWhiteSpace(line))
                                {
                                    entry = new Entry(States.Unknown, string.Empty, string.Empty);
                                    return;
                                }

                                var match = regex.Match(line);
                                if (!match.Success)
                                {
                                    entry = new Entry(States.Unknown, line, string.Empty);
                                    return;
                                }

                                // Group0 ~ "*.pdf filter=lfs diff=lfs merge=lfs -text"
                                // Group1 ~ "*.pdf "
                                // Group2 ~ "filter=lfs diff=lfs merge=lfs -text"
                                // Group3 ~ ""

                                // Der RegEx erwartet 4 Gruppen
                                if (match.Groups.Count != 4)
                                {
                                    entry = new Entry(States.Unknown, line, string.Empty);
                                    return;
                                }

                                // Gruppe3 muss erfolgreich sein.
                                if (!match.Groups[3].Success)
                                {
                                    entry = new Entry(States.Unknown, line, string.Empty);
                                    return;
                                }

                                // Gruppe3 muss leer sein, oder ein Kommentar
                                var group3 = match.Groups[3].Value.Trim();
                                if (group3 != string.Empty && !group3.StartsWith('#'))
                                {
                                    entry = new Entry(States.Unknown, line, string.Empty);
                                    return;
                                }

                                // Gruppe2 interessiert uns nicht wirklich.
                                Debug.Assert(match.Groups[2].Success);

                                // In Gruppe1 steht das gesuchte Pattern
                                var text = match.Groups[1].Value.Trim();
                                if (text.StartsWith('#'))
                                {
                                    entry = new Entry(States.Comment, line, string.Empty);
                                    return;
                                }

                                entry = new Entry(States.LFS, line, text);
                            }
                            getPatternLFS(line, regex: regexLFS, out var entryLFS);
                            _entries.Add(entryLFS);
                        }
                    }
                }
                catch (Exception ex)
                {
                    s_log.Error(ex, "Unable to read file: {0}", filename);
                }
            }
            else
            {
                {
                    {
                        s_log.Error("Directory IsNullOrWhiteSpace.");
                    }
                }
            }
        }

        /// <summary>
        /// Speichert die Einträge alphabetisch sortiert zurück in die .gitattributes-Datei.
        /// Jeder Eintrag bekommt den Standard-LFS-Filter.
        /// </summary>
        public bool Save()
        {
            lock (_syncObject)
            {
                if (!_modified)
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(_repoBaseDir) || !Directory.Exists(_repoBaseDir))
                {
                    return false;
                }

                try
                {
                    var filename = Path.Combine(_repoBaseDir, ".gitattributes");

                    File.WriteAllLines(filename, _entries.Select(x => x.Line));
                    return true;
                }
                catch (Exception ex)
                {
                    s_log.Error(ex, "Unable to write file: {0} (.gitattributes)", _repoBaseDir);
                    return false;
                }
            }
        }
        public Entry? GetLatestMatch(FileInfo fileInfo)
        {
            lock (_syncObject)
            {

                // Wenn das _repoBaseDir ungültig ist, dann können wir nicht wirklich prüfen. Wir betrachten deshalb alles als No-Match.
                if (string.IsNullOrWhiteSpace(_repoBaseDir))
                {
                    // Da "_repoBaseDir" nicht gültig ist, kann es auch keinen Eintrag geben.
                    return null;
                }

                // Prüfen ob die angegebene Datei im Repository liegt. Wenn nicht, dann ist die angefragte Datei ungültig.
                if (!fileInfo.FullName.StartsWith(_repoBaseDir, StringComparison.OrdinalIgnoreCase))
                {
                    // Da "fileInfo" nicht gültig ist, kann es auch keinen Eintrag geben.
                    return null;
                }

                // Pfad relativ zum Repo.
                var relativeFilePath = Path.GetRelativePath(_repoBaseDir, fileInfo.FullName).Replace(Path.DirectorySeparatorChar, '/');

                // Nun alle Einträge durchsuchen und den letzten zurückgeben.
                Entry? latestEntry = null;
                foreach (var entry in _entries)
                {
                    if (entry.IsMatch(relativeFilePath))
                    {
                        latestEntry = entry;
                    }
                }

                // Kein zuständiger Eintrag gefunden.
                return latestEntry;
            }
        }
        /// <summary>
        /// Diese Methode fügt eine neue Dateierweiterung zur Datei hinzu.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool AppendPatternLFS_ExtensionOnly(FileInfo fileInfo)
        {
            lock (_syncObject)
            {
                var pattern = "*" + fileInfo.Extension;
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    // Leere Endungen kann man nicht hinzufügen.
                    return false;
                }

                var entry = new Entry(States.LFS, line: $"{pattern} filter=lfs diff=lfs merge=lfs -text", pattern: pattern);
                _entries.Add(entry);
                _modified = true;
                s_log.Trace("LFS-Dictionary: Erweiterung hinzugefügt (Erweiterung): {0}", pattern);
                return true;
            }
        }
        public bool AppendPatternLFS_Filename(FileInfo fileInfo)
        {
            lock (_syncObject)
            {
                // Wenn das _repoBaseDir ungültig ist, dann können wir nicht wirklich prüfen. Wir betrachten deshalb alles als No-Match.
                if (string.IsNullOrWhiteSpace(_repoBaseDir))
                {
                    // Da "_repoBaseDir" nicht gültig ist, können wir auch nichts hinzufügen.
                    return false;
                }

                // Prüfen ob die angegebene Datei im Repository liegt. Wenn nicht, dann ist die angefragte Datei ungültig.
                if (!fileInfo.FullName.StartsWith(_repoBaseDir, StringComparison.OrdinalIgnoreCase))
                {
                    // Da "fileInfo" nicht gültig ist, können wir auch nichts hinzufügen.
                    return false;
                }

                var pattern = fileInfo.Name;
                var entry = new Entry(States.LFS, line: $"{pattern} filter=lfs diff=lfs merge=lfs -text", pattern: pattern);
                _entries.Add(entry);
                _modified = true;
                s_log.Trace("LFS-Dictionary: Erweiterung hinzugefügt (Dateiname): {0}", pattern);
                return true;
            }
        }
        public bool AppendPatternLFS_FilenameAndPath(FileInfo fileInfo)
        {
            lock (_syncObject)
            {
                // Wenn das _repoBaseDir ungültig ist, dann können wir nicht wirklich prüfen. Wir betrachten deshalb alles als No-Match.
                if (string.IsNullOrWhiteSpace(_repoBaseDir))
                {
                    // Da "_repoBaseDir" nicht gültig ist, können wir auch nichts hinzufügen.
                    return false;
                }

                // Prüfen ob die angegebene Datei im Repository liegt. Wenn nicht, dann ist die angefragte Datei ungültig.
                if (!fileInfo.FullName.StartsWith(_repoBaseDir, StringComparison.OrdinalIgnoreCase))
                {
                    // Da "fileInfo" nicht gültig ist, können wir auch nichts hinzufügen.
                    return false;
                }

                // Pfad relativ zum Repo.
                var pattern = Path.GetRelativePath(_repoBaseDir, fileInfo.FullName).Replace(Path.DirectorySeparatorChar, '/');
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    s_log.Warn("Pattern 'null' kann nicht hinzugefügt werden.");
                    return false;
                }

                var entry = new Entry(States.LFS, line: $"{pattern} filter=lfs diff=lfs merge=lfs -text", pattern: pattern);
                _entries.Add(entry);
                _modified = true;
                s_log.Trace("LFS-Dictionary: Erweiterung hinzugefügt (Fullpath): {0}", pattern);
                return true;
            }
        }
        /// <summary>
        /// Diese Methode entfernt die angegebene Dateierweiterung.
        /// </summary>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool RemovePatternLFS(string pattern)
        {
            lock (_syncObject)
            {
                if (string.IsNullOrWhiteSpace(pattern))
                {
                    s_log.Warn("Pattern 'null' kann nicht entfernt werden.");
                    return false;
                }

                for (var i = 0; i < _entries.Count; i++)
                {
                    var entry = _entries[i];
                    if (entry.Pattern.Equals(pattern))
                    {
                        _entries.RemoveAt(i);
                        _modified = true;
                        s_log.Trace("LFS-Dictionary: Erweiterung entfernt: {0}", pattern);
                        return true;
                    }
                }

                s_log.Trace("LFS-Dictionary: Erweiterung nicht gefunden: {0}", pattern);
                return false;
            }
        }
    }
}