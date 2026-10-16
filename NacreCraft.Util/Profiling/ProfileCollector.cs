using NacreCraft.Codec;
using NacreCraft.Util.Profiling.Metrics;

namespace NacreCraft.Util.Profiling;

//Profile collector interface, maps to vanilla net.minecraft.util.profiling.ProfileCollector
//Extends ProfilerFiller with result/path entry/charted path accessors
public interface ProfileCollector : ProfilerFiller
{
    ProfileResults GetResults();

    ProfilerPathEntry? GetEntry(string path);

    ISet<Pair<string, MetricCategory>> GetChartedPaths();
}
