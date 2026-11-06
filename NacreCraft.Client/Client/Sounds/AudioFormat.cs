namespace NacreCraft.Client.Sounds;

//AudioFormat decoded PCM layout, maps to the subset of javax.sound.sampled.AudioFormat the audio layer reads
public sealed record AudioFormat(int SampleRate, int SampleSizeInBits, int Channels, bool Signed);
