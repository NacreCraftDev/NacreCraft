namespace NacreCraft.Client.Blaze3d;

//GLFWErrorCapture collects GLFW errors raised while a scope is installed, maps to vanilla com.mojang.blaze3d.GLFWErrorCapture
//NC receives the same callback signature from Silk.NET, so Invoke can be passed straight to Glfw.SetErrorCallback
public sealed class GLFWErrorCapture : IEnumerable<GLFWErrorCapture.Error>
{
    private List<Error>? _errors;

    public void Invoke(int error, string description)
    {
        _errors ??= new List<Error>();
        _errors.Add(new Error(error, description));
    }

    public IEnumerator<Error> GetEnumerator() => (_errors ?? Enumerable.Empty<Error>()).GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    public Error? FirstError() => _errors is { Count: > 0 } ? _errors[0] : null;

    public sealed record Error(int ErrorCode, string Description)
    {
        public override string ToString() => $"[GLFW 0x{ErrorCode:X}] {Description}";
    }
}
