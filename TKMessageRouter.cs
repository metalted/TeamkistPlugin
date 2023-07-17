using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TeamkistPlugin
{
    //The message router will apply changes to the world, depending on if we are in the editor or not.
    public static class TKMessageRouter
    {
        public static List<TKMessage> sendBuffer = new List<TKMessage>();

        public static void BlockCreated(string blockJSON, bool serverMessage)
        {
            //Fix has to be applied because the tree gun doesnt assign the properties of the transform correctly to the property list.
            blockJSON = TKUtilities.FixMissingJSONProperties(blockJSON);

            TKStorage.StoreBlock(blockJSON);

            if (serverMessage)
            {
                if (TKManager.InLevelEditor())
                {
                    TKLevelEditorManager.CreateBlock(blockJSON);
                }
            }
            else
            {
                sendBuffer.Add(new TKMessage(){
                    messageType = TKMessageType.BlockCreateEvent,
                    blockJSON = blockJSON
                });                
            }
        }

        public static void BlockDestroyed(string UID, bool serverMessage)
        {
            TKStorage.RemoveBlock(UID);

            if (serverMessage)
            {
                if(TKManager.InLevelEditor())
                {
                    TKLevelEditorManager.DestroyBlock(UID);
                }                
            }
            else
            {
                sendBuffer.Add(new TKMessage()
                {
                    messageType = TKMessageType.BlockDestroyEvent,
                    UID = UID
                });
            }
        }

        public static void BlockUpdated(string UID, string properties, bool serverMessage)
        {
            TKStorage.UpdateBlock(UID, properties);

            if (serverMessage)
            {
                if(TKManager.InLevelEditor())
                {
                    TKLevelEditorManager.UpdateBlock(UID, properties);
                }                
            }
            else
            {
                sendBuffer.Add(new TKMessage()
                {
                    messageType = TKMessageType.BlockChangeEvent,
                    UID = UID,
                    properties = properties
                });
            }
        }

        public static void FloorUpdated(int floor, bool serverMessage)
        {
            TKStorage.StoreFloor(floor);

            if (serverMessage)
            {                
                if(TKManager.InLevelEditor())
                {
                    TKLevelEditorManager.UpdateFloor(floor);
                }                
            }
            else
            {
                sendBuffer.Add(new TKMessage()
                {
                    messageType = TKMessageType.EditorFloorEvent,
                    floor = floor
                });
            }
        }

        public static void SkyboxUpdated(int skybox, bool serverMessage)
        {
            TKStorage.StoreSkybox(skybox);

            if (serverMessage)
            {                
                if(TKManager.InLevelEditor())
                {
                    TKLevelEditorManager.UpdateSkybox(skybox);
                }                
            }
            else
            {
                sendBuffer.Add(new TKMessage()
                 {
                     messageType = TKMessageType.EditorSkyboxEvent,
                     skybox = skybox
                });
            }
        }

        public static void ReleaseSendBuffer()
        {
            if (sendBuffer.Count > 0)
            {
                TKManager.LogMessage("Sending messages: " + sendBuffer.Count);
                TKNetworkManager.SendChangesToServer(sendBuffer);
                sendBuffer.Clear();
            }
        }
    }
}
