namespace FileProcessing.Core
{
    public interface IService
    {
        string Name { get; }

        bool Start();

        bool Stop();
    }
}
