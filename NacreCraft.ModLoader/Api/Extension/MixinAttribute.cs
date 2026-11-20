namespace NacreCraft.ModLoader.Api.Extension;

//Mixin moves the fields and methods of the class carrying this annotation into the target type
//Equivalent to a mixins entry in ncmod.json; when both sides declare the same target the annotation wins
//After the move the source class is left as an empty shell and mod code should not use it, same as a
//Mixin's mixin class
//The rules are read statically from metadata by the loader, readable even when nothing is loaded
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MixinAttribute : Attribute
{
    //Mixin target: the type the members are moved into
    public MixinAttribute(Type target)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    //Target: the target type
    //What ends up in the metadata is the type's serialized name, just a string; reading the rule
    //never needs that type to be loaded
    public Type Target { get; }

    //Interfaces: interfaces the target type should implement as well
    //The moved instance methods are marked virtual so interface dispatch finds them through the vtable
    public Type[] Interfaces { get; set; } = Array.Empty<Type>();
}
