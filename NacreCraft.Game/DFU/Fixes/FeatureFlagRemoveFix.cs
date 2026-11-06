using NacreCraft.Codec;
using NacreCraft.DataFixer;
using NacreCraft.DataFixer.Fixes;
using NacreCraft.DataFixer.Schemas;

namespace NacreCraft.Game.DFU.Fixes;

//Feature flag removal, maps to vanilla FeatureFlagRemoveFix
//Drops the named flags from enabled_features and records them in removed_features
public sealed class FeatureFlagRemoveFix : DataFix
{
    private readonly string _name;
    private readonly IReadOnlySet<string> _flagsToRemove;

    public FeatureFlagRemoveFix(Schema outputSchema, string name, IReadOnlySet<string> flagsToRemove)
        : base(outputSchema, false)
    {
        _name = name;
        _flagsToRemove = flagsToRemove;
    }

    protected override TypeRewriteRule MakeRule()
        => FixTypeEverywhereTyped(_name, GetInputSchema().GetType(References.LightweightLevel),
            input => input.Update(DSL.RemainderFinder(), FixTag));

    private Dynamic<object> FixTag(Dynamic<object> tag)
    {
        var inactiveFeatures = tag.Get("removed_features").AsStream()
            .Result().OrElse(Enumerable.Empty<Dynamic<object>>()).ToList();
        var result = tag.Update("enabled_features", features =>
        {
            var stream = features.AsStream().Result();
            if (!stream.IsPresent) return features;
            var kept = new List<Dynamic<object>>();
            foreach (var feature in stream.Get())
            {
                var asString = feature.AsString().Result();
                if (!asString.IsPresent)
                {
                    kept.Add(feature);
                    continue;
                }
                if (_flagsToRemove.Contains(asString.Get()))
                {
                    inactiveFeatures.Add(tag.CreateString(asString.Get()));
                    continue;
                }
                kept.Add(feature);
            }
            return features.CreateList(kept);
        });
        return inactiveFeatures.Count > 0
            ? result.Set("removed_features", tag.CreateList(inactiveFeatures))
            : result;
    }
}
