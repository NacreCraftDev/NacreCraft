using System.Text;

namespace NacreCraft.Client.Blaze3d.Vulkan.Checkpoints;

//CheckpointFormatter turns retrieved checkpoints into the text attached to a device loss report
//maps to the checkpoint half of vanilla com.mojang.blaze3d.vulkan.VulkanUtils
public static class CheckpointFormatter
{
    public static string Format(IReadOnlyList<CheckpointExtension.QueueCheckpoints> queueCheckpoints)
    {
        if (queueCheckpoints.Count == 0) return "No checkpoints recorded";
        var result = new StringBuilder();
        foreach (var queue in queueCheckpoints)
        {
            result.Append("Queue 0x").Append(queue.Queue.ToString("x")).Append('\n');
            foreach (var checkpoint in queue.Checkpoints)
            {
                result.Append(' ').Append(PipelineStageToString(checkpoint.Stage))
                    .Append(" = ").Append(checkpoint.Type)
                    .Append(' ').Append(checkpoint.Label).Append('\n');
            }
        }
        return result.ToString();
    }

    //PipelineStageToString prints a pipeline stage bit as its Vulkan name, maps to vanilla pipelineStageToString
    public static string PipelineStageToString(ulong stage) => stage switch
    {
        0 => "NONE",
        1 => "TOP_OF_PIPE",
        2 => "DRAW_INDIRECT",
        4 => "VERTEX_INPUT",
        8 => "VERTEX_SHADER",
        16 => "TESSELLATION_CONTROL_SHADER",
        32 => "TESSELLATION_EVALUATION_SHADER",
        64 => "GEOMETRY_SHADER",
        128 => "FRAGMENT_SHADER",
        256 => "EARLY_FRAGMENT_TESTS",
        512 => "LATE_FRAGMENT_TESTS",
        1024 => "COLOR_ATTACHMENT_OUTPUT",
        2048 => "COMPUTE_SHADER",
        4096 => "TRANSFER",
        8192 => "BOTTOM_OF_PIPE",
        16384 => "HOST",
        32768 => "ALL_GRAPHICS",
        65536 => "ALL_COMMANDS",
        131072 => "COMMAND_PREPROCESS",
        262144 => "CONDITIONAL_RENDERING",
        524288 => "TASK_SHADER",
        0x100000 => "MESH_SHADER",
        0x200000 => "RAY_TRACING_SHADER",
        0x400000 => "FRAGMENT_SHADING_RATE_ATTACHMENT",
        0x800000 => "FRAGMENT_DENSITY_PROCESS",
        0x1000000 => "TRANSFORM_FEEDBACK",
        0x2000000 => "ACCELERATION_STRUCTURE_BUILD",
        0x100000000 => "COPY",
        0x200000000 => "RESOLVE",
        0x400000000 => "BLIT",
        0x800000000 => "CLEAR",
        0x1000000000 => "INDEX_INPUT",
        0x2000000000 => "VERTEX_ATTRIBUTE_INPUT",
        0x4000000000 => "PRE_RASTERIZATION_SHADERS",
        _ => "0x" + stage.ToString("x")
    };
}
