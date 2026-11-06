using NacreCraft.Game.Network.Protocol.Game;
using NacreCraft.Network;

namespace NacreCraft.Client.Multiplayer.Prediction;

//PredictiveAction builds the packet for a predicted action, maps to vanilla net.minecraft.client.multiplayer.prediction.PredictiveAction
public delegate Packet<ServerGamePacketListener> PredictiveAction(int sequence);
