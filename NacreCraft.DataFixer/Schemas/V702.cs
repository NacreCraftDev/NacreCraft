namespace NacreCraft.DataFixer.Schemas;

using System;
using System.Collections.Generic;
using NacreCraft.DataFixer.Types.Templates;

//V702 schema node, maps to vanilla net.minecraft.util.datafix.schemas.V702
//Degraded: the type registrations of this version are not ported yet; the node only forwards to its parent
public class V702 : Schema
{
    public V702(int versionKey, Schema? parent) : base(versionKey, parent) { }

    public override Dictionary<string, Func<TypeTemplate>> RegisterEntities(Schema schema)
        => base.RegisterEntities(schema);
}
