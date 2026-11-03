using NacreCraft.Network.Chat;

namespace NacreCraft.Client.Gui.Narration;

//NarrationElementOutput receives narration pieces for one widget, maps to vanilla NarrationElementOutput
public interface NarrationElementOutput
{
    void Add(NarratedElementType type, NarrationThunk contents);

    //Nest returns a child output so nested widgets get a deeper sort level
    NarrationElementOutput Nest();

    void Add(NarratedElementType type, Component contents) => Add(type, NarrationThunk.From(contents));

    void Add(NarratedElementType type, string contents) => Add(type, NarrationThunk.From(contents));

    void Add(NarratedElementType type, params Component[] contents) => Add(type, NarrationThunk.From(contents));
}
