namespace HEngine.Platform.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    byte[] ReadAllBytes(string path);
    void WriteAllBytes(string path, byte[] data);
    void CreateDirectory(string path);
}
