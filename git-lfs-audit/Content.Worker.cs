using SST.GitLfsAudit.Models;
using SST.GitLfsAudit.Services;
using System.Collections.Concurrent;

namespace SST.GitLfsAudit
{
    public partial class Content
    {
        private class Worker
        {
            private readonly Task _task;
            private readonly int _oversizeBytes;
            private readonly LfsDictionary _lfsDictionary;
            private readonly Services.Algorithms.IService _algorithm;
            private readonly BlockingCollection<FileInfo> _queue;
            private readonly List<FileState?> _results = [];

            public Worker(BlockingCollection<FileInfo> queue, LfsDictionary lfsDictionary, int oversizeBytes)
            {
                _queue = queue;
                _lfsDictionary = lfsDictionary;
                _oversizeBytes = oversizeBytes;
                _algorithm = Ioc.GetService<Services.Algorithms.IService>();

                _task = Task.Run(() =>
                {
                    foreach (var item in _queue.GetConsumingEnumerable())
                    {
                        var fileState = ProcessFileState(item);
                        lock (_results)
                        {
                            _results.Add(fileState);
                        }
                    }
                });
            }


            private FileState? ProcessFileState(FileInfo fileInfo)
            {
                if (string.IsNullOrWhiteSpace(fileInfo.FullName))
                {
                    return null;
                }

                if (!File.Exists(fileInfo.FullName))
                {
                    return null;
                }

                // Gibt es für die Datei einen Eintrag?
                var responsibleEntry = _lfsDictionary.GetLatestMatch(fileInfo);

                // Was sagt die Datei-Byte-Analyse?
                if (_algorithm.IsTextFile(fileInfo))
                {
                    // Ist Text...
                    if (responsibleEntry is null /* entspricht Text */ || responsibleEntry.TreatAsText)
                    {
                        // ... und laut .gitattributes eine LFS
                        if (isOverSize(fileInfo))
                        {
                            return new FileState(fileInfo, ok: false, fileType: FileTypes.IsTextOversize, isTracked: false);
                        }
                        else
                        {
                            return new FileState(fileInfo, ok: true, fileType: FileTypes.IsText, isTracked: false);
                        }
                    }
                    else if (responsibleEntry.TreatAsLFS)
                    {
                        if (isOverSize(fileInfo))
                        {
                            // ... aber laut .gitattributes eine LFS
                            return new FileState(fileInfo, ok: true, fileType: FileTypes.IsTextOversize, isTracked: true);
                        }
                        else
                        {
                            // ... aber laut .gitattributes eine LFS
                            return new FileState(fileInfo, ok: false, fileType: FileTypes.IsText, isTracked: true);
                        }
                    }
                    else
                    {
                        // ... und laut .gitattributes unwichtig (weder Text noch LFS)
                        return new FileState(fileInfo, ok: true, isTracked: false, FileTypes.IsIgnored);
                    }
                }
                else
                {
                    // Ist Binär...
                    if (responsibleEntry is null /* entspricht Text */ || responsibleEntry.TreatAsText)
                    {
                        // ... aber laut .gitattributes eine LFS
                        return new FileState(fileInfo, ok: false, fileType: FileTypes.IsBinary, isTracked: false);
                    }
                    else if (responsibleEntry.TreatAsLFS)
                    {
                        // ... aber laut .gitattributes eine LFS
                        return new FileState(fileInfo, ok: true, fileType: FileTypes.IsBinary, isTracked: true);
                    }
                    else
                    {
                        // ... und laut .gitattributes unwichtig
                        return new FileState(fileInfo, ok: true, isTracked: false, FileTypes.IsIgnored);
                    }
                }

                bool isOverSize(FileInfo fileInfo)
                {
                    return fileInfo.Length >= _oversizeBytes;
                }
            }
            public IReadOnlyList<FileState?> WaitCompleted()
            {
                _task.Wait();
                return _results;
            }
        }
    }
}
