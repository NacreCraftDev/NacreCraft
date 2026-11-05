namespace NacreCraft.DataFixer.Schemas;

using System;
using System.Collections.Generic;
using NacreCraft.DataFixer.Types.Templates;

//V1451 schema node, maps to vanilla net.minecraft.util.datafix.schemas.V1451
//Degraded: the type registrations of this version are not ported yet; the node only forwards to its parent
public class V1451 : NamespacedSchema
{
    public V1451(int versionKey, Schema? parent) : base(versionKey, parent) { }

    public override Dictionary<string, Func<TypeTemplate>> RegisterEntities(Schema schema)
        => base.RegisterEntities(schema);
}
