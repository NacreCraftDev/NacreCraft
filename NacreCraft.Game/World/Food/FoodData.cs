using NacreCraft.Game.Server;
using NacreCraft.Game.World.Level;
using NacreCraft.Nbt;
using NacreCraft.Storage;

namespace NacreCraft.Game.World.Food;

//FoodData the hunger, saturation and exhaustion state of a player, maps to vanilla net.minecraft.world.food.FoodData
//The server ticks it once per player tick: exhaustion drains saturation then food, natural regeneration heals while food is high,
//and an empty food bar deals starvation damage on the vanilla difficulty thresholds
public sealed class FoodData
{
    //DefaultTickTimer initial tick timer, maps to DEFAULT_TICK_TIMER
    private const int DefaultTickTimer = 0;

    //DefaultExhaustionLevel initial exhaustion, maps to DEFAULT_EXHAUSTION_LEVEL
    private const float DefaultExhaustionLevel = 0f;

    //_foodLevel hunger points, 0..20
    private int _foodLevel = FoodConstants.MaxFood;

    //_saturationLevel saturation points, 0..foodLevel
    private float _saturationLevel = FoodConstants.StartSaturation;

    //_exhaustionLevel accumulated exhaustion, capped at 40 by AddExhaustion
    private float _exhaustionLevel = DefaultExhaustionLevel;

    //_tickTimer counts ticks toward the next regeneration or starvation event
    private int _tickTimer = DefaultTickTimer;

    //Add applies food and saturation with the vanilla clamps, saturation can never exceed the hunger bar
    private void Add(int food, float saturation)
    {
        _foodLevel = Math.Clamp(food + _foodLevel, 0, FoodConstants.MaxFood);
        _saturationLevel = Math.Clamp(saturation + _saturationLevel, 0f, _foodLevel);
    }

    //Eat applies food by nutrition and saturation modifier, maps to vanilla eat(int, float)
    public void Eat(int food, float saturationModifier)
        => Add(food, FoodConstants.SaturationByModifier(food, saturationModifier));

    //Eat applies a food component, maps to vanilla eat(FoodProperties)
    public void Eat(FoodProperties foodProperties)
        => Add(foodProperties.Nutrition, foodProperties.Saturation);

    //Tick advances the hunger simulation, maps to vanilla FoodData.tick(ServerPlayer)
    //Runs every player tick on the server; the difficulty comes from the owning server's level data
    public void Tick(ServerPlayer player)
    {
        var difficulty = Difficulty.ByName(player.Server?.LevelData.DifficultyName ?? "normal") ?? Difficulty.Normal;
        //Exhaustion above the drop threshold is spent first on saturation, then on the hunger bar unless peaceful
        if (_exhaustionLevel > FoodConstants.ExhaustionDrop)
        {
            _exhaustionLevel -= FoodConstants.ExhaustionDrop;
            if (_saturationLevel > 0f)
            {
                _saturationLevel = Math.Max(_saturationLevel - 1f, 0f);
            }
            else if (difficulty != Difficulty.Peaceful)
            {
                _foodLevel = Math.Max(_foodLevel - 1, 0);
            }
        }
        var naturalRegen = player.Level.GameRules?.GetBool(GameRules.NaturalHealthRegeneration) ?? true;
        var hurt = player.Health < player.MaxHealth;
        //While saturated and at a full hunger bar the player heals quickly, spending up to 6 saturation per heal
        if (naturalRegen && _saturationLevel > 0f && hurt && _foodLevel >= FoodConstants.MaxFood)
        {
            _tickTimer++;
            if (_tickTimer >= FoodConstants.HealthTickCountSaturated)
            {
                var saturationSpent = Math.Min(_saturationLevel, FoodConstants.ExhaustionHeal);
                player.Heal(saturationSpent / FoodConstants.ExhaustionHeal);
                AddExhaustion(saturationSpent);
                _tickTimer = 0;
            }
            return;
        }
        //With food at or above the heal level the player heals slowly, paying with exhaustion instead of saturation
        if (naturalRegen && _foodLevel >= FoodConstants.HealLevel && hurt)
        {
            _tickTimer++;
            if (_tickTimer >= FoodConstants.HealthTickCount)
            {
                player.Heal(1f);
                AddExhaustion(FoodConstants.ExhaustionHeal);
                _tickTimer = 0;
            }
            return;
        }
        //An empty hunger bar starves the player down to the difficulty floor, maps to the vanilla starvation branch
        if (_foodLevel <= FoodConstants.StarveLevel)
        {
            _tickTimer++;
            if (_tickTimer >= FoodConstants.HealthTickCount)
            {
                if (player.Health > 10f || difficulty == Difficulty.Hard
                    || (player.Health > 1f && difficulty == Difficulty.Normal))
                {
                    player.HurtServer(player.Level, player.DamageSources().Starve(), 1f);
                }
                _tickTimer = 0;
            }
            return;
        }
        _tickTimer = 0;
    }

    //ReadAdditionalSaveData reads the hunger state, maps to vanilla readAdditionalSaveData(ValueInput)
    //Missing fields keep the vanilla defaults so old saves without food data start at full hunger
    public void ReadAdditionalSaveData(CompoundTag tag)
    {
        _foodLevel = tag.Contains("foodLevel") ? tag.GetIntValue("foodLevel") : FoodConstants.MaxFood;
        _tickTimer = tag.Contains("foodTickTimer") ? tag.GetIntValue("foodTickTimer") : DefaultTickTimer;
        _saturationLevel = tag.Contains("foodSaturationLevel")
            ? tag.GetFloatValue("foodSaturationLevel")
            : FoodConstants.StartSaturation;
        _exhaustionLevel = tag.Contains("foodExhaustionLevel")
            ? tag.GetFloatValue("foodExhaustionLevel")
            : DefaultExhaustionLevel;
    }

    //AddAdditionalSaveData writes the hunger state, maps to vanilla addAdditionalSaveData(ValueOutput)
    public void AddAdditionalSaveData(CompoundTag tag)
    {
        tag.PutInt("foodLevel", _foodLevel);
        tag.PutInt("foodTickTimer", _tickTimer);
        tag.PutFloat("foodSaturationLevel", _saturationLevel);
        tag.PutFloat("foodExhaustionLevel", _exhaustionLevel);
    }

    //GetFoodLevel the hunger points
    public int GetFoodLevel() => _foodLevel;

    //HasEnoughFood whether the hunger bar can pay for exhaustive manoeuvres such as sprinting, maps to vanilla hasEnoughFood
    public bool HasEnoughFood() => _foodLevel > FoodConstants.SprintLevel;

    //NeedsFood whether the hunger bar is below full, maps to vanilla needsFood
    public bool NeedsFood() => _foodLevel < FoodConstants.MaxFood;

    //AddExhaustion accumulates exhaustion up to the vanilla cap of 40, maps to vanilla addExhaustion
    public void AddExhaustion(float amount) => _exhaustionLevel = Math.Min(_exhaustionLevel + amount, 40f);

    //GetSaturationLevel the saturation points
    public float GetSaturationLevel() => _saturationLevel;

    //GetExhaustionLevel the accumulated exhaustion, exposed for diagnostics and tests
    public float GetExhaustionLevel() => _exhaustionLevel;

    //GetTickTimer the running regeneration or starvation timer, exposed for diagnostics
    public int GetTickTimer() => _tickTimer;

    //SetFoodLevel overwrites the hunger points, maps to vanilla setFoodLevel
    public void SetFoodLevel(int food) => _foodLevel = food;

    //SetSaturation overwrites the saturation points, maps to vanilla setSaturation
    public void SetSaturation(float saturation) => _saturationLevel = saturation;
}
