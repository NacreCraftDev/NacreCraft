using NacreCraft.Codec;
using NacreCraft.Network.Chat;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Attribute;

//BedRule how a dimension treats beds, maps to vanilla net.minecraft.world.attribute.BedRule
//The sleep problem type is not ported yet, so only the rule data is carried here
public sealed record BedRule(BedRule.Rule CanSleep, BedRule.Rule CanSetSpawn, bool Explodes,
    Optional<Component> ErrorMessage)
{
    //NightStartTick night begins 12600 ticks into a day, matching the night marker of the day timeline
    private const long NightStartTick = 12600;

    //NightEndTick night ends at the wake-up marker
    private const long NightEndTick = 23401;

    //DayLength tick length of a full day-night cycle
    private const long DayLength = 24000;

    //CanSleepWhenDark the default rule: sleeping needs night, setting the spawn point always works
    public static readonly BedRule CanSleepWhenDark = new(Rule.WhenDark, Rule.Always, false,
        Optional<Component>.Of(Component.Translatable("block.minecraft.bed.no_sleep")));

    //ExplodesRule the nether and end rule: no sleeping and no spawn point, using a bed detonates it
    public static readonly BedRule ExplodesRule = new(Rule.Never, Rule.Never, true, Optional<Component>.Empty());

    //RuleCodec named rule codec
    public static readonly Codec<Rule> RuleCodec = new StringEnumCodec<Rule>(
        (Rule.Always, "always"),
        (Rule.WhenDark, "when_dark"),
        (Rule.Never, "never"));

    public static readonly Codec<BedRule> Codec = RecordCodecBuilder.Of4<BedRule, Rule, Rule, bool, Optional<Component>>(
        RuleCodec.FieldOf("can_sleep").ForGetter<BedRule, Rule>(rule => rule.CanSleep),
        RuleCodec.FieldOf("can_set_spawn").ForGetter<BedRule, Rule>(rule => rule.CanSetSpawn),
        Codecs.Bool.OptionalFieldOf("explodes", false).ForGetter<BedRule, bool>(rule => rule.Explodes),
        ComponentSerialization.Codec.OptionalFieldOf("error_message")
            .ForGetter<BedRule, Optional<Component>>(rule => rule.ErrorMessage),
        (canSleep, canSetSpawn, explodes, errorMessage) => new BedRule(canSleep, canSetSpawn, explodes, errorMessage));

    public bool CanSleepAt(ServerLevel level) => Test(CanSleep, level);

    public bool CanSetSpawnAt(ServerLevel level) => Test(CanSetSpawn, level);

    //IsDarkOutside approximates vanilla Level.isDarkOutside through the day-night window
    //This port has no sky-darken accumulator yet, so the night span of the day timeline stands in for it
    private static bool IsDarkOutside(ServerLevel level)
    {
        var tickInDay = level.GameTime % DayLength;
        if (tickInDay < 0) tickInDay += DayLength;
        return tickInDay >= NightStartTick && tickInDay < NightEndTick;
    }

    //Rule when a bed rule condition holds, maps to vanilla BedRule.Rule
    public enum Rule
    {
        Always,
        WhenDark,
        Never
    }

    //Test evaluates the rule against the level, maps to vanilla BedRule.Rule.test
    private static bool Test(Rule rule, ServerLevel level)
        => rule switch
        {
            Rule.Always => true,
            Rule.WhenDark => IsDarkOutside(level),
            _ => false
        };
}
