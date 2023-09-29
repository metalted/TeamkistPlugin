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

    public class MultiplayerCharacter : MonoBehaviour
    {
        public SetupModelCar soapbox;
        public SetupModelCar cameraMan;
        public TextMeshPro displayName;
        public GameObject camera;
        public Transform armatureTop;
        public CharacterMode currentMode;

        public bool active;
        public float maxMoveDuration = 0.3f;
        public float maxRotateDuration = 0.2f;

        public Vector3 targetPosition = Vector3.zero;
        public Quaternion targetRotation = Quaternion.identity;
        public Quaternion targetArmatureRotation = Quaternion.identity;
        public Quaternion targetBodyRotation = Quaternion.identity;

        public enum CharacterMode { Build, Paint, Treegun, Read, Race };

        public void SetupCharacter(int hat, int zeepkist, int color)
        {
            Object_Soapbox wardrobe_soapbox = (Object_Soapbox)PlayerManager.Instance.objectsList.wardrobe.GetCosmetic(ZeepkistNetworking.CosmeticItemType.zeepkist, zeepkist, false);
            HatValues wardrobe_hat = (HatValues)PlayerManager.Instance.objectsList.wardrobe.GetCosmetic(ZeepkistNetworking.CosmeticItemType.hat, hat, false);
            CosmeticColor wardrobe_color = (CosmeticColor)PlayerManager.Instance.objectsList.wardrobe.GetCosmetic(ZeepkistNetworking.CosmeticItemType.skin, color, false);

            soapbox.DoCarSetup(wardrobe_soapbox, wardrobe_hat, wardrobe_color, false, false, true);
            cameraMan.DoCarSetup(null, wardrobe_hat, wardrobe_color, false, false, true);
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

        //Position
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            targetPosition = position;
        }

        public void AnimateToPosition(Vector3 target)
        {
            targetPosition = target;
        }

        //Rotations
        public void SetBodyRotation(float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            transform.rotation = rotation;
            targetBodyRotation = rotation;
        }

        public void AnimateToBodyRotation(float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, angle, 0);
            targetBodyRotation = rotation;
        }

        public void SetArmatureRotation(float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, 270, 180 - angle);
            armatureTop.localRotation = rotation;
            targetArmatureRotation = rotation;
        }

        public void AnimateToArmatureRotation(float angle)
        {
            Quaternion rotation = Quaternion.Euler(0, 270, 180 - angle);
            targetArmatureRotation = rotation;
        }

        public void SetRotation(Vector3 euler)
        {
            Quaternion rotation = Quaternion.Euler(euler);
            transform.rotation = rotation;
            targetRotation = rotation;
        }

        public void AnimateToRotation(Vector3 euler)
        {
            Quaternion rotation = Quaternion.Euler(euler);
            targetRotation = rotation;
        }

        public void Update()
        {
            if (active)
            {
                //Position
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

                switch(currentMode)
                {
                    case CharacterMode.Build:
                        //Armature Rotation
                        if (targetArmatureRotation != armatureTop.localRotation)
                        {
                            float angle = Quaternion.Angle(armatureTop.localRotation, targetArmatureRotation);
                            float rotateDuration = angle / maxRotateDuration;

                            armatureTop.localRotation = Quaternion.RotateTowards(armatureTop.localRotation, targetArmatureRotation, rotateDuration * Time.deltaTime);
                        }

                        //Body Rotation
                        if (targetBodyRotation != transform.rotation)
                        {
                            float angle = Quaternion.Angle(transform.rotation, targetBodyRotation);
                            float rotateDuration = angle / maxRotateDuration;

                            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetBodyRotation, rotateDuration * Time.deltaTime);
                        }
                        break;
                    case CharacterMode.Race:
                        //Soapbox
                        if (targetRotation != transform.rotation)
                        {
                            float angle = Quaternion.Angle(transform.rotation, targetRotation);
                            float rotateDuration = angle / maxRotateDuration;

                            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotateDuration * Time.deltaTime);
                        }
                        break;
                }
            }
        }

        public void UpdateTransform(Vector3 pos, Vector3 eul)
        {
            switch (currentMode)
            {
                case MultiplayerCharacter.CharacterMode.Build:
                    AnimateToPosition(pos);
                    AnimateToBodyRotation(eul.y);
                    AnimateToArmatureRotation(eul.x);
                    break;
                case MultiplayerCharacter.CharacterMode.Race:
                    AnimateToPosition(pos);
                    AnimateToRotation(eul);
                    break;
            }
        }
    }

    public static class TKPlayerManager
    {
        #region PlayerPrefab
        //The prefab that is created from the ghost model.
        public static MultiplayerCharacter playerPrefab;

        //Create the player prefab from the ghost model.
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
        #endregion

        #region LocalPlayer
        //Local
        public static Transform localRacer;
        public static Vector3 localPlayerPosition = Vector3.zero;
        public static Vector3 localPlayerEuler = Vector3.zero;
        public static Vector3 lastSendPosition = Vector3.zero;
        public static Vector3 lastSendEuler = Vector3.zero;
        public static MultiplayerCharacter.CharacterMode localCharacterMode = MultiplayerCharacter.CharacterMode.Build;

        //Time between transform updates.
        public static float positionUpdateInterval = 0.15f;
        private static float timer = 0f;

        //Get the data for the current user (hats, soapbox, color and name).
        public static TKPlayer GetLocalPlayerInformation()
        {
            TKPlayer local = new TKPlayer { ID = -1, state = 255 };

            try
            {
                local.name = PlayerManager.Instance.steamAchiever.GetPlayerName(false);
                local.hat = PlayerManager.Instance.avontuurHat.GetCompleteID();
                local.color = PlayerManager.Instance.avontuurColor.GetCompleteID();
                local.soapbox = PlayerManager.Instance.avontuurSoapbox.GetCompleteID();
                TKManager.LogMessage("Found user data: " + local.name + ", H: " + local.hat + ", C: " + local.color + ", S: " + local.soapbox);
            }
            catch(Exception e)
            {
                local.name = "Sphleeble";
                local.hat = 23000;
                local.color = 1000;
                local.soapbox = 1000;
                TKManager.LogMessage("Couldn't find user data!");
                TKManager.LogMessage(e.Message);
            }

            return local;
        }

        public static void Update()
        {
            bool inLevelEditor = TKManager.InLevelEditor();
            bool inGame = TKManager.InGameScene();

            if (!inLevelEditor && !inGame)
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
                if (localPlayerPosition != lastSendPosition || localPlayerEuler != lastSendEuler)
                {
                    TKNetworkManager.SendTransformData(localPlayerPosition, localPlayerEuler, localCharacterMode);
                    TKManager.LogMessage("Sending transform data to server!");
                }

                lastSendPosition = localPlayerPosition;
                lastSendEuler = localPlayerEuler;

                // Reset the timer
                timer = 0f;
            }
        }

        //The local player has gone to the editor.
        public static void OnLocalPlayerToEditor()
        {
            localCharacterMode = MultiplayerCharacter.CharacterMode.Build;
            TKNetworkManager.SendPlayerStateMessage(MultiplayerCharacter.CharacterMode.Build);
        }

        //The local player has gone to the game scene.
        public static void OnLocalPlayerToGame()
        {
            localCharacterMode = MultiplayerCharacter.CharacterMode.Race;
            TKNetworkManager.SendPlayerStateMessage(MultiplayerCharacter.CharacterMode.Race);
        }
        #endregion

        #region RemotePlayers

        //Remote player dictionary.
        public static Dictionary<int, MultiplayerCharacter> remotePlayers = new Dictionary<int, MultiplayerCharacter>();
        public static bool remotePlayersVisible = true;
        
        //Create a remote player under a ID and store it in the remote players dictionary.
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

            player.gameObject.SetActive(remotePlayersVisible);
            player.active = true;
        }

        //Received data about the players currenly in the server.
        public static void ProcessServerPlayerData(List<TKPlayer> playerData)
        {
            foreach (TKPlayer p in playerData)
            {
                InstantiateAndStorePlayer(p);
            }
        }

        //Called from the network manager when a remote player joined the game.
        public static void OnRemotePlayerJoined(TKPlayer player)
        {
            InstantiateAndStorePlayer(player);
            TKManager.LogMessage("Player joined: " + player.name);
        }

        //Called from the network manager when a remote player left the game.
        public static void OnRemotePlayerLeft(int ID)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                GameObject.Destroy(remotePlayers[ID].gameObject);
                remotePlayers.Remove(ID);
                TKManager.LogMessage("Player with ID: " + ID + " left the server!");
            }
        }

        //Called from the network manager when a remote player is going to the editor.
        public static void OnRemotePlayerToEditor(int ID)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                remotePlayers[ID].SetMode(MultiplayerCharacter.CharacterMode.Build);
                TKManager.LogMessage("Changed player state for player with: " + ID + " to editor!");
            }
        }

        //Called from the network manager when a remote player is going to the game scene.
        public static void OnRemotePlayerToGame(int ID)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                remotePlayers[ID].SetMode(MultiplayerCharacter.CharacterMode.Race);
                TKManager.LogMessage("Changed player state for player with: " + ID + " to game!");
            }
        }

        //Called from the network manager with transform data about a certain player.
        public static void ProcessRemotePlayerTransformData(int ID, Vector3 position, Vector3 euler, byte state)
        {
            if (remotePlayers.ContainsKey(ID))
            {
                remotePlayers[ID].UpdateTransform(position, euler);
                
                if(state == 1)
                {
                    remotePlayers[ID].SetMode(MultiplayerCharacter.CharacterMode.Race);
                }
                else
                {
                    remotePlayers[ID].SetMode(MultiplayerCharacter.CharacterMode.Build);
                }

                TKManager.LogMessage("Updated transform of player with ID: " + ID);
            }
            else
            {
                TKManager.LogError("Can't update transform of player because ID is not found. ID: " + ID);
            }
        }
        #endregion

        //Remove all data about remote players.
        public static void ClearSessionData()
        {
            foreach (KeyValuePair<int, MultiplayerCharacter> mc in remotePlayers)
            {
                if (mc.Value != null)
                {
                    GameObject.Destroy(mc.Value.gameObject);
                }
            }

            remotePlayers.Clear();
        }

        public static void HandleConfigUpdate()
        {
            bool cfg_showPlayers = (bool) TKConfig.config[TKConfig.preferencesTitle, TKConfig.preferences_showPlayers].BoxedValue;
            remotePlayersVisible = cfg_showPlayers;

            foreach(KeyValuePair<int, MultiplayerCharacter> mc in remotePlayers)
            {
                mc.Value.transform.gameObject.SetActive(remotePlayersVisible);
            }
        }
        
    }
}
