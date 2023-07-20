using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using Lidgren.Network;
using System.Linq;
using System.Globalization;
using TMPro;

namespace TeamkistPlugin
{
    //Handles all UI for the plugin. 
    //Creates a button and handler in the main menu.
    //Removes functionality from the load button in the level editor, as this is not allowed.
    public static class TKUI
    {
        //This function will split the regular level editor button in to two buttons, one for regular and one for teamkist.
        public static void GenerateLevelEditorOnlineButton()
        {
            //Get the two current buttons.
            OpenUIOnStart mainmenu_canvas = GameObject.FindObjectOfType<OpenUIOnStart>();
            Transform levelEditorGUI = mainmenu_canvas.transform.Find("LevelEditorGUI").transform;
            RectTransform workshopButton = levelEditorGUI.Find("Workshop Button").GetComponent<RectTransform>();
            RectTransform levelEditorButton = levelEditorGUI.Find("Start Level Editor Button").GetComponent<RectTransform>();

            //Calculate the current spacing between the two buttons.
            float buttonSpacing = Mathf.Abs(workshopButton.anchorMax.x - levelEditorButton.anchorMin.x);
            float buttonHeight = Mathf.Abs(levelEditorButton.anchorMax.y - levelEditorButton.anchorMin.y);

            //Create a copy of the level editor button.
            RectTransform editorOnlineButton = GameObject.Instantiate(levelEditorButton.transform, levelEditorButton.transform.parent).GetComponent<RectTransform>();

            //Make the level editor button half size with half spacing.
            levelEditorButton.anchorMin = new Vector2(levelEditorButton.anchorMin.x, levelEditorButton.anchorMin.y + (buttonHeight / 2) + (buttonSpacing / 2));

            //Set the new button on the other half
            editorOnlineButton.anchorMax = new Vector2(editorOnlineButton.anchorMax.x, editorOnlineButton.anchorMax.y - (buttonHeight / 2) - (buttonSpacing / 2));

            //Remove the listener of the new button.
            GenericButton editorOnlineGenericButton = editorOnlineButton.GetComponent<GenericButton>();
            editorOnlineGenericButton.normalColor = new Color(0, 0.547f, 0.82f, 1f);
            editorOnlineGenericButton.buttonImage.color = editorOnlineGenericButton.normalColor;
            editorOnlineGenericButton.onClick.RemoveAllListeners();
            for (int i = editorOnlineGenericButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                editorOnlineGenericButton.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }

            //Add a new listener
            editorOnlineGenericButton.onClick.AddListener(OnEditorOnlineButton);

            //Get the text component, remove the localizer and change the text
            TextMeshProUGUI buttonText = editorOnlineGenericButton.GetComponentInChildren<TextMeshProUGUI>();
            GameObject.Destroy(buttonText.GetComponent<I2.Loc.Localize>());
            buttonText.text = "Teamkist Editor";
        }

        //When the custom button is clicked.
        public static void OnEditorOnlineButton()
        {
            TKNetworkManager.ConnectToTheServer();
            PlayerManager.Instance.weLoadedLevelEditorFromMainMenu = true;
        }

        //Grey out the load button in the level editor while in teamkist mode, because loading a level will mess up the server.
        public static void DisableLoadButton()
        {
            LEV_CustomButton loadButton = TKManager.central.tool.button_load;
            loadButton.normalColor = Color.grey;
            loadButton.hoverColor = Color.grey;
            loadButton.clickColor = Color.grey;
            loadButton.overrideAllColor = true;
            loadButton.overrideNormalColor = true;
            loadButton.onClick.RemoveAllListeners();

            for (int i = loadButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                loadButton.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }
        }
    }
}
