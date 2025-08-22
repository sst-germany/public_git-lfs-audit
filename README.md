# git-lfs-audit

**git-lfs-audit** ist ein kleines .NET Tool, das Git-Repositories auf **fehlende oder inkonsistente Git LFS-Einträge** überprüft und den Nutzer darauf hinweist. 
So stellst du sicher, dass große Dateien korrekt über LFS verwaltet werden.

---

## Installation

### Global über .NET Tool
```bash
dotnet tool install --global git-lfs-audit
````

### Update

```bash
dotnet tool update --global git-lfs-audit
```

---

## Nutzung

Wechsle in das Repository, das du prüfen möchtest, und führe aus:

```bash
git-lfs-audit
```

Optional kannst du zusätzliche Argumente verwenden:

```bash
git-lfs-audit --help
```

Dies zeigt alle verfügbaren Optionen an, z.B. bestimmte Pfade auszuschließen oder die Ausgabe zu formatieren.

```
  -d, --directory      Pfad zum Verzeichnis das geprüft werden soll.

  -o, --optimze        Optimiert die Prüfung und verwendet eine vordefinierte Liste von Dateiendungen.

  -v, --verbose        Erhöht den Detailgrad der Ausgaben in der Console.

  --oversize           Die Größe (Bytes) ab welcher eine Datei als problematisch erkannt werden soll.

  --checkBytes         Die Anzahl der zu prüfenden Bytes, bei der TEXT Erkennung.

  --threadCount        Legt die Anzahl der gleichzeitigen Arbeitsthreads fest.

  --simulation         Führt die Prüfung durch, ändert aber keine Daten.

  --algorithm          [Simple, BomBased (Default)] Gibt den Mechanismus an, mit dem Textdateien und Binärdateien
                       unterschieden werden.

  --extensionpreset    [Simple (Default), UnityProject] Gibt an, welche vordefinierten Dateierweiterungen für die
                       unterscheidund von Text- und Binärdateien verwendet werden sollen.

  --help               Display this help screen.

  --version            Display version information.
```

---

## Lizenz

Dieses Projekt ist unter der [MIT License](LICENSE.md) lizenziert.

---

## Abhängigkeiten

Dieses Projekt nutzt folgende NuGet-Pakete:

* [`CommandLineParser`](https://www.nuget.org/packages/CommandLineParser/) – zur einfachen Definition von CLI-Optionen und Argumenten.
* [`Microsoft.Extensions.DependencyInjection`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/) – für Dependency Injection und Service-Registrierung.
* [`NLog`](https://www.nuget.org/packages/NLog/) – für Logging und Protokollierung von Meldungen.

---

## Repository

[https://github.com/sst-germany/public_git-lfs-audit](https://github.com/sst-germany/public_git-lfs-audit)