using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using HarmonyLib;
using System;
using System.Reflection;

namespace SwordMod
{
    [BepInPlugin("SwordsMachinesArmMod", "SM Sword Mod", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        AssetBundle SwordBundle;

        static Animator SWanimator;
        GameObject PCamera;
        GameObject Puncher;
        GameObject Swordprefab;
         public GameObject FistIconPanel;
        public static FistControl fc;
        bool armgiven;

        private void Awake()
        {
            SwordBundle = AssetBundle.LoadFromMemory(Resource1.sword);
            SwordBundle.LoadAllAssets();
            if (SwordBundle.Contains("arm3"))
            {
                Debug.Log("Loaded sword.bund");
            }
            Harmony harmony = new Harmony("SwordsMachinesArm");
            harmony.PatchAll(typeof(Plugin));
        }

        /*[HarmonyPatch(typeof(Punch), "CoinFlip")]
        [HarmonyPrefix]
        public static void CoinFlip(Punch __instance)
        {
            Plugin.SWanimator.SetTrigger("CoinFlip");
            Plugin.SWanimator.Play("Base Layer.SWThrowCoin");
        }


        [HarmonyPatch(typeof(FistControl), "UpdateFistIcon")]
        [HarmonyPostfix]
        public static void UpdateFistIconPost(FistControl __instance)
        {
        }*/


        private void Update()
        {
            if (!armgiven && SwordBundle)
            {
                GiveYellowArm();
                armgiven = true;
            }            
            
            if (SWanimator)
            {
                if (fc.shopping)
                {
                    SWanimator.SetBool("Shopping", true);
                }
                else
                {
                    SWanimator.SetBool("Shopping", false);
                }
                if(fc.shopping && MonoSingleton<InputManager>.Instance.InputSource.Fire1.IsPressed && MonoSingleton<InputManager>.Instance.InputSource.Fire1.WasPerformedThisFrame)
                {
                    SWanimator.Play("Base Layer.SWShopPress");
                }
               /* else
                {
                    SWanimator.SetBool("Press", False);
                }*/
            }

            if(!checkyellowfist())
            {
                armgiven = false;
            }
        }

        public bool checkyellowfist()
        {
            PCamera = GameObject.FindGameObjectWithTag("MainCamera");
            Puncher = PCamera.GetComponentInChildren<FistControl>().gameObject;
            if(PCamera && Puncher)
            for (int i = 0; i < Puncher.transform.childCount; i++)
            {
                if(Puncher.transform.GetChild(i).GetComponent<PunchTest>())
                {     
                        return true;
                }
            }
            return false;
        }

        public void GiveYellowArm()
        {
            PCamera = GameObject.FindGameObjectWithTag("MainCamera");
            Swordprefab = SwordBundle.LoadAsset<GameObject>("SwordArm");
            Puncher = PCamera.GetComponentInChildren<FistControl>().gameObject;
            GameObject Sprefab = Instantiate(Swordprefab, Puncher.transform);
            Sprefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            Sprefab.transform.localPosition = new Vector3(-0.38f, 0, 0);
            Sprefab.gameObject.layer = 13;
            SkinnedMeshRenderer[] meshrenderers = Sprefab.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer skin in meshrenderers)
            {
                skin.gameObject.layer = 13;
            }
            //i hate this
            //add every fucking componenet and set every fucking variable manually because bundles are a bitch
            //and they just cant fucking remember what scripts a prefab has for some fucking reason i guess

            Sprefab.AddComponent<PunchTest>();
            Sprefab.AddComponent<AudioSource>();
            PunchTest PuncherComp = Sprefab.GetComponent<PunchTest>();
            PuncherComp.nmov = Sprefab.GetComponentInParent<NewMovement>();
            PuncherComp.heldItem = Puncher.GetComponent<FistControl>().heldObject;
            PuncherComp.shud = MonoSingleton<StyleHUD>.Instance;
            PuncherComp.aud = Sprefab.GetComponent<AudioSource>();
            PuncherComp.throwPoint = Sprefab.gameObject.transform.GetChild(1);
            PuncherComp.animator = Sprefab.gameObject.transform.GetComponentInChildren<Animator>();
            PuncherComp.Sword = SwordBundle.LoadAsset<GameObject>("SwordProj");
            PuncherComp.cc = PCamera.GetComponent<CameraController>();
            PuncherComp.particleSystems = Sprefab.GetComponentsInChildren<ParticleSystemRenderer>();
            SWanimator = PuncherComp.animator;
            fc = Puncher.GetComponentInParent<FistControl>();
            PuncherComp.fc = fc.gameObject;
           
            //disable the sword, obvious reasons
            Sprefab.SetActive(false);

            //shout out to hakita/pitr for making these 2 specific variables not public
            List<GameObject> spawnedArms = GetInstanceField(typeof(FistControl), fc, "spawnedArms") as List<GameObject>;
            spawnedArms.Add(Sprefab);
            List<int> spawnedArmsnum = GetInstanceField(typeof(FistControl), fc, "spawnedArmNums") as List<int>;
            spawnedArmsnum.Add(3);
        }

        internal static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
    }
}
