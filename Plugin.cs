using BepInEx;
using HarmonyLib;

namespace TeamkistPlugin
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class TeamkistPlugin : BaseUnityPlugin
    {
        public const string pluginGuid = "com.metalted.zeepkist.teamkistclient";
        public const string pluginName = "Teamkist Client";
        public const string pluginVersion = "1.6";

        public static TeamkistPlugin Instance;

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll();
            Logger.LogInfo($"Plugin Teamkist Client v{pluginVersion} is loaded!");

            //Initialize Teamkist Client
            TKManager.OnInitialize(this);
            //Set a reference to this script to access the plugin.
            Instance = this;
        }

        //Run the game loop on the Teamkist Manager.
        public void Update()
        {
            //We now run all the time but we should only run this when appropriate, which is in the main menu, level editor and test map.
            TKManager.Run();
        }

        public void OnApplicationQuit()
        {
            TKManager.OnApplicationQuit();
        }
    }    

    //Patch will call a function with the name of the scene that is going to be loaded.
    [HarmonyPatch(typeof(UnityEngine.SceneManagement.SceneManager), "LoadScene", new[] { typeof(string) })]
    public class SceneLoadPatch
    {
        public static void Prefix(ref string sceneName)
        {
            TKManager.OnSceneLoad(sceneName);
        }
    }

    //Called when we enter the main menu.
    [HarmonyPatch(typeof(MainMenuUI), "Awake")]
    public class TKMainMenuUIAwakePatch
    {
        public static void Prefix()
        {
            TKManager.OnMainMenu();
        }
    }

    //Called when we enter the level editor
    [HarmonyPatch(typeof(LEV_LevelEditorCentral), "Awake")]
    public class TKLevelEditorAwakePatch
    {
        public static void Postfix(LEV_LevelEditorCentral __instance)
        {
            if (TKManager.teamkistEditor)
            {
                TKManager.OnLevelEditor(__instance);
            }
        }
    }

    //Called when we enter a game scene.
    [HarmonyPatch(typeof(SetupGame), "Awake")]
    public class TKGameSceneAwakePatch
    {
        public static void Postfix(SetupGame __instance)
        {
            if (TKManager.teamkistEditor)
            {
                TKManager.OnGameScene(__instance);
            }
        }
    }

    //Called when the players are spawned.
    [HarmonyPatch(typeof(GameMaster), "SpawnPlayers")]
    public class TKGameMasterSpawnPlayersPatch
    {
        public static void Postfix(GameMaster __instance)
        {
            if (TKManager.teamkistEditor)
            {
                TKPlayerManager.localRacer = __instance.PlayersReady[0].transform;
            }
        }
    }

    //Called when a change is made on an object.
    [HarmonyPatch(typeof(LEV_UndoRedo), "SomethingChanged")]
    public class TKUndoRedoSomethingChangedPatch
    {
        public static void Postfix(ref Change_Collection whatChanged, ref string source)
        {
            if (TKManager.teamkistEditor)
            {
                TKLevelEditorManager.ChangeOccured(whatChanged, false);
            }
        }
    }

    //Called when a change is undone.
    [HarmonyPatch(typeof(LEV_UndoRedo), "ApplyBeforeState")]
    public class TKUndoRedoApplyBeforeStatePatch
    {
        public static void Postfix(LEV_UndoRedo __instance)
        {
            if (TKManager.teamkistEditor)
            {
                Change_Collection.ChangeType changeType = __instance.historyList[__instance.currentHistoryPosition].changeType;

                if (changeType != Change_Collection.ChangeType.selection)
                {
                    TKLevelEditorManager.ChangeOccured(__instance.historyList[__instance.currentHistoryPosition], true);
                }
            }
        }
    }

    //Called when a change is redone.
    [HarmonyPatch(typeof(LEV_UndoRedo), "ApplyAfterState")]
    public class TKUndoRedoApplyAfterStatePatch
    {
        public static void Postfix(LEV_UndoRedo __instance)
        {
            if (TKManager.teamkistEditor)
            {
                Change_Collection.ChangeType changeType = __instance.historyList[__instance.currentHistoryPosition].changeType;

                if (changeType != Change_Collection.ChangeType.selection)
                {
                    TKLevelEditorManager.ChangeOccured(__instance.historyList[__instance.currentHistoryPosition], false);
                }
            }
        }
    }

    //This patch will make sure Zeepkist doesnt load its own file when returning to the level editor from testing.
    //The level should always be loaded from the storage script.
    [HarmonyPatch(typeof(LEV_TestMap), "Start")]
    public class TKTestMapStartPatch
    {
        public static bool Prefix(LEV_TestMap __instance)
        {
            return !TKManager.teamkistEditor;
        }
    }
}
