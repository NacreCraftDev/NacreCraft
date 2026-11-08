using System.IO.Compression;
using NacreCraft.Logging;

namespace NacreCraft.Client.Telemetry;

//TelemetryLogManager owns the dated telemetry logs, maps to vanilla net.minecraft.client.telemetry.TelemetryLogManager
public sealed class TelemetryLogManager : IDisposable
{
    private const string RawExtension = ".json";
    private const int ExpiryDays = 7;

    private readonly string _directory;
    private TelemetryEventLog? _sessionLog;

    private TelemetryLogManager(string directory)
    {
        _directory = directory;
        Prune();
    }

    public static TelemetryLogManager Open(string root)
    {
        Directory.CreateDirectory(root);
        return new TelemetryLogManager(root);
    }

    public TelemetryEventLogger? OpenLogger()
    {
        if (_sessionLog is not null) return _sessionLog;
        var path = Path.Combine(_directory, DateTime.Now.ToString("yyyy-MM-dd") + RawExtension);
        _sessionLog = new TelemetryEventLog(path);
        return _sessionLog;
    }

    //Prune ages out logs older than seven days and compresses older days, matching EventLogDirectory
    private void Prune()
    {
        var today = DateTime.Today;
        try
        {
            foreach (var path in Directory.EnumerateFiles(_directory, "*" + RawExtension))
            {
                var written = File.GetLastWriteTime(path).Date;
                if ((today - written).TotalDays > ExpiryDays)
                {
                    File.Delete(path);
                    continue;
                }
                if (written >= today) continue;

                var gzipPath = path + ".gz";
                if (!File.Exists(gzipPath))
                {
                    using var source = File.OpenRead(path);
                    using var target = File.Create(gzipPath);
                    using var gzip = new GZipStream(target, CompressionLevel.SmallestSize);
                    source.CopyTo(gzip);
                }
                File.Delete(path);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Error($"Failed to prune telemetry logs {e}");
        }
    }

    public void Dispose()
    {
        _sessionLog?.Dispose();
        _sessionLog = null;
    }
}
