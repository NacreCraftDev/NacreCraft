using NacreCraft.Registry;

namespace NacreCraft.Client.Blaze3d.Shaders;

//ShaderSource looks up shader source text by location and stage, maps to vanilla com.mojang.blaze3d.shaders.ShaderSource
//The pipeline cache is handed one of these so shader text can come from the resource packs rather than the game jar
public interface IShaderSource
{
    //Get the source for the given location and stage, null when there is none
    string? Get(Identifier location, ShaderType type);
}
