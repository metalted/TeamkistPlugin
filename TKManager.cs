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
            //Run the network manager, to read messages etc.
            TKNetworkManager.Update();
        }

        //If we are in the level editor in teamkist mode, central will be assigned.
        public static bool InLevelEditor()
        {
            return central != null;
        }

        //Called from the network manager after log in. When logging in the user downloads the world data from the server.
        //After downloading is completed the world data is stored in the storage class. We are now ready to load the level editor.
        public static void OnServerDataImported()
        {
            teamkistEditor = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene("LevelEditor2");
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

        //Scene load events (This is always called).
        public static void OnMainMenu()
        {
            TKConfig.ForceReload();
            TKUI.GenerateLevelEditorOnlineButton();
            teamkistEditor = false;

            //As we are no longer connected to the server, clear storage.
            TKStorage.ClearStorage();
        }

        //Called when entering the level editor. (Only called when in teamkist editor mode
        //Is a replacement for the TestMap start function, to load our map back into the level editor when we return from testing.
        public static void OnLevelEditor(LEV_LevelEditorCentral instance)
        {
            central = instance;

            TeamkistPlugin.Instance.StartCoroutine(TKLevelEditorManager.LoadFromStorage());

            TKUI.DisableLoadButton();

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

        //Scene change events (Always called).
        public static void OnSceneLoad(string loadedScene)
        {
            if(currentScene == "3D_MainMenu" && loadedScene == "LevelEditor2")
            {
                //OnMenuToLevelEditor();
            }
            else if(currentScene == "LevelEditor2" && loadedScene == "3D_MainMenu")
            {
                OnLevelEditorToMenu();
            }
            else if(currentScene == "LevelEditor2" && loadedScene == "GameScene")
            {
                //OnLevelEditorToGame();
            }
            else if(currentScene == "GameScene" && loadedScene == "LevelEditor2")
            {
                //OnGameToLevelEditor();
            }
            else if(currentScene == "GameScene" && loadedScene == "3D_MainMenu")
            {
                //OnGameToMenu();
            }

            currentScene = loadedScene;
        }

        //Called when the Menu is about to be loaded while we are in the Level Editor. (Always called).
        public static void OnLevelEditorToMenu()
        {
            if(teamkistEditor)
            {
                TKNetworkManager.DisconnectFromServer();
            }
        }
    }
}
