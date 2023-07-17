using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TeamkistPlugin
{
    //Pretty simple class, basically a live save file for the level editor.
    //When a change happens online it will be stored here.
    //That way we can just load from storage when returning from testing, instead of having to request the level over and over again.
    //This will save a lot of time when dealing with large levels.
    public static class TKStorage
    {
        //The dictionary that holds all the blocks in the editor.
        public static Dictionary<string, BlockPropertyJSON> levelEditorBlocks = new Dictionary<string, BlockPropertyJSON>();
        //Editor data.
        public static int skyboxID;
        public static int floorID;

        //Clear all data in storage.
        //Is called when importing level data from the server.
        //Should also be called when entering the main menu.
        public static void ClearStorage()
        {
            levelEditorBlocks.Clear();
            skyboxID = 0;
            floorID = -1;
        }

        //Store a block in storage.
        public static void StoreBlock(string blockJSON)
        {
            BlockPropertyJSON blockPropertyJSON = LEV_UndoRedo.GetJSONblock(blockJSON);
            if (!levelEditorBlocks.ContainsKey(blockPropertyJSON.UID))
            {
                levelEditorBlocks.Add(blockPropertyJSON.UID, blockPropertyJSON);
                TKManager.LogMessage("Stored block with UID: " + blockPropertyJSON.UID);
            }
            else
            {
                TKManager.LogError("Can't store block because UID is already in the dictionary. UID: " + blockPropertyJSON.UID);
            }
        }

        //Update a block in storage.
        public static void UpdateBlock(string UID, string properties)
        {
            if(!levelEditorBlocks.ContainsKey(UID))
            {
                TKManager.LogError("Can't update block because UID is not present in the dictionary. UID: " + UID);
                return;
            }

            TKUtilities.AssignPropertyListToBlockPropertyJSON(properties, levelEditorBlocks[UID]);
            TKManager.LogMessage("Updated block with UID: " + UID);
        }

        //Wipe a block from storage.
        public static void RemoveBlock(string UID)
        {
            if(!levelEditorBlocks.ContainsKey(UID))
            {
                TKManager.LogError("Can't remove block because UID is not present in the dictionary. UID: " + UID);
                return;
            }

            levelEditorBlocks.Remove(UID);
            TKManager.LogMessage("Removed block with UID: " + UID);
        }

        //Save the skybox.
        public static void StoreSkybox(int ID)
        {
            skyboxID = ID;
            TKManager.LogMessage("Updated Skybox to: " + ID);
        }

        //Save the floor.
        public static void StoreFloor(int ID)
        {
            floorID = ID;
            TKManager.LogMessage("Updated Floor to: " + ID);
        }

        //This function will store the data retreived from the server in storage.
        public static void ImportServerData(int floor, int skybox, List<string> blockData)
        {
            ClearStorage();
            floorID = floor;
            skyboxID = skybox;

            foreach(string s in blockData)
            {
                StoreBlock(s);
            }
        }
    }
}
