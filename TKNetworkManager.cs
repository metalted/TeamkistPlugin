using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using Lidgren.Network;
using System.Linq;
using System.Globalization;
using System.Threading;

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
        EditorSkyboxEvent = 105
    }

    //A message that can hold any kind of information.
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
            if(client == null)
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
            //We are connected and not connecting anymore.
            isConnectedToServer = true;
            isConnecting = false;
            //Log messages.
            PlayerManager.Instance.messenger.Log("Connected to server", 1f);
            TKManager.LogMessage("Connected to server!");
            //Log in to the server. Logging in will introduce our data to the server, the server will return the world data to us.
            LogIn("Shpleeble", 1006, 1006, 1006, true);
        }

        //Log in with our data.
        public static void LogIn(string playerName, int hat, int color, int soapbox, bool requestServerData)
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

            //Create the log in message which holds our name and data.
            //Setting the final bool to true will cause the server to return the data of the world.
            //No use case for false yet.
            NetOutgoingMessage outgoingMessage = client.CreateMessage();
            outgoingMessage.Write((byte)TKMessageType.LogIn);
            outgoingMessage.Write(playerName);
            outgoingMessage.Write(hat);
            outgoingMessage.Write(color);
            outgoingMessage.Write(soapbox);
            outgoingMessage.Write(requestServerData);
            client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        public static void SendTransformData(Vector3 position, Vector3 euler)
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
            client.SendMessage(outgoingMessage, NetDeliveryMethod.UnreliableSequenced);
        }

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
                    outgoingMessage.Write((byte)1);
                    break;
            }

            client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered);
        }

        //Is called when the user presses the quit button in the level editor.
        //The scene transistion will cause this function to be called.
        public static void DisconnectFromServer()
        {
            if(client == null)
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
            client.Disconnect("");
        }

        //Called from the network when our status changes to DISCONNECTED.
        public static void OnDisconnectedFromServer()
        {
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
                                        hat = incomingMessage.ReadInt32(),
                                        color = incomingMessage.ReadInt32(),
                                        soapbox = incomingMessage.ReadInt32()
                                    };

                                    playerData.Add(player);
                                }
                                TKPlayerManager.ProcessServerPlayerData(playerData);
                                break;
                            case TKMessageType.JoinedPlayerData:
                                TKPlayer joinedPlayer = new TKPlayer
                                {
                                    ID = incomingMessage.ReadInt32(),
                                    state = incomingMessage.ReadByte(),
                                    name = incomingMessage.ReadString(),
                                    hat = incomingMessage.ReadInt32(),
                                    color = incomingMessage.ReadInt32(),
                                    soapbox = incomingMessage.ReadInt32()
                                };

                                TKPlayerManager.OnRemotePlayerJoined(joinedPlayer);
                                break;
                            case TKMessageType.PlayerLeft:
                                TKPlayerManager.OnRemotePlayerLeft(incomingMessage.ReadInt32());
                                break;
                            case TKMessageType.PlayerTransformData:
                                int ID = incomingMessage.ReadInt32();
                                Vector3 playerPosition = new Vector3(incomingMessage.ReadFloat(), incomingMessage.ReadFloat(), incomingMessage.ReadFloat());
                                Vector3 playerEuler = new Vector3(incomingMessage.ReadFloat(), incomingMessage.ReadFloat(), incomingMessage.ReadFloat());
                                TKPlayerManager.ProcessRemotePlayerTransformData(ID, playerPosition, playerEuler);
                                break;
                            case TKMessageType.PlayerStateData:
                                int stateID = incomingMessage.ReadInt32();
                                byte state = incomingMessage.ReadByte();
                                if(state == 1)
                                {
                                    TKPlayerManager.OnRemotePlayerToGame(stateID);
                                }
                                else
                                {
                                    TKPlayerManager.OnRemotePlayerToEditor(stateID);
                                }
                                break;
                        }
                        break;
                }
            }
        }
    }
}
