namespace SST.GitLfsAudit.Models
{
    public class FileState
    {
        public bool Ok;
        public bool IsTracked;
        public FileTypes FileType;
        public FileInfo FileInfo;


        public FileState(FileInfo fileInfo, bool ok, bool isTracked, FileTypes fileType)
        {
            FileInfo = fileInfo;
            Ok = ok;
            IsTracked = isTracked;
            FileType = fileType;
        }
    }
}
