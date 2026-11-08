namespace NacreCraft.Client.Telemetry;

//ITelemetrySession the authlib TelemetrySession counterpart, disabled when there is no authenticated user
public interface ITelemetrySession
{
    bool IsEnabled { get; }

    ITelemetryEvent CreateNewEvent(string exportKey);
}

//ITelemetryEvent one event being exported, maps to authlib TelemetryEvent/TelemetryPropertyContainer
public interface ITelemetryEvent
{
    void AddProperty(string key, string value);

    void AddProperty(string key, int value);

    void AddProperty(string key, long value);

    void AddProperty(string key, bool value);

    void AddNullProperty(string key);

    void Send();
}
