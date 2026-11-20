using NacreCraft.Client.Blaze3d.Systems;
using NacreCraft.Client.Blaze3d.Vertex;
using NacreCraft.Client.Render.Rendertype;

namespace NacreCraft.Client.Render;

//MultiBufferSource hands out one vertex consumer per render type, maps to vanilla net.minecraft.client.renderer.MultiBufferSource
public interface MultiBufferSource
{
    VertexConsumer GetBuffer(RenderType renderType);

    //BufferSource batches every render type into one staged buffer, maps to vanilla MultiBufferSource.BufferSource
    //GetBuffer selects the render type, EndBatch records one draw per render type through the matching PreparedRenderType
    public sealed class BufferSource : MultiBufferSource, IDisposable
    {
        private readonly GpuDevice _device;
        private readonly StagedVertexBuffer _staged = new();
        private readonly Dictionary<RenderType, Draw> _draws = new();
        private readonly List<(RenderType Type, Draw Draw)> _order = new();
        private bool _building;
        private bool _disposed;

        //IsEmpty whether no render type has been requested since the last batch
        public bool IsEmpty => !_building;

        //LastDrawCount draws recorded by the most recent EndBatch
        public int LastDrawCount { get; private set; }

        public BufferSource(GpuDevice device)
        {
            _device = device;
        }

        public VertexConsumer GetBuffer(RenderType renderType)
        {
            if (_draws.TryGetValue(renderType, out var existing))
                return _staged.GetVertexBuilder(existing);

            var draw = _staged.AppendDraw(renderType.Format(), renderType.PrimitiveTopology());
            _draws[renderType] = draw;
            _order.Add((renderType, draw));
            _building = true;
            return _staged.GetVertexBuilder(draw);
        }

        //EndBatch ends every draw, uploads the staged buffers and records the draws into the open pass
        //Prepare resolves the dynamic transform and textures for each render type; CurrentContext must be installed
        public void EndBatch(RenderPass pass)
        {
            if (!_building)
            {
                LastDrawCount = 0;
                return;
            }
            foreach (var (_, draw) in _order)
                _staged.EndDraw(draw);
            _staged.Upload(_device);

            var draws = 0;
            foreach (var (type, draw) in _order)
            {
                if (draw.VertexCount == 0) continue;
                type.Prepare().Draw(pass, _staged.GetExecuteInfo(draw));
                draws++;
            }
            LastDrawCount = draws;

            Reset();
        }

        //Discard drops the current batch without drawing it
        public void Discard() => Reset();

        private void Reset()
        {
            _staged.EndFrame();
            _draws.Clear();
            _order.Clear();
            _building = false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _staged.Dispose();
            _disposed = true;
        }
    }
}
