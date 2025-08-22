namespace SST.GitLfsAudit.Services.Options
{
    /// <summary>
    /// Stellt die verfügbaren Optionen für die Kommandozeile dar.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Pfad zum Verzeichnis das geprüft werden soll.
        /// </summary>
        [CommandLine.Option('d', "directory", Required = false, HelpText = "Pfad zum Verzeichnis das geprüft werden soll.")]
        public string? Directory { get; set; } = ".\\";

        /// <summary>
        /// Optimiert die Prüfung und verwendet eine vordefinierte Liste von Dateiendungen.
        /// </summary>
        [CommandLine.Option('o', "optimze", Required = false, HelpText = "Optimiert die Prüfung und verwendet eine vordefinierte Liste von Dateiendungen.")]
        public bool Optimize { get; set; } = true;

        /// <summary>
        /// Erhöht den Detailgrad der Ausgaben in der Console.
        /// </summary>
        [CommandLine.Option('v', "verbose", Required = false, HelpText = "Erhöht den Detailgrad der Ausgaben in der Console.")]
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Die Größe (Bytes) ab welcher eine Datei als problematisch erkannt werden soll.
        /// </summary>
        [CommandLine.Option(longName: "oversize", Required = false, HelpText = "Die Größe (Bytes) ab welcher eine Datei als problematisch erkannt werden soll.")]
        public int OversizeBytes { get; set; } = 1024 * 1024 * 5; // 3MB

        /// <summary>
        /// Die Anzahl der zu prüfenden Bytes, bei der TEXT Erkennung.
        /// </summary>
        [CommandLine.Option(longName: "checkBytes", Required = false, HelpText = "Die Anzahl der zu prüfenden Bytes, bei der TEXT Erkennung.")]
        public int AnalyseBytes { get; set; } = 1024 * 8;

        [CommandLine.Option(longName: "threadCount", Required = false, HelpText = "Legt die Anzahl der gleichzeitigen Arbeitsthreads fest.")]
        public int ThreadCount { get; set; } = 4;

        /// <summary>
        /// Führt nur einen trocken durchlauf durch.
        /// </summary>
        [CommandLine.Option(longName: "simulation", Required = false, HelpText = "Führt die Prüfung durch, ändert aber keine Daten.")]
        public bool DryRun { get; set; } = false;

        /// <summary>
        /// Gibt den Mechanismus an, mit dem Textdateien und Binärdateien unterschieden werden.
        /// </summary>
        [CommandLine.Option(longName: "algorithm", Required = false, HelpText = "[Simple, BomBased (Default)] Gibt den Mechanismus an, mit dem Textdateien und Binärdateien unterschieden werden.")]
        public Algorithms.ServiceTypes Algorithm { get; set; } = Algorithms.ServiceTypes.DefaultSetting;

        /// <summary>
        /// Gibt an, welche vordefinierten Dateierweiterungen für die unterscheidund von Text- und Binärdateien verwendet werden sollen.
        /// </summary>
        [CommandLine.Option(longName: "extensionpreset", Required = false, HelpText = "[Simple (Default), UnityProject] Gibt an, welche vordefinierten Dateierweiterungen für die unterscheidund von Text- und Binärdateien verwendet werden sollen.")]
        public ExtensionPresets.ServiceTypes ExtensionPreset { get; set; } = ExtensionPresets.ServiceTypes.DefaultSetting;
    }
}
