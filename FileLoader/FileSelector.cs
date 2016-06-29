using FileProcessing.Loader.Models;

namespace FileProcessing.Loader
{
    public class FileSelector : IFileSelector
    {
        public ProcessFile GetFileToProcess()
        {
            return new ProcessFile();
        }
    }
}
