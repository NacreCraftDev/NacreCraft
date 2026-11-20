namespace NacreCraft.ModLoader.Api;

//The entrypoint phase contracts; a manifest entrypoint type must implement the interface of its phase
//All methods are void and synchronous: bootstrap runs before the kernel, so there is nothing to await;
//a mod that needs async work schedules it itself
public interface INcPreLaunchInitializer
{
    //OnPreLaunch: every mod's preLaunch runs before any mod's main
    void OnPreLaunch();
}

public interface INcModInitializer
{
    //OnInitialize: the regular initialization phase, run in dependency order after all mods are prepared
    void OnInitialize();
}

public interface INcClientModInitializer
{
    //OnInitializeClient: driven by ModApi at the client-side kernel anchor, after the mod set is frozen
    void OnInitializeClient();
}

public interface INcServerModInitializer
{
    //OnInitializeServer: driven by ModApi at the server-side kernel anchor, after the mod set is frozen
    void OnInitializeServer();
}
