using System.Net.Http;
using System.Security.Cryptography;
using NacreCraft.Logging;

namespace NacreCraft.Resources;

//DownloadQueue downloads server resource packs into a per-pack cache, maps to vanilla net.minecraft.server.packs.DownloadQueue
//Simplified: the HTTP layer is HttpClient directly and the JSON event log is not kept
public sealed class DownloadQueue : IDisposable
{
    private const int MAX_KEPT_PACKS = 20;
    private static readonly HttpClient Client = new();

    private readonly string _cacheDir;

    public DownloadQueue(string cacheDir)
    {
        _cacheDir = cacheDir;
        Directory.CreateDirectory(cacheDir);
        DownloadCacheCleaner.VacuumCacheDir(cacheDir, MAX_KEPT_PACKS);
    }

    public sealed record DownloadRequest(string Url, string? Hash);

    public sealed record BatchConfig(IReadOnlyDictionary<string, string> Headers, long MaxSize = long.MaxValue);

    public sealed record BatchResult(Dictionary<Guid, string> Downloaded, HashSet<Guid> Failed);

    //RunDownloadAsync downloads every request into its own cache folder, maps to vanilla runDownload
    public async Task<BatchResult> RunDownloadAsync(BatchConfig config,
        IReadOnlyDictionary<Guid, DownloadRequest> requests, CancellationToken cancellationToken = default)
    {
        var result = new BatchResult(new Dictionary<Guid, string>(), new HashSet<Guid>());
        foreach (var (id, request) in requests)
        {
            try
            {
                var path = await DownloadAsync(id, request, config, cancellationToken).ConfigureAwait(false);
                result.Downloaded[id] = path;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to download {request.Url}: {e.Message}");
                result.Failed.Add(id);
            }
        }
        return result;
    }

    private async Task<string> DownloadAsync(Guid id, DownloadRequest request, BatchConfig config,
        CancellationToken cancellationToken)
    {
        var targetDir = Path.Combine(_cacheDir, id.ToString());
        Directory.CreateDirectory(targetDir);
        var fileName = Path.GetFileName(new Uri(request.Url).AbsolutePath);
        if (string.IsNullOrEmpty(fileName)) fileName = "pack.zip";
        var targetPath = Path.Combine(targetDir, fileName);

        using var message = new HttpRequestMessage(HttpMethod.Get, request.Url);
        foreach (var (key, value) in config.Headers) message.Headers.TryAddWithoutValidation(key, value);
        using var response = await Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is { } length && length > config.MaxSize)
            throw new IOException($"Pack exceeds the size limit: {length} > {config.MaxSize}");
        await using (var output = File.Create(targetPath))
            await response.Content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);

        if (request.Hash is { } expected)
        {
            var actual = Convert.ToHexString(SHA1.HashData(File.ReadAllBytes(targetPath)));
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Hash mismatch: expected {expected}, got {actual}");
        }
        return targetPath;
    }

    public void Dispose()
    {
        DownloadCacheCleaner.VacuumCacheDir(_cacheDir, MAX_KEPT_PACKS);
    }
}
