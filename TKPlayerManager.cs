using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;

namespace TeamkistPlugin
{
    public static class TKPlayerManager
    {
        public static MultiplayerCharacter playerPrefab;

        public static void Update()
        {
            //Stuff
        }

        public static void FindAndProcessPlayerModels()
        {
            if(playerPrefab != null) { return; }

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
            foreach(Ghost_AnimateWheel gaw in animateWheelScripts)
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
    }

    public class MultiplayerCharacter : MonoBehaviour
    {
        public SetupModelCar soapbox;
        public SetupModelCar cameraMan;
        public TextMeshPro displayName;
        public GameObject camera;
        public Transform armatureTop;

        public bool active;
        public float moveSpeed = 20f;

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
                    break;
                case CharacterMode.Race:                    
                    cameraMan.gameObject.SetActive(false);
                    soapbox.gameObject.SetActive(true);
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
            armatureTop.localEulerAngles = new Vector3(0, 270, 180 + angle);
        }       
        
        public void Update()
        {
            if (active)
            {
                if(targetPosition != transform.position)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
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
