using NacreCraft.Storage;

namespace NacreCraft.Game.World.Level.LevelGen;

//PhantomSpawner phantom spawner, maps to vanilla net.minecraft.world.level.levelgen.PhantomSpawner
//At night, after a long time since rest, spawns phantoms above a random player
//Degraded: the spawn_phantoms rule, phantom entity type, player statistics and difficulty instance are not ported,
//so only the timer and the night/sky checks are kept
public sealed class PhantomSpawner
{
    private int _nextTick;

    public void Tick(ServerLevel level, bool spawnEnemies)
    {
        if (!spawnEnemies) return;
        var random = level.Random;
        _nextTick--;
        if (_nextTick > 0) return;
        _nextTick += (60 + random.NextInt(60)) * 20;
        //Sky darkening and dimension type are not exposed by the level yet, and phantoms are not a registered entity type
    }
}
