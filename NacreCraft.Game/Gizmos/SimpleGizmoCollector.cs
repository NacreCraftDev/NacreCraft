namespace NacreCraft.Game.Gizmos;

//SimpleGizmoCollector keeps gizmos until the render pass drains them, maps to vanilla net.minecraft.gizmos.SimpleGizmoCollector
public sealed class SimpleGizmoCollector : GizmoCollector
{
    private readonly List<GizmoInstance> _gizmos = new();
    private readonly List<GizmoInstance> _temporaryGizmos = new();

    public GizmoProperties Add(Gizmo gizmo)
    {
        var instance = new GizmoInstance(gizmo);
        _gizmos.Add(instance);
        return instance;
    }

    public List<GizmoInstance> DrainGizmos()
    {
        var result = new List<GizmoInstance>(_gizmos);
        result.AddRange(_temporaryGizmos);
        var currentMillis = Environment.TickCount64;
        _gizmos.RemoveAll(gizmo => gizmo.ExpireTimeMillis < currentMillis);
        _temporaryGizmos.Clear();
        return result;
    }

    public List<GizmoInstance> GetGizmos() => _gizmos;

    public void AddTemporaryGizmos(IEnumerable<GizmoInstance> gizmos) => _temporaryGizmos.AddRange(gizmos);

    //GizmoInstance one collected gizmo plus its display properties
    public sealed class GizmoInstance : GizmoProperties
    {
        private readonly Gizmo _gizmo;
        private bool _isAlwaysOnTop;
        private long _startTimeMillis;
        private long _expireTimeMillis;
        private bool _shouldFadeOut;

        internal GizmoInstance(Gizmo gizmo) => _gizmo = gizmo;

        public GizmoProperties SetAlwaysOnTop()
        {
            _isAlwaysOnTop = true;
            return this;
        }

        public GizmoProperties PersistForMillis(int milliseconds)
        {
            _startTimeMillis = Environment.TickCount64;
            _expireTimeMillis = _startTimeMillis + milliseconds;
            return this;
        }

        public GizmoProperties FadeOut()
        {
            _shouldFadeOut = true;
            return this;
        }

        public float GetAlphaMultiplier(long currentMillis)
        {
            if (!_shouldFadeOut) return 1.0f;
            var duration = _expireTimeMillis - _startTimeMillis;
            var timeSinceStart = currentMillis - _startTimeMillis;
            return 1.0f - Math.Clamp((float)timeSinceStart / duration, 0.0f, 1.0f);
        }

        public bool IsAlwaysOnTop() => _isAlwaysOnTop;

        public long ExpireTimeMillis => _expireTimeMillis;

        public Gizmo GizmoValue => _gizmo;
    }
}
