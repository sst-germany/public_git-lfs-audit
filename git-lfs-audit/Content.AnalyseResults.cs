using SST.GitLfsAudit.Models;

namespace SST.GitLfsAudit
{
    public partial class Content
    {
        private class AnalyseResults
        {
            public int ErrorCounter { get; }
            public IReadOnlyList<FileState?> FileStates { get; }

            public AnalyseResults()
            {
                FileStates = [];
                ErrorCounter = 0;
            }
            public AnalyseResults(IReadOnlyList<FileState?> results, int errorCounter)
            {
                FileStates = results;
                ErrorCounter = errorCounter;
            }
        }
    }
}
