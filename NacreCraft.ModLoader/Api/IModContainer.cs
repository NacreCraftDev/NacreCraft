namespace NacreCraft.ModLoader.Api;

//IModContainer: the mod-facing read-only view of one loaded mod
//Metadata is an immutable snapshot taken at load, Status is live and changes as the loading flow advances
public interface IModContainer
{
    IModMetadata Metadata { get; }

    //Status: the current status; a mod the loader rejected is not in the container list at all
    ModStatus Status { get; }
}

//IModMetadata: the manifest facts of a mod, all immutable
//The types reused here (ModEnvironment/ModStatus/ModContact) live in the impl namespace and are frozen as contract
public interface IModMetadata
{
    string Id { get; }
    string Version { get; }
    string Name { get; }
    string Description { get; }
    IReadOnlyList<string> Authors { get; }
    IReadOnlyList<string> Contributors { get; }
    string License { get; }

    ModContact Contact { get; }
    ModEnvironment Environment { get; }

    //Depends: the declared dependencies with their version constraints, as written in the manifest
    IReadOnlyDictionary<string, string> Depends { get; }
}
