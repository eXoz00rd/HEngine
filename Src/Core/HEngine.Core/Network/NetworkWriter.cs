namespace HEngine.Core.Network;

public class NetworkWriter {
    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;

    public NetworkWriter()
    {
        _writer = new BinaryWriter(_stream);
    }

    public void Write(int value)
        => _writer.Write(value);

    public void Write(float value)
        => _writer.Write(value);

    public void Write(string value)
        => _writer.Write(value);

    public byte[] ToArray()
        => _stream.ToArray();

    public void Dispose()
    {
        _writer?.Dispose();
        _stream?.Dispose();
    }
}

public class NetworkReader {
    private readonly BinaryReader _reader;

    public NetworkReader(byte[] data)
    {
        _reader = new BinaryReader(new MemoryStream(data));
    }

    public int ReadInt32()
        => _reader.ReadInt32();

    public float ReadSingle()
        => _reader.ReadSingle();

    public string ReadString()
        => _reader.ReadString();

    public void Dispose()
        => _reader?.Dispose();
}