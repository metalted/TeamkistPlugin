using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace TeamkistPlugin
{
    //Main Manager of the Teamkist Plugin.
    public static class TKManager
    {
        //A reference to the central script, will always be null if not a teamkist editor.
        public static LEV_LevelEditorCentral central;
        //A reference to the game scene script, will always be null if not in a testmap in teamkist editor.
        public static SetupGame testGame;
        //Are we currently in teamkist editor mode.
        public static bool teamkistEditor = false;
        //The name of the scene we are currently in.
        private static string currentScene = "";

        //Called from the plugin to initialize.
        public static void OnInitialize(TeamkistPlugin plugin)
        {
            TKConfig.InitializeConfig(plugin.Config);
            TKNetworkManager.Initialize();
        }

        //When unity is shut down, make sure we disconnect from the server.
        public static void OnApplicationQuit()
        {
            if(TKNetworkManager.isConnectedToServer)
            {
                TKNetworkManager.DisconnectFromServer();
            }
        }

        //Called from the update function of the plugin.
        public static void Run()
        {
            if (currentScene == "3D_MainMenu" || teamkistEditor)
            {
                TKNetworkManager.Update();
                TKPlayerManager.Update();
            }
        }

        //Function is called when we actually entered the main menu.
        public static void OnMainMenu()
        {
            //Reload any settings in the config, pretty much only useful for first starting the game but it can't hurt to do it each time the main menu loads.
            TKConfig.ForceReload();
            //Generate the button to join the server.
            TKUI.GenerateLevelEditorOnlineButton();
            //As we are in the main menu we are not in the server.
            teamkistEditor = false;

            //Check if we are still connected, maybe an error occured. Make sure we are in the right state.
            if (TKNetworkManager.isConnectedToServer)
            {
                //Oopsiepoopsie ?
                try
                {
                    TKNetworkManager.client.Disconnect("");
                }
                catch { }

                TKNetworkManager.isConnectedToServer = false;
                TKNetworkManager.isConnecting = false;
            }


            //As we are no longer connected to the server, clear storage.
            TKStorage.ClearStorage();

            //We need the player manager to grab the objects.
            TKPlayerManager.FindAndProcessPlayerModels();
            //And clear previous session
            TKPlayerManager.ClearSessionData();
        }

        //If we are in the level editor in teamkist mode, central is assigned.
        public static bool InLevelEditor()
        {
            return central != null;
        }

        //Called when entering the level editor. (Only called when in teamkist editor mode
        //Is a replacement for the TestMap start function, to load our map back into the level editor when we return from testing.
        public static void OnLevelEditor(LEV_LevelEditorCentral instance)
        {
            central = instance;

            TeamkistPlugin.Instance.StartCoroutine(TKLevelEditorManager.LoadFromStorage());

            TKUI.DisableLoadButton();

            TKPlayerManager.OnLocalPlayerToEditor();

            if (!central.testMap.GlobalLevel.IsTestLevel)
            {
                return;
            }

            central.testMap.GlobalLevel.IsTestLevel = false;
            central.manager.unsavedContent = false;


            if (central.manager.weLoadedLevelEditorFromMainMenu)
            {
                return;
            }

            central.undoRedo.historyList = central.manager.tempUndoList;
        }

        //When in teamkist mode and in the game scene, the SetupGame will be assigned.
        public static bool InGameScene()
        {
            return testGame != null;
        }

        //Called when the game scene starts, only when the editor in teamkist mode, so only when the testmap starts.
        public static void OnGameScene(SetupGame instance)
        {
            testGame = instance;

            //The player is going in to game mode.
            TKPlayerManager.OnLocalPlayerToGame();
        }

        //Called from the network manager after log in. When logging in the user downloads the world data from the server.
        //After downloading is completed the world data is stored in the storage class. We are now ready to load the level editor.
        public static void OnServerDataImported()
        {
            teamkistEditor = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelEditor2");
        }

        //Scene change events (Always called).
        public static void OnSceneLoad(string loadedScene)
        {
            if (currentScene == "LevelEditor2" && loadedScene == "3D_MainMenu")
            {
                //We pressed quit from the level editor. Disconnect from the server if we are in teamkist mode.
                if (teamkistEditor)
                {
                    TKNetworkManager.DisconnectFromServer();
                }
            }

            currentScene = loadedScene;
        }

        //Logging message controlled by the config file. Should default to off.
        #region Logging
        public static void LogMessage(string msg)
        {
            if (TKConfig.logMessages)
            {
                Debug.Log(msg);
            }
        }

        public static void LogWarning(string msg)
        {
            if (TKConfig.logWarnings)
            {
                Debug.LogWarning(msg);
            }
        }

        public static void LogError(string msg)
        {
            if (TKConfig.logErrors)
            {
                Debug.LogError(msg);
            }
        }
        #endregion
    }
}
