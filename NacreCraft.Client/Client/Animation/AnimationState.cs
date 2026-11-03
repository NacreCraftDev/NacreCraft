using System.Numerics;
using NacreCraft.Util;

namespace NacreCraft.Client.Animation;

//AnimationState playback clock for one animation, maps to vanilla net.minecraft.world.entity.AnimationState
public sealed class AnimationState
{
    private const int Stopped = int.MinValue;

    private int _startTick = Stopped;

    public void Start(int tickCount) => _startTick = tickCount;

    public void StartIfStopped(int tickCount)
    {
        if (!IsStarted()) Start(tickCount);
    }

    public void AnimateWhen(bool condition, int tickCount)
    {
        if (condition) StartIfStopped(tickCount);
        else Stop();
    }

    public void Stop() => _startTick = Stopped;

    public void IfStarted(Action<AnimationState> timer)
    {
        if (IsStarted()) timer(this);
    }

    public void FastForward(int ticks, float timeScale)
    {
        if (!IsStarted()) return;
        _startTick -= (int)(ticks * timeScale);
    }

    public long GetTimeInMillis(float ageInTicks) => (long)((ageInTicks - _startTick) * 50f);

    public bool IsStarted() => _startTick != Stopped;

    public void CopyFrom(AnimationState state) => _startTick = state._startTick;
}
