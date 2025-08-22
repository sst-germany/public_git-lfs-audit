using SST.GitLfsAudit.Models;
using System.Diagnostics;

namespace SST.GitLfsAudit
{
    internal static class Processor
    {
        #region nLog instance (s_log)
        private static NLog.Logger s_log { get; } = NLog.LogManager.GetCurrentClassLogger();
        #endregion

        public static bool Execute(string? baseDirectory)
        {
            // Check directory
            if (string.IsNullOrEmpty(baseDirectory))
            {
                baseDirectory = ".\\";
            }
            baseDirectory = Path.GetFullPath(baseDirectory);
            if (!Directory.Exists(baseDirectory))
            {
                s_log.Error($"Directory: '{baseDirectory}' does not exist.");
                return false;
            }

            var sw = Stopwatch.StartNew();
            try
            {
                static Version? getEntryAssemblyVersion()
                {
                    var text = Path.Combine(System.AppContext.BaseDirectory, "git-lfs-audit.dll");
                    if (text == null)
                    {
                        return null;
                    }

                    return getAssemblyVersion(new FileInfo(text));
                    static Version? getAssemblyVersion(FileInfo assembly)
                    {
                        if (!File.Exists(assembly.FullName))
                        {
                            return null;
                        }

                        try
                        {
                            var versionInfo = FileVersionInfo.GetVersionInfo(assembly.FullName);
                            return new Version(versionInfo.FileMajorPart, versionInfo.FileMinorPart, versionInfo.FileBuildPart, versionInfo.ProductPrivatePart);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
                s_log.Info("LFS-AUDIT (Version {0}) beginnt mit der Analyse von Verzeichnis '{1}'.", getEntryAssemblyVersion(), baseDirectory);

                // Dateilisten zusammenstellen
                s_log.Info(string.Empty);
                s_log.Info("-=[SCAN]=-");
                s_log.Info("Stelle Dateilisten zusammen, das kann einige Zeit dauern...");
                var contents = Content.ScanRepositoriesForContent(baseDirectory);
                if (contents.Count == 0)
                {
                    s_log.Warn("No git repositories found: {0}", baseDirectory);
                    return true;
                }
                s_log.Info(" (Dauer: {0:0.00} Sekunden)", sw.Elapsed.TotalSeconds);

                var results = new List<FileState?>[contents.Count];
                // Dateien analysieren
                s_log.Info(string.Empty);
                s_log.Info("-=[ANALYSE]=-");
                s_log.Info("Beginne mit der Anlayse, das kann einige Zeit dauern...");
                foreach (var content in contents)
                {
                    content.Analyse();
                }
                s_log.Info(" (Dauer: {0:0.00} Sekunden)", sw.Elapsed.TotalSeconds);

                s_log.Info(string.Empty);
                s_log.Info("-=[Auswertung]=-");
                var foundAnyProblems = false;
                foreach (var content in contents)
                {
                    var errorCount = content.Presentation();
                    if (errorCount > 0)
                    {
                        foundAnyProblems = true;
                        content.TrySolveProblems();
                    }
                }

                s_log.Info(string.Empty);
                s_log.Info("-=[Zusammenfassung]=-");

                if (foundAnyProblems)
                {
                    s_log.Warn("Verarbeitung beendet. Es wurde Probleme entdeckt. Bitte prüfen sie das Repository erneut.");
                    return false;
                }

                s_log.Info("Verarbeitung beendet. Keine Probleme entdeckt.");
                return true;
            }
            catch (Exception ex)
            {
                s_log.Error(ex, "Unerwartete Exception. Programm mit Fehlern beendet.");
                return false;
            }
        }
    }
}
