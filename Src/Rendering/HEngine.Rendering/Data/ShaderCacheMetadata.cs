using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HEngine.Rendering.Data;

public sealed class ShaderCacheMetadata
{
    [JsonPropertyName("sourceHash")]
    public string SourceHash { get; set; } = string.Empty;

    [JsonPropertyName("sourceTimestamp")]
    public long SourceTimestamp { get; set; }

    [JsonPropertyName("variantKey")]
    public string VariantKey { get; set; } = string.Empty;

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("shaderFileName")]
    public string ShaderFileName { get; set; } = string.Empty;

    [JsonPropertyName("cacheVersion")]
    public int CacheVersion { get; set; } = 1;

    public static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes);
    }

    public bool IsValid(string shaderFilePath, string currentSourceHash)
    {
        if (CacheVersion != 1)
            return false;

        if (SourceHash != currentSourceHash)
            return false;

        if (!File.Exists(shaderFilePath))
            return false;

        var fileTimestamp = File.GetLastWriteTimeUtc(shaderFilePath).Ticks;
        if (SourceTimestamp != fileTimestamp)
            return false;

        return true;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public static ShaderCacheMetadata? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ShaderCacheMetadata>(json);
        }
        catch
        {
            return null;
        }
    }
}
