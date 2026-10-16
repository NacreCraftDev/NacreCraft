//Client/Network subdirectory global usings to simplify client protocol file references
//Same origin as Game/Network/GlobalUsings.cs; after the client protocol implementation moved into this library it needs the same set
global using NacreCraft.Config;
global using NacreCraft.Network;
global using NacreCraft.Network.Protocol;
global using NacreCraft.Network.Chat;
global using NacreCraft.Game.World.Inventory;
global using NacreCraft.Network.Protocol.Common;
global using NacreCraft.Network.Protocol.Configuration;
global using NacreCraft.Network.Protocol.Login;
global using NacreCraft.Network.Protocol.Cookie;
global using NacreCraft.Game.Network.Protocol.Game;
global using NacreCraft.Client.Network;
global using NacreCraft.Client.Network.Protocol.Game;
global using NacreCraft.Client.Network.Protocol.Login;
global using NacreCraft.Client.Network.Protocol.Configuration;
global using NacreCraft.Registry;
//The world namespace is referenced as the World prefix by client network and level code
global using World = NacreCraft.Game.World;
