namespace NacreCraft.DataFixer.Schemas;

using System;
using System.Collections.Generic;
using NacreCraft.DataFixer.Types.Templates;

//V4070 schema node, maps to vanilla net.minecraft.util.datafix.schemas.V4070
//Degraded: the type registrations of this version are not ported yet; the node only forwards to its parent
public class V4070 : NamespacedSchema
{
    public V4070(int versionKey, Schema? parent) : base(versionKey, parent) { }

    public override Dictionary<string, Func<TypeTemplate>> RegisterEntities(Schema schema)
        => base.RegisterEntities(schema);
}
