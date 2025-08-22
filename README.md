# git-lfs-audit

**git-lfs-audit** ist ein kleines .NET Tool, das Git-Repositories auf **fehlende oder inkonsistente Git LFS-Eintr�ge** �berpr�ft und den Nutzer darauf hinweist. 
So stellst du sicher, dass gro�e Dateien korrekt �ber LFS verwaltet werden.

---

## Installation

### Global �ber .NET Tool
```bash
dotnet tool install --global git-lfs-audit
````

### Update

```bash
dotnet tool update --global git-lfs-audit
```

---

## Nutzung

Wechsle in das Repository, das du pr�fen m�chtest, und f�hre aus:

```bash
git-lfs-audit
```

Optional kannst du zus�tzliche Argumente verwenden:

```bash
git-lfs-audit --help
```

Dies zeigt alle verf�gbaren Optionen an, z.B. bestimmte Pfade auszuschlie�en oder die Ausgabe zu formatieren.

```
  -d, --directory      Pfad zum Verzeichnis das gepr�ft werden soll.

  -o, --optimze        Optimiert die Pr�fung und verwendet eine vordefinierte Liste von Dateiendungen.

  -v, --verbose        Erh�ht den Detailgrad der Ausgaben in der Console.

  --oversize           Die Gr��e (Bytes) ab welcher eine Datei als problematisch erkannt werden soll.

  --checkBytes         Die Anzahl der zu pr�fenden Bytes, bei der TEXT Erkennung.

  --threadCount        Legt die Anzahl der gleichzeitigen Arbeitsthreads fest.

  --simulation         F�hrt die Pr�fung durch, �ndert aber keine Daten.

  --algorithm          [Simple, BomBased (Default)] Gibt den Mechanismus an, mit dem Textdateien und Bin�rdateien
                       unterschieden werden.

  --extensionpreset    [Simple (Default), UnityProject] Gibt an, welche vordefinierten Dateierweiterungen f�r die
                       unterscheidund von Text- und Bin�rdateien verwendet werden sollen.

  --help               Display this help screen.

  --version            Display version information.
```

---

## Lizenz

Dieses Projekt ist unter der [MIT License](LICENSE.md) lizenziert.

---

## Abh�ngigkeiten

Dieses Projekt nutzt folgende NuGet-Pakete:

* [`CommandLineParser`](https://www.nuget.org/packages/CommandLineParser/) � zur einfachen Definition von CLI-Optionen und Argumenten.
* [`Microsoft.Extensions.DependencyInjection`](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/) � f�r Dependency Injection und Service-Registrierung.
* [`NLog`](https://www.nuget.org/packages/NLog/) � f�r Logging und Protokollierung von Meldungen.

---

## Repository

[https://github.com/sst-germany/public_git-lfs-audit](https://github.com/sst-germany/public_git-lfs-audit)