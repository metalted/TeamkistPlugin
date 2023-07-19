using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Lidgren.Network;

namespace TeamkistPlugin
{
    public class TKPlayer
    {
        public int ID;
        public string name;
        public int hat;
        public int color;
        public int soapbox;
        public byte state;
    }

    public static class TKPlayerManager
    {
        public static MultiplayerCharacter playerPrefab;
        
        //Local
        public static Transform localRacer;
        public static Vector3 localPlayerPosition = Vector3.zero;
        public static Vector3 localPlayerEuler = Vector3.zero;
        public static Vector3 lastSendPosition = Vector3.zero;
        public static Vector3 lastSendEuler = Vector3.zero;

        public static float positionUpdateInterval = 0.25f;
        private static float timer = 0f;

        //Remote
        public static Dictionary<int, MultiplayerCharacter> remotePlayers = new Dictionary<int, MultiplayerCharacter>();

        public static void Update()
        {
            bool inLevelEditor = TKManager.InLevelEditor();
            bool inGame = TKManager.InGameScene();

            if(!inLevelEditor && !inGame)
            {
                return;
            }

            if (TKManager.InLevelEditor())
            {
                localPlayerPosition = TKManager.central.cam.cameraTransform.position;
                localPlayerEuler = TKManager.central.cam.cameraTransform.eulerAngles;
            }
            else if (TKManager.InGameScene())
            {
                if (localRacer != null)
                {
                    localPlayerPosition = localRacer.position;
                    localPlayerEuler = localRacer.eulerAngles;
                }
            }

            // Increment the timer
            timer += Time.deltaTime;

            // Check if the interval has passed
            if (timer >= positionUpdateInterval)
            {
                if(localPlayerPosition != lastSendPosition || localPlayerEuler != lastSendEuler)
                {
                    TKNetworkManager.SendTransformData(localPlayerPosition, localPlayerEuler);
                    TKManager.LogMessage("Sending transform data to server!");
                }

                lastSendPosition = localPlayerPosition;
                lastSendEuler = localPlayerEuler;

                // Reset the timer
                timer = 0f;
            }
        }

        public static void ClearRemoteData()
        {
            foreach(KeyValuePair<int, MultiplayerCharacter> mc in remotePlayers)
            {
                if(mc.Value != null)
                {
                    GameObject.Destroy(mc.Value.gameObject);
                }
            }

            remotePlayers.Clear();
        }

        public static void FindAndProcessPlayerModels()
        {
            if (playerPrefab != null) { return; }

            NetworkedGhostSpawner networkedGhostSpawner = GameObject.FindObjectOfType<NetworkedGhostSpawner>();
            NetworkedZeepkistGhost networkedZeepkistGhost = networkedGhostSpawner.zeepkistGhostPrefab;

            Transform cameraManOriginal = networkedZeepkistGhost.cameraManModel.transform;
            Transform soapboxOriginal = networkedZeepkistGhost.ghostModel.transform;
            Transform displayNameOriginal = networkedZeepkistGhost.nameDisplay.transform;

            playerPrefab = new GameObject("Multiplayer Character").AddComponent<MultiplayerCharacter>();

            GameObject.DontDestroyOnLoad(playerPrefab);

            //Create copies and assign them.
            playerPrefab.soapbox = GameObject.Instantiate(soapboxOriginal, playerPrefab.transform).GetComponent<SetupModelCar>();
            playerPrefab.cameraMan = GameObject.Instantiate(cameraManOriginal, playerPrefab.transform).GetComponent<SetupModelCar>();
            playerPrefab.displayName = GameObject.Instantiate(displayNameOriginal, playerPrefab.transform).GetComponent<TextMeshPro>();

            //Process the soapbox
            Ghost_AnimateWheel[] animateWheelScripts = playerPrefab.soapbox.transform.GetComponentsInChildren<Ghost_AnimateWheel>();
            foreach (Ghost_AnimateWheel gaw in animateWheelScripts)
            {
                GameObject.Destroy(gaw);
            }

            //Attach the left and right arm to the top of the armature
            Transform armatureTopSX = playerPrefab.soapbox.transform.Find("Character/Armature/Top");
            Transform leftArmSX = playerPrefab.soapbox.transform.Find("Character/Left Arm");
            Transform rightArmSX = playerPrefab.soapbox.transform.Find("Character/Right Arm");
            leftArmSX.parent = armatureTopSX;
            leftArmSX.localPosition = new Vector3(-0.25f, 0, 1.25f);
            leftArmSX.localEulerAngles = new Vector3(0, 240, 0);
            rightArmSX.parent = armatureTopSX;
            rightArmSX.localPosition = new Vector3(-0.25f, 0, -1.25f);
            rightArmSX.localEulerAngles = new Vector3(0, 120, 0);

            //Process the camera man
            playerPrefab.camera = playerPrefab.cameraMan.transform.Find("Character/Right Arm/Camera").gameObject;

            //Attach the left and right arm to the top of the armature
            playerPrefab.armatureTop = playerPrefab.cameraMan.transform.Find("Character/Armature/Top");
            Transform leftArm = playerPrefab.cameraMan.transform.Find("Character/Left Arm");
            Transform rightArm = playerPrefab.cameraMan.transform.Find("Character/Right Arm");
            leftArm.parent = playerPrefab.armatureTop;
            leftArm.localPosition = new Vector3(-0.25f, 0, 1.25f);
            leftArm.localEulerAngles = new Vector3(0, 240, 0);
            rightArm.parent = playerPrefab.armatureTop;
            rightArm.localPosition = new Vector3(-0.25f, 0, -1.25f);
            rightArm.localEulerAngles = new Vector3(0, 120, 0);

            //Disable the camera object.
            playerPrefab.camera.SetActive(false);

            //Process the display name
            DisplayPlayerName dpn = playerPrefab.displayName.transform.GetComponent<DisplayPlayerName>();
            GameObject.Destroy(dpn);
            Transform hoethouder = playerPrefab.displayName.transform.Find("hoethouder");
            GameObject.Destroy(hoethouder.gameObject);
            playerPrefab.displayName.transform.localScale = new Vector3(-1, 1, 1);

            playerPrefab.gameObject.SetActive(false);
        }

        public static void ProcessServerPlayerData(List<TKPlayer> playerData)
        {
            foreach(TKPlayer p in playerData)
            {
                InstantiateAndStorePlayer(p);                
            }
        }

        public static void InstantiateAndStorePlayer(TKPlayer playerData)
        {
            //Add them to the dictionary.
            if (remotePlayers.ContainsKey(playerData.ID))
            {
                TKManager.LogError("Can't add remote player because ID is not unique! ID: " + playerData.ID);
                return;
            }

            //Create a new multiplayer character.
            MultiplayerCharacter player = GameObject.Instantiate<MultiplayerCharacter>(playerPrefab);
            //Apply the dont destroy on load flag, we will destroy them manually when leaving multiplayer.
            GameObject.DontDestroyOnLoad(player.gameObject);

            //Initialize the character.
            player.SetupCharacter(playerData.hat, playerData.soapbox, playerData.color);
            player.SetDisplayName(playerData.name);

            switch (playerData.state)
            {
                case 0:
                    player.SetMode(MultiplayerCharacter.CharacterMode.Build);
                    break;
                case 1:
                    player.SetMode(MultiplayerCharacter.CharacterMode.Race);
                    break;
            }
            
            remotePlayers.Add(playerData.ID, player);

            player.gameObject.SetActive(true);
            player.active = true;
        }

        //Local changes
        public static void OnLocalPlayerToEditor()
        {
            TKNetworkManager.SendPlayerStateMessage(MultiplayerCharacter.CharacterMode.Build);
        }

        public static void OnLocalPlayerToGame()
        {
            TKNetworkManager.SendPlayerStateMessage(MultiplayerCharacter.CharacterMode.Race);
        }

        //Calls from remote players.
        public static void OnRemotePlayerJoined(TKPlayer player)
        {
            InstantiateAndStorePlayer(player);
            TKManager.LogMessage("Player joined: " + player.name);
        }

        public static void OnRemotePlayerLeft(int ID)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                GameObject.Destroy(remotePlayers[ID].gameObject);
                remotePlayers.Remove(ID);
                TKManager.LogMessage("Player with ID: " + ID + " left the server!");
            }
        }

        public static void OnRemotePlayerToEditor(int ID)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                remotePlayers[ID].SetMode(MultiplayerCharacter.CharacterMode.Build);
                TKManager.LogMessage("Changed player state for player with: " + ID + " to editor!");
            }
        }

        public static void OnRemotePlayerToGame(int ID)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                remotePlayers[ID].SetMode(MultiplayerCharacter.CharacterMode.Race);
                TKManager.LogMessage("Changed player state for player with: " + ID + " to game!");
            }
        }        

        public static void ProcessRemotePlayerTransformData(int ID, Vector3 position, Vector3 euler)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                MultiplayerCharacter mc = remotePlayers[ID];
                switch(mc.currentMode)
                {
                    case MultiplayerCharacter.CharacterMode.Build:
                        mc.AnimateToPosition(position);
                        mc.SetBodyRotation(euler.y);
                        mc.SetArmatureRotation(euler.x);
                        break;
                    case MultiplayerCharacter.CharacterMode.Race:
                        mc.AnimateToPosition(position);
                        mc.SetRotation(euler);
                        break;
                }

                TKManager.LogMessage("Updated transform of player with ID: " + ID);
            }
            else
            {
                TKManager.LogError("Can't update transform of player because ID is not found. ID: " + ID);
            }
        }
    }

    public class MultiplayerCharacter : MonoBehaviour
    {
        public SetupModelCar soapbox;
        public SetupModelCar cameraMan;
        public TextMeshPro displayName;
        public GameObject camera;
        public Transform armatureTop;
        public CharacterMode currentMode;

        public bool active;
        public float maxMoveDuration = 0.5f;

        public Vector3 targetPosition = Vector3.zero;

        public enum CharacterMode { Build, Paint, Treegun, Read, Race };

        public void SetupCharacter(int hat, int zeepkist, int color)
        {
            Object_Soapbox wardrobe_soapbox = (Object_Soapbox)PlayerManager.Instance.objectsList.wardrobe.GetCosmetic(ZeepkistNetworking.CosmeticItemType.zeepkist, zeepkist, false);
            HatValues wardrobe_hat = (HatValues)PlayerManager.Instance.objectsList.wardrobe.GetCosmetic(ZeepkistNetworking.CosmeticItemType.hat, hat, false);
            CosmeticColor wardrobe_color = (CosmeticColor)PlayerManager.Instance.objectsList.wardrobe.GetCosmetic(ZeepkistNetworking.CosmeticItemType.skin, color, false);

            soapbox.DoCarSetup(wardrobe_soapbox, wardrobe_hat, wardrobe_color, false);
            cameraMan.DoCarSetup(null, wardrobe_hat, wardrobe_color, false);
        }

        public void SetMode(CharacterMode mode)
        {
            switch (mode)
            {
                case CharacterMode.Build:
                case CharacterMode.Paint:
                case CharacterMode.Treegun:
                case CharacterMode.Read:
                    soapbox.gameObject.SetActive(false);
                    cameraMan.gameObject.SetActive(true);
                    currentMode = CharacterMode.Build;
                    break;
                case CharacterMode.Race:                    
                    cameraMan.gameObject.SetActive(false);
                    soapbox.gameObject.SetActive(true);
                    currentMode = CharacterMode.Race;
                    break;                
            }
        }        

        public void SetDisplayName(string name)
        {
            displayName.text = name;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            targetPosition = position;
        }

        public void AnimateToPosition(Vector3 target)
        {
            targetPosition = target;
        }

        public void SetBodyRotation(float angle)
        {
            transform.eulerAngles = new Vector3(0, angle, 0);
        }

        public void SetArmatureRotation(float angle)
        {
            armatureTop.localEulerAngles = new Vector3(0, 270, 180 - angle);
        }       

        public void SetRotation(Vector3 euler)
        {
            transform.eulerAngles = euler;
        }

        public void Update()
        {
            if (active)
            {
                if (targetPosition != transform.position)
                {
                    float distance = Vector3.Distance(transform.position, targetPosition);
                    float moveDuration = distance / maxMoveDuration;

                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveDuration * Time.deltaTime);
                }

                try
                {
                    displayName.transform.LookAt(Camera.main.transform.position);
                }
                catch { }
            }
        }
    }
}
