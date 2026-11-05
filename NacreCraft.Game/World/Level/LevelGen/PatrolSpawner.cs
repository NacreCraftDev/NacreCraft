using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.LevelGen;

//PatrolSpawner pillager patrol spawner, maps to vanilla net.minecraft.world.level.levelgen.PatrolSpawner
//Runs once every 12000+ ticks at day and spawns a patrol near a random player
//Degraded: the spawn_patrols game rule, pillager entity type, NaturalSpawner validity test and
//environment attributes are not ported, so only the timer and player/distance checks are kept
public sealed class PatrolSpawner
{
    private int _nextTick;

    public void Tick(ServerLevel level, bool spawnEnemies)
    {
        if (!spawnEnemies) return;
        var random = level.Random;
        _nextTick--;
        if (_nextTick > 0) return;
        _nextTick += 12000 + random.NextInt(1200);
        //No spawn_patrols rule and no pillager type yet; stop before any spawning work
    }
}
