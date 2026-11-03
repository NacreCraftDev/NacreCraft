using NacreCraft.Logging;

namespace NacreCraft.Client.RealmsClient.Gui.Task;

//RepeatedDelayStrategy how many cycles a task waits before running again
//maps to com.mojang.realmsclient.gui.task.RepeatedDelayStrategy
public interface RepeatedDelayStrategy
{
    public static readonly RepeatedDelayStrategy Constant = new ConstantStrategy();

    long DelayCyclesAfterSuccess();

    long DelayCyclesAfterFailure();

    //ExponentialBackoff doubles the wait per failure up to maxBackoff, maps to vanilla exponentialBackoff
    public static RepeatedDelayStrategy ExponentialBackoff(int maxBackoff) => new ExponentialBackoffStrategy(maxBackoff);

    private sealed class ConstantStrategy : RepeatedDelayStrategy
    {
        public long DelayCyclesAfterSuccess() => 1;

        public long DelayCyclesAfterFailure() => 1;
    }

    private sealed class ExponentialBackoffStrategy(int maxBackoff) : RepeatedDelayStrategy
    {
        private int _failureCount;

        public long DelayCyclesAfterSuccess()
        {
            _failureCount = 0;
            return 1;
        }

        public long DelayCyclesAfterFailure()
        {
            _failureCount++;
            //Shift counts wrap in C#, so the exponent is capped before the min against maxBackoff
            var expandedDelay = Math.Min(1L << Math.Min(_failureCount, 62), (long)maxBackoff);
            Log.Debug($"Skipping for {expandedDelay} extra cycles");
            return expandedDelay;
        }
    }
}
