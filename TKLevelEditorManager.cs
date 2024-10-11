using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using Lidgren.Network;
using System.Linq;
using System.Globalization;
using System.Collections;

namespace TeamkistPlugin
{
    public static class TKLevelEditorManager
    {
        //A change occured on one of the blocks in the level editor. Called through the control z system and the something changed function.
        //Function is only called when in teamkist editor mode.
        public static void ChangeOccured(Change_Collection collection, bool changeWasUndo)
        {
            foreach (Change_Single cs in collection.changeList)
            {
                switch (collection.changeType)
                {
                    case Change_Collection.ChangeType.block:

                        if(!changeWasUndo)
                        {
                            //Block Creation
                            if(cs.before == null)
                            {
                                TKMessageRouter.BlockCreated(cs.after, false);
                            }
                            //Block Destruction
                            else if(cs.after == null)
                            {
                                TKMessageRouter.BlockDestroyed(cs.GetUID(), false);
                            }
                            //Block Update
                            else
                            {
                                //TKMessageRouter.BlockUpdated(cs.GetUID(), TKUtilities.PropertyListToString(LEV_UndoRedo.GetJSONblock(cs.after).properties), false);
                                TKMessageRouter.BlockUpdated(cs.GetUID(), TKUtilities.FixedPropertyListToString(cs.after), false);
                            }
                        }
                        else
                        {
                            //Block Creation
                            if (cs.after == null)
                            {
                                TKMessageRouter.BlockCreated(cs.before, false);
                            }
                            //Block Destruction
                            else if (cs.before == null)
                            {
                                TKMessageRouter.BlockDestroyed(cs.GetUID(), false);
                            }
                            //Block Update
                            else
                            {
                                //TKMessageRouter.BlockUpdated(cs.GetUID(), TKUtilities.PropertyListToString(LEV_UndoRedo.GetJSONblock(cs.before).properties), false);
                                TKMessageRouter.BlockUpdated(cs.GetUID(), TKUtilities.FixedPropertyListToString(cs.before), false);
                            }
                        }                        
                        break;
                    //Floor change
                    case Change_Collection.ChangeType.floor:
                        TKMessageRouter.FloorUpdated(changeWasUndo ? cs.int_before : cs.int_after, false);
                        break;
                    //Skybox change
                    case Change_Collection.ChangeType.skybox:
                        TKMessageRouter.SkyboxUpdated(changeWasUndo ? cs.int_before : cs.int_after, false);
                        break;
                }
            }

            //This can be improved, maybe by having a message time interval. Currently all messages are just being send immediately after creation.
            TKMessageRouter.ReleaseSendBuffer();
        }
               
        //Create a block in the level editor based on a BlockPropertyJSON class.
        public static void CreateBlock(BlockPropertyJSON blockPropertyJSON)
        {
            TKManager.central.undoRedo.GenerateNewBlock(blockPropertyJSON, blockPropertyJSON.UID);
            TKManager.central.validation.RecalcBlocksAndDraw(false);
            TKManager.LogMessage("Creating new block in level editor!");
        }

        //Overloaded method that excepts a BlockPropertyJSON in string format.
        //Is called from the Message Router.
        public static void CreateBlock(string blockJSON)
        {
            BlockPropertyJSON blockPropertyJSON = LEV_UndoRedo.GetJSONblock(blockJSON);
            CreateBlock(blockPropertyJSON);
        }

        //Destroy a block in the level editor based on UI.
        public static void DestroyBlock(string UID)
        {
            BlockProperties blockProperties = TKManager.central.undoRedo.TryGetBlockFromAllBlocks(UID);

            if (blockProperties == null)
            {
                TKManager.LogWarning("Trying to destroy block that doesn't exist! UID: " + UID);
                return;
            }
            else
            {
                TKManager.central.undoRedo.allBlocksDictionary.Remove(UID);
                GameObject.Destroy(blockProperties.gameObject);
                TKManager.central.validation.RecalcBlocksAndDraw(false);
                TKManager.LogMessage("Destroyed block in level editor!");

                RemoteChangeOccured(UID);
            }
        }

        //Update a block in the level editor.
        public static void UpdateBlock(string UID, string properties)
        {
            BlockProperties blockProperties = TKManager.central.undoRedo.TryGetBlockFromAllBlocks(UID);

            if(blockProperties == null)
            {
                TKManager.LogWarning("Trying to update block that doesn't exist! UID: " + UID);
                return;
            }
            else
            {
                //This is how the control z system applies a change.
                //Get the block from the dictionary, convert it to a JSON block.
                //Destroy the original, then apply the properties to the JSON block.
                //Then recreate it as a new block.
                TKManager.central.undoRedo.allBlocksDictionary.Remove(UID);
                BlockPropertyJSON blockPropertyJSON = blockProperties.ConvertBlockToJSON_v15();
                GameObject.Destroy(blockProperties.gameObject);
                TKUtilities.AssignPropertyListToBlockPropertyJSON(properties, blockPropertyJSON);
                TKManager.central.undoRedo.GenerateNewBlock(blockPropertyJSON, blockPropertyJSON.UID);
                TKManager.LogMessage("Updated block in level editor!");

                RemoteChangeOccured(UID);
            }
        }

        //Update the floor in the level editor from a value.
        public static void UpdateFloor(int floor)
        {
            TKManager.central.painter.SetLoadGroundMaterial(floor);
            TKManager.LogMessage("Updated floor in level editor to " + floor + "!");
        }

        //Update the skybox in the level editor from a value.
        public static void UpdateSkybox(int skybox)
        {
            TKManager.central.skybox.SetToSkybox(skybox, true);
            TKManager.LogMessage("Updated skybox in level editor to " + skybox + "!");
        }

        //This is called when loading the level editor in teamkist mode.
        //This has to be a coroutine because we have to wait a frame for all the level editor scripts to be initialized.
        //Specifically the ground material.
        public static IEnumerator LoadFromStorage()
        {
            yield return new WaitForEndOfFrame();
            UpdateSkybox(TKStorage.skyboxID);
            UpdateFloor(TKStorage.floorID);            

            foreach(KeyValuePair<string, BlockPropertyJSON> block in TKStorage.levelEditorBlocks)
            {
                CreateBlock(block.Value);
            }

            TKManager.central.validation.RecalcBlocksAndDraw(false);
        }

        public static void RemoteChangeOccured(string UID)
        {
            if (TKManager.InLevelEditor())
            {
                //Check if we are in the editor
                if (TKManager.central != null)
                {
                    //Are we holding anything ?
                    if (TKManager.central.selection.list.Count > 0)
                    {
                        //Validate the selection
                        // List to keep track of destroyed block indexes
                        List<int> updatedIndexes = new List<int>();

                        for (int i = 0; i < TKManager.central.selection.list.Count; i++)
                        {
                            BlockProperties bp = TKManager.central.selection.list[i];

                            if (bp == null || bp.UID == UID)
                            {
                                updatedIndexes.Add(i);
                            }
                        }

                        if (updatedIndexes.Count > 0)
                        {
                            // Remove destroyed indexes from the list after processing
                            for (int i = updatedIndexes.Count - 1; i >= 0; i--)
                            {
                                int indexToRemove = updatedIndexes[i];

                                // Safely remove the item at the specified index
                                if (indexToRemove >= 0 && indexToRemove < TKManager.central.selection.list.Count)
                                {
                                    TKManager.central.selection.list.RemoveAt(indexToRemove);
                                }
                            }

                            PlayerManager.Instance.messenger.Log("Simultaneous editing not supported! Deselecting all... Test track/leave and return to resync...", 3f);

                            TKManager.central.selection.DeselectAllBlocks(true, "Teamkist");
                        }
                    }
                }
            }
        }
    }
}