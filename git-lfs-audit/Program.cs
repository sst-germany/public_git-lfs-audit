
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Config;
using NLog.Targets;
using SST.GitLfsAudit.Models;
using SST.GitLfsAudit.Services;
using SST.GitLfsAudit.Services.Options;

namespace SST.GitLfsAudit
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Parse CommandLine
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed((o) =>
                {
                    // Service Collection erstellen
                    var services = new ServiceCollection();
                    Ioc.ConfigureServices((services) =>
                    {
                        // Options
                        services.AddSingleton<Services.Options.IService>(implementationInstance => new Services.Options.Service(o));
                        // Algorithm
                        switch (o.Algorithm)
                        {
                            case Services.Algorithms.ServiceTypes.Simple:
                                services.AddTransient<Services.Algorithms.IService, Services.Algorithms.Simple>();
                                break;
                            case Services.Algorithms.ServiceTypes.BomBased:
                                services.AddTransient<Services.Algorithms.IService, Services.Algorithms.BomBased>();
                                break;
                        }
                        // ExtensionPreset
                        switch (o.ExtensionPreset)
                        {
                            case Services.ExtensionPresets.ServiceTypes.Simple:
                                services.AddSingleton<Services.ExtensionPresets.IService, Services.ExtensionPresets.Simple>();
                                break;
                            case Services.ExtensionPresets.ServiceTypes.UnityProject:
                                services.AddSingleton<Services.ExtensionPresets.IService, Services.ExtensionPresets.UnityProject>();
                                break;
                        }
                    });

                    try
                    {
                        #region static void configureColoredConsoleLogging(bool verbose)
                        static void configureColoredConsoleLogging(bool verbose)
                        {
                            // Neue NLog-Konfiguration anlegen
                            var config = new LoggingConfiguration();

                            // ColoredConsole-Target erstellen
                            var consoleTarget = new ColoredConsoleTarget("console")
                            {
                                //Layout = @"${longdate} | ${level:uppercase=true} | ${message} ${exception:format=toString}"
                                Layout = @"${message} ${exception:format=toString}"
                            };

                            // Farben je nach Log-Level festlegen
                            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
                            {
                                Condition = "level == LogLevel.Debug",
                                ForegroundColor = ConsoleOutputColor.Gray
                            });
                            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
                            {
                                Condition = "level == LogLevel.Info",
                                ForegroundColor = ConsoleOutputColor.White
                            });
                            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
                            {
                                Condition = "level == LogLevel.Warn",
                                ForegroundColor = ConsoleOutputColor.Yellow
                            });
                            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
                            {
                                Condition = "level == LogLevel.Error",
                                ForegroundColor = ConsoleOutputColor.Red
                            });
                            consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
                            {
                                Condition = "level == LogLevel.Fatal",
                                ForegroundColor = ConsoleOutputColor.White,
                                BackgroundColor = ConsoleOutputColor.Red
                            });

                            // Target mit Log-Level-Regel verbinden
                            config.AddRule(verbose ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal, consoleTarget);

                            // Konfiguration aktivieren
                            LogManager.Configuration = config;
                        }
                        #endregion
                        configureColoredConsoleLogging(o.Verbose);

                        if (!Processor.Execute(o.Directory))
                        {
                            Environment.ExitCode = (int)ErrorCodes.FoundProblems;
                        }
                        else
                        {
                            Environment.ExitCode = (int)ErrorCodes.Ok;
                        }
                    }
                    finally
                    {
                        // Beenden des ServiceProviders
                        Ioc.Terminate();
                    }
                })
                .WithNotParsed(onNotParsed);

            #region static void onNotParsed(IEnumerable<CommandLine.Error> errs)
            static void onNotParsed(IEnumerable<CommandLine.Error> errs)
            {
                Environment.ExitCode = (int)ErrorCodes.CommandLine;
            }
            #endregion
        }
    }
}
