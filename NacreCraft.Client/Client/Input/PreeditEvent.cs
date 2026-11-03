using System.Runtime.InteropServices;
using System.Text;
using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Input;

//PreeditEvent an IME composition run, maps to vanilla net.minecraft.client.input.PreeditEvent
public sealed record PreeditEvent
{
    public string FullText { get; }
    public int CaretPosition { get; }
    public IReadOnlyList<string> Blocks { get; }
    public int FocusedBlock { get; }

    public PreeditEvent(string fullText, int caretPosition, IReadOnlyList<string> blocks, int focusedBlock)
    {
        if ((uint)focusedBlock >= (uint)blocks.Count)
            throw new ArgumentOutOfRangeException(nameof(focusedBlock));
        FullText = fullText;
        CaretPosition = caretPosition;
        Blocks = blocks;
        FocusedBlock = focusedBlock;
    }

    //CreateFromCallback reads the IME callback buffers, maps to vanilla createFromCallback
    public static PreeditEvent? CreateFromCallback(int preeditSize, nint preeditPtr, int blockCount, nint blockSizesPtr,
        int focusedBlock, int caret)
    {
        if (preeditSize == 0) return null;
        var codepoints = ReadIntBuffer(preeditSize, preeditPtr);
        var blockSizes = ReadIntBuffer(blockCount, blockSizesPtr);
        var fullText = new StringBuilder();
        var blocks = new List<string>(blockSizes.Length);
        var offset = 0;
        var convertedCaret = 0;
        foreach (var blockSize in blockSizes)
        {
            var blockBuilder = new StringBuilder();
            for (var i = 0; i < blockSize; i++)
            {
                if (offset == caret) convertedCaret = fullText.Length + blockBuilder.Length;
                blockBuilder.Append(char.ConvertFromUtf32(codepoints[offset]));
                offset++;
            }
            var block = blockBuilder.ToString();
            blocks.Add(block);
            fullText.Append(block);
        }
        if (offset == caret) convertedCaret = fullText.Length;
        return new PreeditEvent(fullText.ToString(), convertedCaret, blocks, focusedBlock);
    }

    private static int[] ReadIntBuffer(int size, nint pointer)
    {
        var result = new int[size];
        Marshal.Copy(pointer, result, 0, size);
        return result;
    }

    //ToFormattedText renders the blocks with the focused block highlighted, maps to vanilla toFormattedText
    public MutableComponent ToFormattedText(Style focusedStyle)
    {
        if (Blocks.Count == 1) return Component.Literal(Blocks[0]).WithStyle(focusedStyle);
        var result = Component.Empty();
        for (var i = 0; i < Blocks.Count; i++)
        {
            var part = Component.Literal(Blocks[i]);
            if (i == FocusedBlock) part.WithStyle(focusedStyle);
            result.Append(part);
        }
        return result;
    }
}
