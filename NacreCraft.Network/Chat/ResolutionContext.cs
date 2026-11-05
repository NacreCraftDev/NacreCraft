using NacreCraft.Commands;

namespace NacreCraft.Network.Chat;

//ResolutionContext the context used when resolving components, maps to vanilla net.minecraft.network.chat.ResolutionContext
//Vanilla validates ObjectInfo instances; the object content type is not ported, so the validator works on object
public sealed record ResolutionContext(CommandSourceStack? Source, object? DefaultScoreboardEntity,
    Func<object, bool> ObjectInfoValidator, int DepthLimit, ResolutionContext.LimitBehavior DepthLimitBehavior)
{
    //Validate keeps the value when the validator accepts it
    public object? Validate(object description) => ObjectInfoValidator(description) ? description : null;

    public static ResolutionContext Create(CommandSourceStack source) => CreateBuilder().WithSource(source).Build();

    public static Builder CreateBuilder() => new();

    //LimitBehavior what happens once the depth limit is exceeded
    public enum LimitBehavior
    {
        DiscardRemaining,
        StopProcessingAndCopyRemaining
    }

    //Builder builder of a resolution context, maps to vanilla ResolutionContext.Builder
    public sealed class Builder
    {
        private CommandSourceStack? _source;
        private object? _defaultScoreboardEntity;
        private Func<object, bool> _objectInfoValidator = _ => true;
        private int _depthLimit = 100;
        private LimitBehavior _depthLimitBehavior = LimitBehavior.StopProcessingAndCopyRemaining;

        public Builder WithSource(CommandSourceStack source)
        {
            _source = source;
            return this;
        }

        public Builder WithEntityOverride(object? defaultScoreboardEntity)
        {
            _defaultScoreboardEntity = defaultScoreboardEntity;
            return this;
        }

        public Builder WithObjectInfoValidator(Func<object, bool> objectInfoValidator)
        {
            _objectInfoValidator = objectInfoValidator;
            return this;
        }

        public Builder SetDepthLimit(int depthLimit)
        {
            _depthLimit = depthLimit;
            return this;
        }

        public Builder SetDepthLimitBehavior(LimitBehavior behavior)
        {
            _depthLimitBehavior = behavior;
            return this;
        }

        public ResolutionContext Build()
            => new(_source, _defaultScoreboardEntity, _objectInfoValidator, _depthLimit, _depthLimitBehavior);
    }
}
