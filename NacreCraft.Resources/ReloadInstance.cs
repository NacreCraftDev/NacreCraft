namespace NacreCraft.Resources;

//ReloadInstance a running resource reload, maps to vanilla net.minecraft.server.packs.resources.ReloadInstance
//Vanilla hands this object to the loading overlay, which polls IsDone and ActualProgress and calls CheckExceptions once
//it reports done; the same shape lets any caller drive a reload without blocking on it
public interface ReloadInstance
{
    //IsDone true once every listener ran, or the chain stopped on a failure, maps to vanilla ReloadInstance.isDone
    bool IsDone { get; }

    //ActualProgress 0..1 taken from how many listeners finished, maps to vanilla getActualProgress
    float ActualProgress { get; }

    //CheckExceptions rethrows the failure the chain stopped on, maps to vanilla checkExceptions
    void CheckExceptions();

    //Pump advances the reload by at most one listener and is called by whoever drives it
    //NC runs every listener on the caller's thread rather than vanilla's background executors, so a reload is stepped
    //instead of scheduled; stepping once per tick spreads it over frames and keeps the reported progress honest
    void Pump();
}
