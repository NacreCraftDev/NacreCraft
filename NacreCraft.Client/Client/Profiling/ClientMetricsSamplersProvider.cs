using NacreCraft.Client.Render.World;
using NacreCraft.Util.Profiling;
using NacreCraft.Util.Profiling.Metrics;
using NacreCraft.Util.Profiling.Metrics.Profiling;

namespace NacreCraft.Client.Profiling;

//ClientMetricsSamplersProvider registers the client metric samplers, maps to vanilla net.minecraft.client.profiling.ClientMetricsSamplersProvider
//Vanilla exposes LevelExtractor, SectionRenderDispatcher and the GPU device samplers; this port keeps the samplers whose backing
//data already exists (tick time, heap, registry samplers, section counts) and leaves the missing renderer/GPU ones out
public sealed class ClientMetricsSamplersProvider : MetricsSamplerProvider
{
    private readonly LevelRenderer _levelRenderer;
    private readonly HashSet<MetricSampler> _samplers = new();
    private readonly ProfilerSamplerAdapter _samplerFactory = new();

    public ClientMetricsSamplersProvider(Func<long> wallTimeSource, LevelRenderer levelRenderer)
    {
        _levelRenderer = levelRenderer;
        _samplers.Add(CreateTickTimeSampler(wallTimeSource));
        RegisterStaticSamplers();
    }

    private void RegisterStaticSamplers()
    {
        _samplers.Add(MetricSampler.Create("heap MiB", MetricCategory.Jvm,
            () => GC.GetTotalMemory(false) / 1024.0 / 1024.0));
        foreach (var sampler in MetricsRegistry.Instance.GetRegisteredSamplers())
            _samplers.Add(sampler);

        _samplers.Add(MetricSampler.CreateExtractSampler("totalChunks", MetricCategory.ChunkRendering,
            () => _levelRenderer.SectionCount));
        _samplers.Add(MetricSampler.CreateExtractSampler("renderedChunks", MetricCategory.ChunkRendering,
            () => _levelRenderer.VisibleSectionCount));
    }

    public ISet<MetricSampler> Samplers(Func<ProfileCollector> singleTickProfiler)
    {
        foreach (var sampler in _samplerFactory.NewSamplersFoundInProfiler(singleTickProfiler))
            _samplers.Add(sampler);
        return _samplers;
    }

    //CreateTickTimeSampler measures the time between the tick start hook and the sampler read, maps to the vanilla ticktime sampler
    private static MetricSampler CreateTickTimeSampler(Func<long> wallTimeSource)
    {
        long start = 0;
        return MetricSampler.Builder("ticktime", MetricCategory.TickLoop, _ => wallTimeSource() - start, 0)
            .WithBeforeTick(_ => start = wallTimeSource())
            .WithThresholdAlert(new MetricSampler.ValueIncreasedByPercentage(2.0f))
            .Build();
    }
}
