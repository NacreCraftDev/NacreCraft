namespace NacreCraft.DataFixer.Schemas;

using System;
using System.Collections.Generic;
using NacreCraft.DataFixer.Types.Templates;

//V2832 schema node, maps to vanilla net.minecraft.util.datafix.schemas.V2832
//Degraded: the type registrations of this version are not ported yet; the node only forwards to its parent
public class V2832 : NamespacedSchema
{
    public V2832(int versionKey, Schema? parent) : base(versionKey, parent) { }

    public override Dictionary<string, Func<TypeTemplate>> RegisterEntities(Schema schema)
        => base.RegisterEntities(schema);
}
