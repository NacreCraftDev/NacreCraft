namespace NacreCraft.Client.Input;

//CharacterEvent one typed codepoint, maps to vanilla net.minecraft.client.input.CharacterEvent
public sealed record CharacterEvent(int Codepoint)
{
    public string CodepointAsString() => char.ConvertFromUtf32(Codepoint);

    //IsAllowedChatCharacter rejects the formatting marker and control characters, maps to StringUtil.isAllowedChatCharacter
    public bool IsAllowedChatCharacter() => Codepoint != 167 && Codepoint >= 32 && Codepoint != 127;
}
