using System.Text;

namespace SST.GitLfsAudit
{
    internal static class ConsoleTools
    {
        #region nLog instance (s_log)
        private static NLog.Logger s_log { get; } = NLog.LogManager.GetCurrentClassLogger();
        #endregion

        public static bool AskYesNo(string question, NLog.LogLevel level, bool defaultYes = true)
        {
            // Anzeige der Frage mit [Y/n] oder [y/N] je nach Default
            var defaultOption = defaultYes ? "J/n" : "j/N";
            s_log.Log(level, $"{question} [{defaultOption}]: ");

            while (true)
            {
                var input = Console.ReadLine()?.Trim().ToLowerInvariant();
                switch (input)
                {
                    case "":
                        return defaultYes;
                    case "j":
                    case "ja":
                        return true;
                    case "n":
                    case "nein":
                        return false;
                    default:
                        Console.Write("Bitte 'j' oder 'n' eingeben: ");
                        break;
                }
            }
        }
        public static char AskDynamic(string question, NLog.LogLevel level, char[] options)
        {
            var sb = new StringBuilder();
            foreach (var c in options)
            {
                sb.Append(c);
                sb.Append(',');
            }
            sb.Remove(sb.Length - 1, 1);

            s_log.Log(level, $"{question} [{sb}]: ");
            while (true)
            {
                var input = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (input is not null && input.Length >= 1)
                {
                    foreach (var c in options)
                    {
                        if (c == input[0])
                        {
                            return c;
                        }
                    }
                }

                Console.Write($"Bitte [{sb}] eingeben: ");
            }
        }
    }
}
