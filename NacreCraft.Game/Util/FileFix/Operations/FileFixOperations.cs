using NacreCraft.Storage;

namespace NacreCraft.Game.Util.FileFix;

//FileFixOperations factories for the common operations, maps to vanilla net.minecraft.util.filefix.operations.FileFixOperations
public static class FileFixOperations
{
    public static Move MoveSimple(string file) => new(file, file);

    public static Move Move(string from, string to) => new(from, to);

    public static RegexMove MoveRegex(string filePattern, string replacePattern) => new(filePattern, replacePattern);

    public static DeleteFileOrEmptyDirectory Delete(string target) => new(target);

    public static ApplyInFolders ApplyInFolders(FileRelation applicableFolders, IReadOnlyList<FileFixOperation> operations)
        => new(applicableFolders, operations);

    public static GroupMove GroupMove(IReadOnlyDictionary<string, string> data, IReadOnlyList<Move> moves)
        => new(data, moves);
}
