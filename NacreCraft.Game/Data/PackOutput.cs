using NacreCraft.Registry;

namespace NacreCraft.Game.Data;

//PackOutput the root output folder of a data run, maps to vanilla net.minecraft.data.PackOutput
public sealed class PackOutput(string outputFolder)
{
    public enum Target
    {
        DATA_PACK,
        RESOURCE_PACK,
        REPORTS
    }

    public string OutputFolder => outputFolder;

    public string GetOutputFolder() => outputFolder;

    public string GetOutputFolder(Target target)
        => Path.Combine(outputFolder, target switch
        {
            Target.DATA_PACK => "data",
            Target.RESOURCE_PACK => "assets",
            _ => "reports"
        });

    public PathProvider CreatePathProvider(Target target, string kind) => new(this, target, kind);

    public sealed class PathProvider(PackOutput output, Target target, string kind)
    {
        private readonly string _root = output.GetOutputFolder(target);

        public string File(Identifier element, string extension)
            => Resolve(element.WithPath(path => kind + "/" + path + "." + extension));

        public string Json(Identifier element)
            => Resolve(element.WithPath(path => kind + "/" + path + ".json"));

        public string Json<T>(ResourceKey<T> element) where T : class => Json(element.Identifier);

        private string Resolve(Identifier relative)
            => Path.Combine(_root, relative.Path.Replace('/', Path.DirectorySeparatorChar));
    }
}
