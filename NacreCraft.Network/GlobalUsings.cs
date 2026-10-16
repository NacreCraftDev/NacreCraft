//Network global usings for the protocol layer to simplify protocol file references
//Needed after the connection-layer protocols (handshake/status/ping/login/common/cookie/configuration) were moved into this library
//Deliberately excludes NacreCraft.Network.Chat, whose Component clashes with the NacreCraft.Game.World.Items.Component namespace name
//Under NacreCraft.Network.* it would resolve to the namespace, so files that need it must use their own using or a fully qualified name
global using NacreCraft.Config;
global using NacreCraft.Network;
global using NacreCraft.Network.Protocol;
global using NacreCraft.Registry;
global using NacreCraft.Network.Protocol.Common;
global using NacreCraft.Network.Protocol.Cookie;
