using NacreCraft.Codec;
using NacreCraft.Registry;
using NacreCraft.Registry.State;
using NacreCraft.Util;

namespace NacreCraft.Game.World.Items.Component;

//Tool the mining rules of an item, maps to vanilla net.minecraft.world.item.component.Tool
//Rules are evaluated in order; the first rule matching the block wins
public sealed record Tool(
    IReadOnlyList<Tool.Rule> Rules,
    float DefaultMiningSpeed,
    int DamagePerBlock,
    bool CanDestroyBlocksInCreative)
{
    public static readonly Codec<Tool> Codec = RecordCodecBuilder.Of4(
        Rule.Codec.ListOf().FieldOf("rules").ForGetter((Tool value) => value.Rules),
        Codecs.Float.OptionalFieldOf("default_mining_speed", 1f)
            .ForGetter((Tool value) => value.DefaultMiningSpeed),
        ExtraCodecs.NonNegativeInt.OptionalFieldOf("damage_per_block", 1)
            .ForGetter((Tool value) => value.DamagePerBlock),
        Codecs.Bool.OptionalFieldOf("can_destroy_blocks_in_creative", true)
            .ForGetter((Tool value) => value.CanDestroyBlocksInCreative),
        (rules, speed, damage, creative) => new Tool(rules, speed, damage, creative));

    //GetMiningSpeed the speed for a block state, maps to vanilla getMiningSpeed
    public float GetMiningSpeed(BlockState state)
    {
        foreach (var rule in Rules)
        {
            if (rule.Speed.IsPresent && state.Is(rule.Blocks)) return rule.Speed.Get();
        }
        return DefaultMiningSpeed;
    }

    //IsCorrectForDrops whether the tool is correct for the block's drops, maps to vanilla isCorrectForDrops
    public bool IsCorrectForDrops(BlockState state)
    {
        foreach (var rule in Rules)
        {
            if (rule.CorrectForDrops.IsPresent && state.Is(rule.Blocks)) return rule.CorrectForDrops.Get();
        }
        return false;
    }

    //Rule one mining rule, maps to vanilla Tool.Rule
    public sealed record Rule(HolderSet<Block> Blocks, Optional<float> Speed, Optional<bool> CorrectForDrops)
    {
        public static readonly Codec<Rule> Codec = RecordCodecBuilder.Of3(
            HolderSetCodecs.BlockSet.FieldOf("blocks").ForGetter((Rule value) => value.Blocks),
            Codecs.Float.OptionalFieldOf("speed").ForGetter((Rule value) => value.Speed),
            Codecs.Bool.OptionalFieldOf("correct_for_drops").ForGetter((Rule value) => value.CorrectForDrops),
            (blocks, speed, correct) => new Rule(blocks, speed, correct));
    }
}
