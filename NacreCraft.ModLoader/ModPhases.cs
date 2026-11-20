using System.Reflection;
using NacreCraft.ModLoader.Api;

namespace NacreCraft.ModLoader;

//ModPhases: the entrypoint phase vocabulary and the contract each phase demands
//The loader invokes preLaunch and main itself; client and server are driven by ModApi through NcLoader.InvokeEntrypoints
internal static class ModPhases
{
    public const string PreLaunch = "preLaunch";
    public const string Main = "main";
    public const string Client = "client";
    public const string Server = "server";

    //IsKnown: whether a manifest phase name is one of the four the contract defines
    public static bool IsKnown(string phase) => phase is PreLaunch or Main or Client or Server;

    //IsLoaderInvoked: whether the loader runs this phase itself during bootstrap
    public static bool IsLoaderInvoked(string phase) => phase is PreLaunch or Main;

    //ContractOf: the interface an entrypoint type of this phase must implement, null for an unknown phase
    public static Type? ContractOf(string phase) => phase switch
    {
        PreLaunch => typeof(INcPreLaunchInitializer),
        Main => typeof(INcModInitializer),
        Client => typeof(INcClientModInitializer),
        Server => typeof(INcServerModInitializer),
        _ => null,
    };

    //MethodNameOf: the phase method name, kept next to ContractOf so the two can never drift apart
    public static string MethodNameOf(string phase) => phase switch
    {
        PreLaunch => nameof(INcPreLaunchInitializer.OnPreLaunch),
        Main => nameof(INcModInitializer.OnInitialize),
        Client => nameof(INcClientModInitializer.OnInitializeClient),
        Server => nameof(INcServerModInitializer.OnInitializeServer),
        _ => string.Empty,
    };

    //ResolveMethod: the phase method of one entrypoint type, resolved through the interface map
    //so explicit interface implementations are found as well
    public static MethodInfo? ResolveMethod(Type type, string phase)
    {
        var contract = ContractOf(phase);
        if (contract is null)
            return null;

        var map = type.GetInterfaceMap(contract);
        var name = MethodNameOf(phase);
        for (var index = 0; index < map.InterfaceMethods.Length; index++)
        {
            if (map.InterfaceMethods[index].Name == name)
                return map.TargetMethods[index];
        }
        return null;
    }
}
