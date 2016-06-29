using FileProcessing.Loader.Models;

namespace FileProcessing.Loader
{
    public interface IFileSelector
    {
        ProcessFile GetFileToProcess();
    }
}