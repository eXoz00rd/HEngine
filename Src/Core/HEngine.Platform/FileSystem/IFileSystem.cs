namespace HEngine.Platform.FileSystem;

public interface IFileSystem
{
    bool FileExists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    byte[] ReadAllBytes(string path);
    void WriteAllBytes(string path, byte[] data);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
    void CreateDirectory(string path);
}
