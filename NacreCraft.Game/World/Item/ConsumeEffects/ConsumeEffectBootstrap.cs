namespace NacreCraft.Game.World.Items.ConsumeEffects;

//ConsumeEffectBootstrap consume effect type registration, maps to the vanilla ConsumeEffect.Type static block
//Touching one type field registers only that type, so this entry touches all five before the registry is frozen and item components decode
public static class ConsumeEffectBootstrap
{
    public static void RegisterAll()
    {
        _ = ApplyStatusEffectsConsumeEffect.EffectType;
        _ = RemoveStatusEffectsConsumeEffect.EffectType;
        _ = ClearAllStatusEffectsConsumeEffect.EffectType;
        _ = TeleportRandomlyConsumeEffect.EffectType;
        _ = PlaySoundConsumeEffect.EffectType;
    }
}
