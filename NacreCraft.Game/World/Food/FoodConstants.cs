namespace NacreCraft.Game.World.Food;

//FoodConstants food and exhaustion constants, maps to vanilla net.minecraft.world.food.FoodConstants
//The numbers drive FoodData's drain, regeneration and starvation branches; names follow the vanilla fields
public static class FoodConstants
{
    //MaxFood the hunger bar maximum, maps to MAX_FOOD
    public const int MaxFood = 20;

    //MaxSaturation the saturation bar maximum, maps to MAX_SATURATION
    public const float MaxSaturation = 20f;

    //StartSaturation saturation a fresh player starts with, maps to START_SATURATION
    public const float StartSaturation = 5f;

    //SaturationFloor unused floor kept for parity with the vanilla field, maps to SATURATION_FLOOR
    public const float SaturationFloor = 2.5f;

    //ExhaustionDrop exhaustion consumed per drop, maps to EXHAUSTION_DROP
    public const float ExhaustionDrop = 4f;

    //HealthTickCount ticks between heal ticks when food is merely sufficient, maps to HEALTH_TICK_COUNT
    public const int HealthTickCount = 80;

    //HealthTickCountSaturated ticks between heal ticks while saturated, maps to HEALTH_TICK_COUNT_SATURATED
    public const int HealthTickCountSaturated = 10;

    //HealLevel food level needed for the slow natural regeneration branch, maps to HEAL_LEVEL
    public const int HealLevel = 18;

    //SprintLevel food level needed to start sprinting, maps to SPRINT_LEVEL
    public const int SprintLevel = 6;

    //StarveLevel food level at which starvation damage starts, maps to STARVE_LEVEL
    public const int StarveLevel = 0;

    //FoodSaturationPoor nutrition saturation modifier of poor food, maps to FOOD_SATURATION_POOR
    public const float FoodSaturationPoor = 0.1f;

    //FoodSaturationLow nutrition saturation modifier of low food, maps to FOOD_SATURATION_LOW
    public const float FoodSaturationLow = 0.3f;

    //FoodSaturationNormal nutrition saturation modifier of normal food, maps to FOOD_SATURATION_NORMAL
    public const float FoodSaturationNormal = 0.6f;

    //FoodSaturationGood nutrition saturation modifier of good food, maps to FOOD_SATURATION_GOOD
    public const float FoodSaturationGood = 0.8f;

    //FoodSaturationMax nutrition saturation modifier of the best ordinary food, maps to FOOD_SATURATION_MAX
    public const float FoodSaturationMax = 1f;

    //FoodSaturationSupernatural nutrition saturation modifier of golden food, maps to FOOD_SATURATION_SUPERNATURAL
    public const float FoodSaturationSupernatural = 1.2f;

    //ExhaustionHeal exhaustion added per natural heal tick, maps to EXHAUSTION_HEAL
    public const float ExhaustionHeal = 6f;

    //ExhaustionJump exhaustion added by a jump, maps to EXHAUSTION_JUMP
    public const float ExhaustionJump = 0.05f;

    //ExhaustionSprintJump exhaustion added by a sprint jump, maps to EXHAUSTION_SPRINT_JUMP
    public const float ExhaustionSprintJump = 0.2f;

    //ExhaustionMine exhaustion added by breaking a block, maps to EXHAUSTION_MINE
    public const float ExhaustionMine = 0.005f;

    //ExhaustionAttack exhaustion added by an attack, maps to EXHAUSTION_ATTACK
    public const float ExhaustionAttack = 0.1f;

    //ExhaustionWalk exhaustion added by walking, maps to EXHAUSTION_WALK
    public const float ExhaustionWalk = 0f;

    //ExhaustionCrouch exhaustion added by crouching, maps to EXHAUSTION_CROUCH
    public const float ExhaustionCrouch = 0f;

    //ExhaustionSprint exhaustion added by sprinting, maps to EXHAUSTION_SPRINT
    public const float ExhaustionSprint = 0.1f;

    //ExhaustionSwim exhaustion added by swimming, maps to EXHAUSTION_SWIM
    public const float ExhaustionSwim = 0.01f;

    //SaturationByModifier converts a nutrition value and saturation modifier into the saturation the food grants
    //Maps to vanilla saturationByModifier: nutrition * modifier * 2
    public static float SaturationByModifier(int nutrition, float modifier) => nutrition * modifier * 2f;
}
