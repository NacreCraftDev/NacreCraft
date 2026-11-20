namespace NacreCraft.Client.Blaze3d;

//GLFWErrorScope installs a capture as the active GLFW error sink and restores the previous one on dispose, maps to vanilla
//com.mojang.blaze3d.GLFWErrorScope
//Silk.NET exposes GLFW through a windowing backend whose callback is owned by the backend, so NC keeps the scope chain in
//managed state and lets the window layer feed the active capture through GLFWErrorCapture.Invoke
public sealed class GLFWErrorScope : IDisposable
{
    private static GLFWErrorCapture? _current;

    private readonly GLFWErrorCapture? _previous;
    private readonly GLFWErrorCapture _expected;
    private bool _closed;

    public GLFWErrorScope(GLFWErrorCapture capture)
    {
        _expected = capture;
        _previous = _current;
        _current = capture;
    }

    //Current the capture currently receiving GLFW errors
    public static GLFWErrorCapture? Current => _current;

    public void Dispose()
    {
        if (_closed) return;
        _closed = true;
        if (!ReferenceEquals(_current, _expected))
            throw new InvalidOperationException("GLFW error callback has unexpectedly changed during this scope!");
        _current = _previous;
    }
}
