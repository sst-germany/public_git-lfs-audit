using NLog;
using SST.GitLfsAudit.Models;
using SST.GitLfsAudit.Services;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace SST.GitLfsAudit
{
    public partial class Content
    {
        #region nLog instance (s_log)
        private static Logger s_log { get; } = LogManager.GetCurrentClassLogger();
        #endregion

        public string RepoDirectory { get; }
        public LfsDictionary LfsDictionary { get; }
        public int DetectedFileCount => _fileInfos.Count;

        private AnalyseResults? _analyseResults = null;
        private readonly List<FileInfo> _fileInfos = [];
        private readonly Services.Options.Options _options;


        private Content(string repoDirectory)
        {
            Debug.Assert(Directory.Exists(repoDirectory));
            Debug.Assert(Directory.Exists(Path.Combine(repoDirectory, ".git")));

            _options = Ioc.GetService<Services.Options.IService>().Options;


            RepoDirectory = repoDirectory;
            LfsDictionary = new LfsDictionary(repoDirectory);
            s_log.Info("- Repository erkannt: {0}", repoDirectory);
        }
        /// <summary>
        /// Diese Methode erstellt eine Dateiliste des übergebenen Repository Verzeichnisses.
        /// Sollte in dem Repo ein Sub-Repo liegen, wird für dieses Repo ein eigenes <see cref="Content"/> Objekt erzeugt.
        /// Die gefundenen Objekte werden im 'return' als Liste zurückgegeben.
        /// </summary>
        /// <param name="repoDirectory">Das Basisverzeichnis, welches nach den Dateien durchsucht werden soll. Dieses Verzeichnis muss ein Git-Repo sein.</param>
        /// <param name="contentContainer">(Optional) Dieser Parameter wird für rekursive Aufrufe verwendet um die erstellten <see cref="Content"/> Objekte zu sammeln.</param>
        /// <returns>Eine Liste mit <see cref="Content"/> Instanzen. Die Liste kann auch leer sein.</returns>
        public static List<Content> ScanRepositoriesForContent(string repoDirectory, List<Content>? contentContainer = null)
        {
            if (contentContainer is null)
            {
                contentContainer = new List<Content>();
            }

            if (string.IsNullOrWhiteSpace(repoDirectory))
            {
                s_log.Error("Directory null or empty");
                return contentContainer;
            }

            if (!Directory.Exists(repoDirectory))
            {
                s_log.Error("Directory not found: {0}", repoDirectory);
                return contentContainer;
            }

            if (!Directory.Exists(Path.Combine(repoDirectory, ".git")))
            {
                s_log.Error("RepoDirectory must be a Git-Repo: {0}", repoDirectory);
                return contentContainer;
            }

            // Neue Content Instanz erstellen
            var content = new Content(repoDirectory);

            // Und die Instanz dem Sammelcontainer hinzufügen
            contentContainer.Add(content);

            // Alle Dateien hinzufügen
            addFiles(content, repoDirectory);

            // Rekursiv durch weitere Unterverzeichnisse gehen.
            foreach (var subDir in Directory.GetDirectories(repoDirectory))
            {
                if (!subDir.ToLower().EndsWith("\\.git"))
                {
                    // Alle Ordner nach Dateien untersuchen. Sub-Repos werden erneut über Execute(..) getriggert.
                    void enumerateSubDirectoryRecursive(string? directory)
                    {
                        if (string.IsNullOrWhiteSpace(directory))
                        {
                            s_log.Error("Directory not specified: {0}", directory);
                            return;
                        }

                        if (!Directory.Exists(directory))
                        {
                            s_log.Error("Directory not found: {0}", directory);
                            return;
                        }

                        // Prüfen ob dieses Verzeichnis eventuel ein neues Git-Repo ist (Sub-Repo).
                        if (Directory.Exists(Path.Combine(directory, ".git")))
                        {
                            // This is a new Repo!

                            _ = ScanRepositoriesForContent(directory, contentContainer);
                            return;
                        }

                        // Das Verzeichnis ist gültig, wir fügen alle Dateien hinzu.
                        addFiles(content, directory);

                        // Rekursiv durch weitere Unterverzeichnisse gehen.
                        foreach (var subDir in Directory.GetDirectories(directory))
                        {
                            enumerateSubDirectoryRecursive(directory: subDir);
                        }
                    }
                    enumerateSubDirectoryRecursive(subDir);
                }
            }

            return contentContainer;

            static void addFiles(Content content, string directory)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(directory))
                    {
                        var fileInfo = new FileInfo(file);
                        if (!fileInfo.Name.Equals(".gitattributes", StringComparison.InvariantCultureIgnoreCase) &&
                            !fileInfo.Name.Equals(".gitignore", StringComparison.InvariantCultureIgnoreCase) &&
                            !fileInfo.Name.Equals(".gitkeep", StringComparison.InvariantCultureIgnoreCase))
                        {
                            content._fileInfos.Add(fileInfo);
                        }
                    }
                }
                catch (Exception ex)
                {
                    s_log.Error(ex, "Exception while enumerating files of directory: {0}", directory);
                }
            }
        }


        public bool Analyse()
        {
            _analyseResults = null;

            var threadCount = Math.Clamp(_options.ThreadCount, 1, 10);

            s_log.Info("- Repository Analyse: {0} (Thread-Count={1})", RepoDirectory, threadCount);

            if (_fileInfos.Count == 0)
            {
                _analyseResults = new AnalyseResults();
                return true;
            }

            // Ein Kopie der FileInfos in einer speziellen Collection zusammenstellen.
            var queue = new BlockingCollection<FileInfo>(_fileInfos.Count);
            foreach (var file in _fileInfos)
            {
                queue.Add(file);
            }
            queue.CompleteAdding();

            // Die gewünschten Worker erstellen und die Collection übergeben.
            var workers = new Worker[threadCount];
            for (var i = 0; i < threadCount; i++)
            {
                workers[i] = new Worker(queue, LfsDictionary, _options.OversizeBytes);
            }

            // Warten bis alle Worker fertig sind.
            var results = new List<FileState?>(DetectedFileCount);
            for (var i = 0; i < threadCount; i++)
            {
                results.AddRange(workers[i].WaitCompleted());
            }

            _analyseResults = new AnalyseResults(results, results.Count(x => x is null || !x.Ok));
            return _analyseResults.ErrorCounter == 0;
        }
        public int Presentation()
        {
            _analyseResults ??= new AnalyseResults();

            if (_analyseResults.ErrorCounter > 0)
            {
                // Display
                s_log.Info("----------------------------------------------------------------------------------------------------");
                s_log.Info($"Repo: {RepoDirectory}");
                s_log.Info("----------------------------------------------------------------------------------------------------");
                s_log.Info(" OK | TRK | TYPE  | FILENAME (size)");
                s_log.Info("----------------------------------------------------------------------------------------------------");
                foreach (var fileState in _analyseResults.FileStates)
                {
                    if (fileState is null)
                    {
                        s_log.Error(" ERR | Unable to analyse file (unknown name).");
                    }
                    else
                    {
                        if (fileState.Ok)
                        {
                            if (_options.Verbose)
                            {
                                // OK, Tracked, Type
                                s_log.Warn(" OK | {0} | {1} | {2}",
                                    getTrackedText(fileState.IsTracked),
                                    getFileTypeText(fileState.FileType),
                                    fileState.FileInfo.FullName);
                            }
                        }
                        else
                        {
                            // OK, Tracked, Type
                            s_log.Warn("ERR | {0} | {1} | {2} ({3:0.00}MB)",
                                getTrackedText(fileState.IsTracked),
                                getFileTypeText(fileState.FileType),
                                fileState.FileInfo.FullName,
                                fileState.FileInfo.Length / 1024.0 / 1024.0);
                        }
                    }
                }
                s_log.Info("----------------------------------------------------------------------------------------------------");
                s_log.Info(" OK | TRK | TYPE  | FILENAME (size)");
                s_log.Info("----------------------------------------------------------------------------------------------------");
                s_log.Info("");
                s_log.Warn("Es wurden {0} Probleme erkannt.", _analyseResults.ErrorCounter);
            }
            else
            {
                s_log.Info("- Repository '{0}': Keine Probleme", RepoDirectory);
            }

            return _analyseResults.ErrorCounter;

            // Returns: "TKR" wenn "isTracked" true ist, sonst "---"
            #region string getTrackedText(bool isTracked)
            string getTrackedText(bool isTracked)
            {
                if (isTracked)
                {
                    return "TRK";
                }
                else
                {
                    return "---";
                }
            }
            #endregion
            // Returns: "TEXT", "OVSZ", "BIN "; Je nach Dateityp.
            #region string getFileTypeText(FileTypes fileType)
            string getFileTypeText(FileTypes fileType)
            {
                return fileType switch
                {
                    FileTypes.IsText => "TEXT ",
                    FileTypes.IsTextOversize => "OVSZ",
                    _ => "BIN "
                };
            }
            #endregion
        }
        public void TrySolveProblems()
        {
            _analyseResults ??= new AnalyseResults();
            if (_analyseResults.ErrorCounter == 0)
            {
                // Sang und klanglos verlassen.
                return;
            }

            if (_options.DryRun)
            {
                s_log.Info("Dry Run: Es werden keine Änderungen vorgenommen.");
                return;
            }

            // Versuch der Fehlerbehebung
            if (ConsoleTools.AskYesNo("Soll das Programm versuchen die Fehler zu beheben?", LogLevel.Warn, false))
            {
                var modified = 0;
                foreach (var fileState in _analyseResults.FileStates)
                {
                    if (fileState is null)
                    {
                        // Hier können wir leider nichts machen.
                        continue;
                    }

                    // Wir gehen hier alle Results durch und die !OK sind zu fixen.
                    if (!fileState.Ok)
                    {
                        // Jeder Fehler wird erneut abgefragt, da die Korrektur eines vorhergehenden Fehlers diesen behoben haben könnte.
                        var responsibleEntry = LfsDictionary.GetLatestMatch(fileState.FileInfo);
                        if (fileState.IsTracked)
                        {
                            // Entfernen
                            if (responsibleEntry is not null)
                            {
                                var question = "Soll der Eintrag entfernt werden? (j) Ja, (n) Nein oder (a) Abbrechen?";
                                char[] options = ['j', 'n', 'a'];

                                s_log.Info("Eintrag: {0}", fileState.FileInfo.FullName);
                                var result = ConsoleTools.AskDynamic(question, LogLevel.Warn, options);

                                var abort = false;
                                switch (result)
                                {
                                    case 'j':
                                        if (LfsDictionary.RemovePatternLFS(responsibleEntry.Pattern))
                                        {
                                            s_log.Info("Entfernt: {0}", responsibleEntry.Pattern);
                                            modified++;
                                        }
                                        break;
                                    case 'n':
                                        break;
                                    case 'a':
                                        abort = true;
                                        break;
                                    default:
                                        break;
                                }
                                if (abort)
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // Hinzufügen
                            if (responsibleEntry is null)
                            {
                                string question;
                                char[] options;

                                if (fileState.FileInfo.Extension == string.Empty)
                                {
                                    question = "Wie soll der Eintrag erstellt werden: (d) Dateiname, (p) Pfad, (i) Ignorieren oder (a) Abbrechen?";
                                    options = ['d', 'p', 'i', 'a'];
                                }
                                else
                                {
                                    question = "Wie soll der Eintrag erstellt werden: (e) Dateierweiterung, (d) Dateiname, (p) Pfad, (i) Ignorieren oder (a) Abbrechen?";
                                    options = ['e', 'd', 'p', 'i', 'a'];
                                }

                                s_log.Info("Eintrag: {0}", fileState.FileInfo.FullName);
                                var result = ConsoleTools.AskDynamic(question, LogLevel.Warn, options);

                                var abort = false;
                                switch (result)
                                {
                                    case 'e':
                                        {
                                            if (!LfsDictionary.AppendPatternLFS_ExtensionOnly(fileState.FileInfo))
                                            {
                                                s_log.Error("Fehler beim hinzfügen.");
                                            }
                                            else
                                            {
                                                modified++;
                                            }
                                        }
                                        break;
                                    case 'd':
                                        {
                                            if (!LfsDictionary.AppendPatternLFS_Filename(fileState.FileInfo))
                                            {
                                                s_log.Error("Fehler beim hinzfügen.");
                                            }
                                            else
                                            {
                                                modified++;
                                            }
                                        }
                                        break;
                                    case 'p':
                                        {
                                            if (!LfsDictionary.AppendPatternLFS_FilenameAndPath(fileState.FileInfo))
                                            {
                                                s_log.Error("Fehler beim hinzfügen.");
                                            }
                                            else
                                            {
                                                modified++;
                                            }
                                        }
                                        break;
                                    case 'a':
                                        {
                                            abort = true;
                                        }
                                        break;
                                    default:
                                        break;
                                }

                                if (abort)
                                {
                                    break;
                                }
                            }
                        }
                    } // if (!fileState.Ok)
                }

                // Zusammenfassung
                if (modified > 0)
                {
                    if (LfsDictionary.Save())
                    {
                        s_log.Info("Die Datei '.gitattributes' wurde erfolgreich gespeichert. {0} Einträge wurden angepasst. Bitte prüfe das Repository erneut.", modified);
                    }
                    else
                    {
                        s_log.Error("Fehler beim Speichern der Datei: .gitattributes");
                    }
                }
            }
        }
    }
}
