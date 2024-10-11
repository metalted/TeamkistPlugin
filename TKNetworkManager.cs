using UnityEngine;
using System.Collections.Generic;
using Lidgren.Network;

namespace TeamkistPlugin
{
    //Different kinds of messages on the network.
    public enum TKMessageType
    {
        LogIn = 10,
        JoinedPlayerData = 11,
        ServerPlayerData = 12,
        PlayerTransformData = 13,
        PlayerStateData = 14,
        PlayerLeft = 15,
        ServerData = 20,
        LevelEditorChangeEvents = 100,        
        BlockCreateEvent = 101,
        BlockDestroyEvent = 102,
        BlockChangeEvent = 103,
        EditorFloorEvent = 104,
        EditorSkyboxEvent = 105,
        CustomMessage = 200
    }

    //A message that can hold any kind of information, but is mostly used for level editor changes.
    public class TKMessage
    {
        public TKMessageType messageType;
        public string blockJSON;
        public string UID;
        public string properties;
        public int floor;
        public int skybox;
    }

    //The network manager is responsible for sending, receiving and processing messages.
    public static class TKNetworkManager
    {
        //Are we currenty connecting to the server. Used so users don't bash the connect button.
        public static bool isConnecting = false;
        //Are we currently connected to the teamkist server?
        public static bool isConnectedToServer = false;

        //The netconfiguration and client.
        public static NetPeerConfiguration netPeerConfiguration;
        public static NetClient client = null;

        //Custom message event
        public delegate void CustomEventDelegate(string data);
        public static event CustomEventDelegate customTeamkistEvent;

        //Flag for remote horn
        public static bool isRemoteHorn = false;

        //Initializing the network means creating and starting the client.
        public static void Initialize()
        {
            netPeerConfiguration = new NetPeerConfiguration(TKConfig.appIdentifier);
            netPeerConfiguration.ConnectionTimeout = 5000;
            client = new NetClient(netPeerConfiguration);
            client.Start();
        }

        //Read the messages when necessary.
        public static void Update()
        {
            if(isConnecting || isConnectedToServer)
            {
                ReadMessagesFromServer();
            }
        }

        //Called when we press the teamkist editor button.
        public static void ConnectToTheServer()
        {
            TKManager.LogMessage("ConnectToTheServer()");

            if (client == null)
            {
                TKManager.LogError("Can't connect to server because client has not been initialized yet!");
                return;
            }

            if(isConnectedToServer)
            {
                TKManager.LogWarning("Trying to connect to server, but client is already connected!");
                return;
            }
            //Try to connect to the server.
            if(!isConnecting)
            {
                PlayerManager.Instance.messenger.Log("Connecting to server...", 60f);
                TKManager.LogMessage("Trying to connect to the server.\nServer IP  : " + TKConfig.serverIP + "\nServer Port: " + TKConfig.serverPort);
                try
                {
                    isConnecting = true;
                    client.Connect(TKConfig.serverIP, TKConfig.serverPort);
                }
                catch
                {
                    OnConnectionError();
                }                
            }
            //Impatient people.
            else
            {
                TKManager.LogWarning("Trying to initiate connecting, but client is already connecting!");
            }
        }

        //Called when our status changes to CONNECTED.
        public static void OnConnectedToServer()
        {
            TKManager.LogMessage("OnConnectedToServer()");

            //We are connected and not connecting anymore.
            isConnectedToServer = true;
            isConnecting = false;
            //Log messages.
            PlayerManager.Instance.messenger.Log("Connected to server", 1f);
            TKManager.LogMessage("Connected to server!");

            //Get the current player info so we can log in with their data.
            TKPlayer localInfo = TKPlayerManager.GetLocalPlayerInformation();

            //Log in to the server. Logging in will introduce our data to the server, the server will return the world data to us.
            LogIn(localInfo, true);
        }

        //Log in with our data.
        public static void LogIn(TKPlayer player, bool requestServerData)
        {
            if (client == null)
            {
                TKManager.LogError("Can't log in to server because client has not been initialized yet!");
                return;
            }

            if (!isConnectedToServer)
            {
                TKManager.LogWarning("Can't log in to server because client is not connected!");
                return;
            }

            //Create the log in message.
            //Setting the final bool to true will cause the server to return the data of the world.
            //No use case for false yet.
            NetOutgoingMessage outgoingMessage = client.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.LogIn);
            outgoingMessage.Write(player.name);
            outgoingMessage.Write(player.zeepkist);
            outgoingMessage.Write(player.frontWheels);
            outgoingMessage.Write(player.rearWheels);
            outgoingMessage.Write(player.paraglider);
            outgoingMessage.Write(player.horn);
            outgoingMessage.Write(player.hat);
            outgoingMessage.Write(player.glasses);
            outgoingMessage.Write(player.color_body);
            outgoingMessage.Write(player.color_leftArm);
            outgoingMessage.Write(player.color_rightArm);
            outgoingMessage.Write(player.color_leftLeg);
            outgoingMessage.Write(player.color_rightLeg);
            outgoingMessage.Write(player.color);
            outgoingMessage.Write(requestServerData);
            client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        //Send the transform data, rotation and position of the local user to the server.
        public static void SendTransformData(Vector3 position, Vector3 euler, MultiplayerCharacter.CharacterMode mode)
        {
            if (client == null)
            {
                TKManager.LogError("Can't send transform data to server because client has not been initialized yet!");
                return;
            }

            if (!isConnectedToServer)
            {
                TKManager.LogWarning("Can't send transform data to server because client is not connected!");
                return;
            }

            NetOutgoingMessage outgoingMessage = client.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerTransformData);
            outgoingMessage.Write(position.x);
            outgoingMessage.Write(position.y);
            outgoingMessage.Write(position.z);
            outgoingMessage.Write(euler.x);
            outgoingMessage.Write(euler.y);
            outgoingMessage.Write(euler.z);
            switch (mode)
            {
                case MultiplayerCharacter.CharacterMode.Paraglider:
                    outgoingMessage.Write((byte)2);
                    break;
                case MultiplayerCharacter.CharacterMode.Race:
                    outgoingMessage.Write((byte)1);
                    break;
                default:
                    outgoingMessage.Write((byte)0);
                    break;
            }
            client.SendMessage(outgoingMessage, NetDeliveryMethod.UnreliableSequenced);
        }

        //Set the player state for the local player to the server, this can be building or racing.
        public static void SendPlayerStateMessage(MultiplayerCharacter.CharacterMode mode)
        {
            if (client == null)
            {
                TKManager.LogError("Can't send player state to server because client has not been initialized yet!");
                return;
            }

            if (!isConnectedToServer)
            {
                TKManager.LogWarning("Can't send player state to server because client is not connected!");
                return;
            }

            NetOutgoingMessage outgoingMessage = client.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.PlayerStateData);

            switch (mode)
            {
                case MultiplayerCharacter.CharacterMode.Build:
                case MultiplayerCharacter.CharacterMode.Paint:
                case MultiplayerCharacter.CharacterMode.Read:
                case MultiplayerCharacter.CharacterMode.Treegun:
                    outgoingMessage.Write((byte)0);
                    break;
                case MultiplayerCharacter.CharacterMode.Race:
                case MultiplayerCharacter.CharacterMode.Offroad:
                    outgoingMessage.Write((byte)1);
                    break;
                case MultiplayerCharacter.CharacterMode.Paraglider:
                    outgoingMessage.Write((byte)2);
                    break;
            }

            client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        //Is called when the user presses the quit button in the level editor.
        //The scene transition will cause this function to be called.
        public static void DisconnectFromServer()
        {
            TKManager.LogMessage("DisconnectFromServer()");

            if (client == null)
            {
                TKManager.LogError("Can't disconnect from server because client has not been initialized yet!");
                return;
            }

            if(!isConnectedToServer)
            {
                TKManager.LogWarning("Trying to disconnect from server, but client is not connected!");
                return;
            }

            //Disconnect from the server.
            TKManager.LogMessage("Initiating disconnect from server!");
            client.Disconnect("");
        }

        //Called from the network when our status changes to DISCONNECTED.
        public static void OnDisconnectedFromServer()
        {
            TKManager.LogMessage("OnDisconnectedFromServer()");

            //If we were connected to the server, that means this was a disconnect.
            if(isConnectedToServer)
            {
                //Regular disconnect.
                PlayerManager.Instance.messenger.Log("Disconnected from server!", 3f);
                TKManager.LogMessage("Disconnected from server!");
            }
            //If we werent connected to the server yet, it means we were trying to connect, but connecting failed.
            else
            {
                //Failed to connect.
                PlayerManager.Instance.messenger.Log("Failed to connect to server!", 3f);
                TKManager.LogMessage("Failed to connect to server!");
            }

            //As we disconnected, reset all bools.
            isConnectedToServer = false;
            isConnecting = false;
            TKManager.teamkistEditor = false;
        }

        //This function is called when the ip settings can't be resolved.
        public static void OnConnectionError()
        {
            PlayerManager.Instance.messenger.Log("Network Settings Error! Check IP and Port!", 3f);
            TKManager.LogError("Error in server settings!");
            isConnectedToServer = false;
            isConnecting = false;
            TKManager.teamkistEditor = false;
        }

        //This function will send all level editor block, floor and skybox changes to the server.
        public static void SendChangesToServer(List<TKMessage> changes)
        {
            //Create a message.
            NetOutgoingMessage outgoingMessage = client.CreateMessage();
            //Set the LevelEditorChangeEvents byte header.
            outgoingMessage.Write((byte)TKMessageType.LevelEditorChangeEvents);
            //Set the amount of changes to read.
            outgoingMessage.Write(changes.Count);

            foreach (TKMessage change in changes)
            {
                //Each change will have a type header.
                outgoingMessage.Write((byte)change.messageType);

                switch (change.messageType)
                {
                    case TKMessageType.BlockCreateEvent:
                        outgoingMessage.Write(change.blockJSON);
                        break;
                    case TKMessageType.BlockDestroyEvent:
                        outgoingMessage.Write(change.UID);
                        break;
                    case TKMessageType.BlockChangeEvent:
                        outgoingMessage.Write(change.UID);
                        outgoingMessage.Write(change.properties);
                        break;
                    case TKMessageType.EditorFloorEvent:
                        outgoingMessage.Write(change.floor);
                        break;
                    case TKMessageType.EditorSkyboxEvent:
                        outgoingMessage.Write(change.skybox);
                        break;
                }
            }

            //Send the message with changes to the server.
            client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public static void SendCustomMessage(string message)
        {
            NetOutgoingMessage outgoingMessage = client.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.CustomMessage);
            outgoingMessage.Write(message);
            client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        //Read messages returned from the server.
        public static void ReadMessagesFromServer()
        {
            if(client == null)
            {
                TKManager.LogWarning("Trying to read messages from server but client is not initialized!");
                return;
            }

            NetIncomingMessage incomingMessage;

            while((incomingMessage = client.ReadMessage()) != null)
            {
                TKManager.LogMessage("Network Message: " + incomingMessage.MessageType.ToString());
                switch(incomingMessage.MessageType)
                {
                    //These messages are from Lidgren and are used to determine connection status.
                    case NetIncomingMessageType.StatusChanged:
                        switch(incomingMessage.SenderConnection.Status)
                        {
                            case NetConnectionStatus.Connected:
                                OnConnectedToServer();
                                break;
                            case NetConnectionStatus.Disconnected:
                                OnDisconnectedFromServer();
                                break;
                            default:
                                TKManager.LogMessage("Unhandled Status Change: " + incomingMessage.SenderConnection.Status);
                                break;
                        }
                        break;
                    //Data messages are always custom.
                    case NetIncomingMessageType.Data:

                        //Get the first byte which is the type of message.
                        TKMessageType messageType = (TKMessageType)incomingMessage.ReadByte();
                        switch (messageType)
                        {
                            //Level editor block changes.
                            case TKMessageType.LevelEditorChangeEvents:
                                int changeCount = incomingMessage.ReadInt32();
                                for (int i = 0; i < changeCount; i++)
                                {
                                    TKMessageType changeEventType = (TKMessageType)incomingMessage.ReadByte();
                                    switch (changeEventType)
                                    {
                                        case TKMessageType.BlockCreateEvent:
                                            TKMessageRouter.BlockCreated(incomingMessage.ReadString(), true);
                                            break;
                                        case TKMessageType.BlockDestroyEvent:
                                            TKMessageRouter.BlockDestroyed(incomingMessage.ReadString(), true);
                                            break;
                                        case TKMessageType.BlockChangeEvent:
                                            TKMessageRouter.BlockUpdated(incomingMessage.ReadString(), incomingMessage.ReadString(), true);
                                            break;
                                        case TKMessageType.EditorFloorEvent:
                                            TKMessageRouter.FloorUpdated(incomingMessage.ReadInt32(), true);
                                            break;
                                        case TKMessageType.EditorSkyboxEvent:
                                            TKMessageRouter.SkyboxUpdated(incomingMessage.ReadInt32(), true);
                                            break;
                                    }
                                }
                                break;
                            //Server data we receive after we log in.
                            case TKMessageType.ServerData:

                                //Read all the data from the message.
                                int floorID = incomingMessage.ReadInt32();
                                int skyboxID = incomingMessage.ReadInt32();
                                int blockCount = incomingMessage.ReadInt32();
                                List<string> blockData = new List<string>();

                                for(int i = 0; i < blockCount; i++)
                                {
                                    blockData.Add(incomingMessage.ReadString());
                                }

                                TKManager.LogMessage($"Received Server Data:\nFloor: {floorID}\nSkybox: {skyboxID}\nBlockCount:{blockCount}");
                                
                                //Store the retreived data in the storage class.
                                //Storing world data will automatically clear the dictionary.
                                TKStorage.ImportServerData(floorID, skyboxID, blockData);

                                //Call the data imported function which will cause the game to load the level editor.
                                TKManager.OnServerDataImported();
                                break;
                            //All player information currently on the server, is simultaniously received together with the world data.
                            case TKMessageType.ServerPlayerData:
                                int playerCount = incomingMessage.ReadInt32();
                                List<TKPlayer> playerData = new List<TKPlayer>();
                                for(int i = 0; i < playerCount; i++)
                                {
                                    TKPlayer player = new TKPlayer
                                    {
                                        ID = incomingMessage.ReadInt32(),
                                        state = incomingMessage.ReadByte(),
                                        name = incomingMessage.ReadString(),
                                        zeepkist = incomingMessage.ReadInt32(),
                                        frontWheels = incomingMessage.ReadInt32(),
                                        rearWheels = incomingMessage.ReadInt32(),
                                        paraglider = incomingMessage.ReadInt32(),
                                        horn = incomingMessage.ReadInt32(),
                                        hat = incomingMessage.ReadInt32(),
                                        glasses = incomingMessage.ReadInt32(),
                                        color_body = incomingMessage.ReadInt32(),
                                        color_leftArm = incomingMessage.ReadInt32(),
                                        color_rightArm = incomingMessage.ReadInt32(),
                                        color_leftLeg = incomingMessage.ReadInt32(),
                                        color_rightLeg = incomingMessage.ReadInt32(),
                                        color = incomingMessage.ReadInt32()
                                    };

                                    playerData.Add(player);
                                }
                                TKPlayerManager.ProcessServerPlayerData(playerData);
                                break;
                            //When a player joins the server while we are already online, the server sends this message.
                            case TKMessageType.JoinedPlayerData:
                                TKPlayer joinedPlayer = new TKPlayer
                                {
                                    ID = incomingMessage.ReadInt32(),
                                    state = incomingMessage.ReadByte(),
                                    name = incomingMessage.ReadString(),
                                    zeepkist = incomingMessage.ReadInt32(),
                                    frontWheels = incomingMessage.ReadInt32(),
                                    rearWheels = incomingMessage.ReadInt32(),
                                    paraglider = incomingMessage.ReadInt32(),
                                    horn = incomingMessage.ReadInt32(),
                                    hat = incomingMessage.ReadInt32(),
                                    glasses = incomingMessage.ReadInt32(),
                                    color_body = incomingMessage.ReadInt32(),
                                    color_leftArm = incomingMessage.ReadInt32(),
                                    color_rightArm = incomingMessage.ReadInt32(),
                                    color_leftLeg = incomingMessage.ReadInt32(),
                                    color_rightLeg = incomingMessage.ReadInt32(),
                                    color = incomingMessage.ReadInt32()
                                };
                                TKPlayerManager.OnRemotePlayerJoined(joinedPlayer);
                                break;
                            //When a player left the server, we get this message from the server containing the server ID of that player.
                            case TKMessageType.PlayerLeft:
                                TKPlayerManager.OnRemotePlayerLeft(incomingMessage.ReadInt32());
                                break;
                            //Movement and rotation data of a remote player.
                            case TKMessageType.PlayerTransformData:
                                int ID = incomingMessage.ReadInt32();
                                Vector3 playerPosition = new Vector3(incomingMessage.ReadFloat(), incomingMessage.ReadFloat(), incomingMessage.ReadFloat());
                                Vector3 playerEuler = new Vector3(incomingMessage.ReadFloat(), incomingMessage.ReadFloat(), incomingMessage.ReadFloat());
                                byte playerState = incomingMessage.ReadByte();
                                TKPlayerManager.ProcessRemotePlayerTransformData(ID, playerPosition, playerEuler, playerState);
                                break;
                            //State data of remote player, when switching between racing and building.
                            case TKMessageType.PlayerStateData:
                                int stateID = incomingMessage.ReadInt32();
                                byte state = incomingMessage.ReadByte();
                                switch (state)
                                {
                                    case 2:
                                        TKPlayerManager.OnRemotePlayerParaglider(stateID);
                                        break;
                                    case 1:
                                        TKPlayerManager.OnRemotePlayerToGame(stateID);
                                        break;
                                    default:
                                        TKPlayerManager.OnRemotePlayerToEditor(stateID);
                                        break;
                                }
                                break;
                            case TKMessageType.CustomMessage:
                                try
                                {
                                    string messagePayload = incomingMessage.ReadString();
                                    customTeamkistEvent?.Invoke(messagePayload);
                                }
                                catch { }                                
                                break;
                        }
                        break;
                }
            }
        }

        public static void HandleCustomMessages(string data)
        {
            string[] dataParts = data.Split(";");
            switch (dataParts[0])
            {
                case "Horn":
                    if (dataParts.Length > 1)
                    {
                        string userID = dataParts[1];
                        int userIDInt;
                        if (int.TryParse(userID, out userIDInt))
                        {
                            if (TKPlayerManager.remotePlayers.ContainsKey(userIDInt))
                            {
                                //Get the horn
                                MultiplayerCharacter mpc = TKPlayerManager.remotePlayers[userIDInt];
                                Transform mpct = mpc.transform;
                                int hornID = mpc.playerData.horn;

                                //Play the sound
                                isRemoteHorn = true;
                                PlayerManager.Instance.hornsIndex.PlayHornPlayback((FMOD_HornsIndex.HornType)hornID, mpct, 2);
                            }
                        }
                    }
                    break;
                default:
                    TKManager.LogMessage("Custom message of type: " + dataParts[0] + " not implemented by Teamkist");
                    break;
            }
        }
    }
}
