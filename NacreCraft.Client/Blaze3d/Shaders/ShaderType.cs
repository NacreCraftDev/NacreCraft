namespace NacreCraft.Client.Blaze3d.Shaders;

//ShaderType shader stage, maps to vanilla com.mojang.blaze3d.shaders.ShaderType
public enum ShaderType
{
    Vertex,
    Fragment
}

//ShaderTypeExtensions helpers mirroring vanilla ShaderType name/extension/idConverter
public static class ShaderTypeExtensions
{
    //GetName stage name used in shader file names, maps to vanilla getName
    public static string GetName(this ShaderType type) => type == ShaderType.Vertex ? "vertex" : "fragment";

    //Extension file extension of the stage, maps to vanilla extension
    public static string Extension(this ShaderType type) => type == ShaderType.Vertex ? ".vsh" : ".fsh";

    //ByLocation resolves the stage from a path suffix, maps to vanilla byLocation
    public static ShaderType? ByLocation(string path)
        => path.EndsWith(".vsh") ? ShaderType.Vertex
            : path.EndsWith(".fsh") ? ShaderType.Fragment
            : null;
}
