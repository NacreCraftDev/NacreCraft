using NacreCraft.Commands;
using NacreCraft.Commands.Arguments;
using NacreCraft.Commands.Builder;
using NacreCraft.Commands.Context;
using NacreCraft.Commands.Execution;
using NacreCraft.Commands.Execution.Tasks;

namespace NacreCraft.Game.Commands;

//ReturnCommand /return command, maps to vanilla net.minecraft.server.commands.ReturnCommand
//Inside a function the value or failure travels back through the frame return consumer, which is how /function reports a result
//and how an enclosing function can hand one back; the run modifier keeps the rest of the line in return mode
public static class ReturnCommand
{
    public static void Register(CommandDispatcher<CommandSourceStack> dispatcher)
    {
        dispatcher.Register(LiteralArgumentBuilder<CommandSourceStack>.Literal("return")
            .Requires(s => s.HasPermission(2))
            .Then(RequiredArgumentBuilder<CommandSourceStack, int>.Argument("value", IntegerArgumentType.Integer())
                .Executes(new ReturnValueExecutor()))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("fail").Executes(new ReturnFailExecutor()))
            .Then(LiteralArgumentBuilder<CommandSourceStack>.Literal("run")
                .ForwardCustom(dispatcher.GetRoot(), new ReturnFromCommandModifier(), false)));
    }

    //ReturnValueExecutor returns a success value, maps to vanilla ReturnValueCustomExecutor
    private sealed class ReturnValueExecutor : CustomCommandExecutor<CommandSourceStack>
    {
        public void Run(CommandSourceStack sender, ContextChain<CommandSourceStack> currentStep,
            ChainModifiers modifiers, ExecutionControl<CommandSourceStack> output)
        {
            var returnValue = IntegerArgumentType.GetInteger(currentStep.GetTopContext(), "value");
            sender.Callback.OnSuccess(returnValue);
            var frame = output.CurrentFrame();
            frame.ReturnSuccess(returnValue);
            frame.Discard();
        }
    }

    //ReturnFailExecutor returns a failure, maps to vanilla ReturnFailCustomExecutor
    private sealed class ReturnFailExecutor : CustomCommandExecutor<CommandSourceStack>
    {
        public void Run(CommandSourceStack sender, ContextChain<CommandSourceStack> currentStep,
            ChainModifiers modifiers, ExecutionControl<CommandSourceStack> output)
        {
            sender.Callback.OnFailure();
            var frame = output.CurrentFrame();
            frame.ReturnFailure();
            frame.Discard();
        }
    }

    //ReturnFromCommandModifier runs the following command in return mode, maps to vanilla ReturnFromCommandCustomModifier
    private sealed class ReturnFromCommandModifier : CustomModifierExecutor<CommandSourceStack>
    {
        public void Apply(CommandSourceStack originalSource, List<CommandSourceStack> sources,
            ContextChain<CommandSourceStack> chain, ChainModifiers modifiers,
            ExecutionControl<CommandSourceStack> output)
        {
            if (sources.Count == 0)
            {
                if (modifiers.IsReturn()) output.QueueNext(FallthroughTask<CommandSourceStack>.Instance());
                return;
            }
            //The current frame is replaced by the continuation, which carries the return flag into the next stage
            output.CurrentFrame().Discard();
            var nextState = chain.NextStage()
                ?? throw new InvalidOperationException("return run must be followed by a command");
            var command = nextState.GetTopContext().GetInput();
            var continuation = new BuildContexts<CommandSourceStack>.Continuation(command, nextState,
                modifiers.SetReturn(), originalSource, sources);
            output.QueueNext(continuation.Execute);
        }
    }
}
