using BepInEx;
using BoplFixedMath;
using HarmonyLib;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PlatformApi
{
    [BepInPlugin("com.David_Loves_JellyCar_Worlds.PlatformApi", "PlatformApi", "1.0.0")]
    public class PlatformApi : BaseUnityPlugin
    {
        private static StickyRoundedRectangle platformPrefab;
        private static Object SlimeCamObject;
        public static Material PlatformMat;
        public static Logger logger = new Logger();
        public static AssetBundle MyAssetBundle;
        public static List<GameObject> PlatformList = new List<GameObject>();
        public enum PathType
        {
            None,
            AntiLockPlatform,
            VectorFieldPlatform
        }
        public void Awake()
        {
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("Awake");
            Harmony harmony = new Harmony("com.David_Loves_JellyCar_Worlds.PlatformApi");
            Logger.LogInfo("harmany created");
            harmony.PatchAll();
            Logger.LogInfo("PlatformApi Patch Compleate!");
            //get the platform prefab out of the Platform ability gameobject (david) DO NOT REMOVE!
            //chatgpt code to get the Platform ability object
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            Debug.Log("getting platform object");
            GameObject PlatformAbility = null;
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Platform")
                {
                    //store its reference
                    PlatformAbility = obj;
                    Debug.Log("Found the Platform object");
                    break;
                }
            }
            if (PlatformAbility)
            {
                var platformTransform = PlatformAbility.GetComponent(typeof(PlatformTransform)) as PlatformTransform;
                platformPrefab = platformTransform.platformPrefab;
            }
            //thanks almafa64 on discord for the path stuff.
            if (!MyAssetBundle)
            {
                MyAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/assetbundle2");
            }
            string[] assetNames = MyAssetBundle.GetAllAssetNames();
            foreach (string name in assetNames)
            {
                Debug.Log("asset name is: " + name);
            }
            //load the slime cam for use in spawning platforms with slimecam
            SlimeCamObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/slimetrailcam.prefab");
            MyAssetBundle.Unload(false);
        }
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            
            if (IsLevelName(scene.name))
            {
                Debug.Log("OnSceneLoaded");
                PlatformList = new List<GameObject>();
                //GetChild(int index);
                var level = GameObject.Find("Level");
                if (level)
                {
                    //platform list
                    var PlatformArray = FindObjectsOfType(typeof(ShakablePlatform));
                    foreach (var Platform in PlatformArray)
                    {
                        if (Platform != null)
                        {
                            var Shakeable = (ShakablePlatform)Platform;
                            PlatformList.Add(Shakeable.gameObject);
                        }
                    }
                    //steal mat
                    var plat = level.transform.GetChild(0);
                    if (plat)
                    {
                        PlatformMat = plat.gameObject.GetComponent<SpriteRenderer>().material;
                        //Debug.Log("mat is " + PlatformMat + " and its name is " + PlatformMat.name);
                    }
                    else
                    {
                        Logger.LogWarning("Couldnt Find Platfrom to steal Platform Mat from. this can happen if you remove all platforms on scene load. pls manualy steal a platfrom mat and set PlatformApi.PlatformMat to it.");
                        Debug.LogWarning("Couldnt Find Platfrom to steal Platform Mat from. this can happen if you remove all platforms on scene load. pls manualy steal a platfrom mat and set PlatformApi.PlatformMat to it.");
                    }
                }
            }
        }
        internal static bool IsLevelName(String input)
        {
            Regex regex = new Regex("Level[0-9]+", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }
        public static GameObject SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson, double MassPerArea = 0.05, Vector4[] color = null, PlatformType platformType = PlatformType.slime, bool UseSlimeCam = false, Sprite sprite = null, PathType pathType = PathType.None, double OrbitForce = 1, Vec2[] OrbitPath = null, double DelaySeconds = 1, bool isBird = false, double orbitSpeed = 100, double expandSpeed = 100, Vec2[] centerPoint = null, double normalSpeedFriction = 1, double DeadZoneDist = 1, double OrbitAccelerationMulitplier = 1, double targetRadius = 5, double ovalness01 = 1)
        {
            // Spawn platform (david - and now melon)
            var StickyRect = FixTransform.InstantiateFixed<StickyRoundedRectangle>(platformPrefab, new Vec2(X, Y));
            StickyRect.rr.Scale = Fix.One;
            var platform = StickyRect.GetComponent<ResizablePlatform>();
            platform.GetComponent<DPhysicsRoundedRect>().ManualInit();
            ResizePlatform(platform, Width, Height, Radius);
            //rotatson (in radiens)
            StickyRect.GetGroundBody().up = new Vec2(rotatson);
            //mass
            platform.MassPerArea = FloorToThousandnths(MassPerArea);
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            //TODO remove sprite object on scene change
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.material = PlatformMat;
                //set tag to ground so that blink doesnt cause problums
                var transform = StickyRect.transform;
                transform.gameObject.tag = "ground";

            }
            if (color != null)
            {
                spriteRenderer.color = color[0];
            }

            //PlatformType
            StickyRect.platformType = platformType;
            //slime cam
            if (UseSlimeCam)
            {
                spriteRenderer.material = PlatformMat;

                var transform = StickyRect.transform;
                UnityEngine.Object.Instantiate(SlimeCamObject, transform);
                transform.gameObject.tag = "ground";
            }

            var ShakeablePlatform = platform.GetComponent<ShakablePlatform>();
            AccessTools.Field(typeof(ShakablePlatform), "originalMaterial").SetValue(ShakeablePlatform, spriteRenderer.material);
            //moving platform
            if (pathType == PathType.AntiLockPlatform)
            {
                //antilock platform
                var AntiLockPlatformComp = platform.gameObject.AddComponent(typeof(AntiLockPlatform)) as AntiLockPlatform;
                AntiLockPlatformComp.OrbitForce = FloorToThousandnths(OrbitForce);
                AntiLockPlatformComp.OrbitPath = OrbitPath;
                AntiLockPlatformComp.DelaySeconds = FloorToThousandnths(DelaySeconds);
                AntiLockPlatformComp.isBird = isBird;
            }
            if (pathType == PathType.VectorFieldPlatform)
            {
                var centerPointReal = Vec2.zero;
                if (centerPoint != null)
                {
                    centerPointReal = centerPoint[0];
                }
                var VectorFieldPlatformComp = platform.gameObject.AddComponent(typeof(VectorFieldPlatform)) as VectorFieldPlatform;
                VectorFieldPlatformComp.centerPoint = centerPointReal;
                VectorFieldPlatformComp.DeadZoneDist = FloorToThousandnths(DeadZoneDist);
                VectorFieldPlatformComp.DelaySeconds = FloorToThousandnths(DelaySeconds);
                VectorFieldPlatformComp.expandSpeed = FloorToThousandnths(expandSpeed);
                VectorFieldPlatformComp.normalSpeedFriction = FloorToThousandnths(normalSpeedFriction);
                VectorFieldPlatformComp.OrbitAccelerationMulitplier = FloorToThousandnths(OrbitAccelerationMulitplier);
                VectorFieldPlatformComp.orbitSpeed = FloorToThousandnths(orbitSpeed);
                VectorFieldPlatformComp.ovalness01 = FloorToThousandnths(ovalness01);
                VectorFieldPlatformComp.targetRadius = FloorToThousandnths(targetRadius);
            }
            Debug.Log("Spawned platform at position (" + X + ", " + Y + ") with dimensions (" + Width + ", " + Height + ") and radius " + Radius);
            return StickyRect.transform.gameObject;
        }
        internal static Fix FloorToThousandnths(double value)
        {
            return Fix.Floor(((Fix)value) * (Fix)1000) / (Fix)1000;
        }
        internal static void ResizePlatform(ResizablePlatform platform, Fix newWidth, Fix newHeight, Fix newRadius)
        {
            platform.ResizePlatform(newHeight, newWidth, newRadius, true);
        }
        //this can be called anytime the object is active. this means you can have animated levels with shape changing platforms
        /// <summary>
        /// Resizes the platform. also recalculates the mass. can be called anytime. only works on platforms that are created with SpawnPlatform or the platform ability
        public static void ResizePlatform(GameObject platform, Fix newWidth, Fix newHeight, Fix newRadius)
        {
            var platform2 = platform.GetComponent<ResizablePlatform>();
            platform2.ResizePlatform(newHeight, newWidth, newRadius, true);
        }
        /// <summary>
        /// sets the rotatson of the platform to rot.
        /// </summary>
        /// <param name="rot">rotatson in radiens.</param>
        public static void SetRot(GameObject platform, Fix rot)
        {
            var body = platform.GetComponent<BoplBody>();
            body.up = new Vec2(rot);
        }
        /// <summary>
        /// sets mass per area. mass is calculated with the following formula. 10 + MassPerArea * PlatformArea
        public static void SetMassPerArea(GameObject platform, Fix MassPerArea)
        {
            var platform2 = platform.GetComponent<ResizablePlatform>();
            platform2.MassPerArea = MassPerArea;
        }
        /// <summary>
        /// sets the sprite of the platform. make sure your sprites pivet and size is set correctly. note that this replaces the material.
        public static void SetSprite(GameObject platform, Sprite sprite)
        {
            var StickyRect = platform.GetComponent<StickyRoundedRectangle>();
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            spriteRenderer.sprite = sprite;
            spriteRenderer.material = PlatformMat;
            var ShakeablePlatform = platform.GetComponent<ShakablePlatform>();
            AccessTools.Field(typeof(ShakablePlatform), "originalMaterial").SetValue(ShakeablePlatform, spriteRenderer.material);
        }
        /// <summary>
        /// sets the color of the platform.
        public static void SetColor(GameObject platform, Color color)
        {
            var StickyRect = platform.GetComponent<StickyRoundedRectangle>();
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            spriteRenderer.color = color;
            var transform = StickyRect.transform;
            transform.gameObject.tag = "ground";
        }
        /// <summary>
        /// sets the PlatformType of the platform. this affects matchoman boulders and drill colors. 
        public static void SetType(GameObject platform, PlatformType platformType)
        {
            var StickyRect = platform.GetComponent<StickyRoundedRectangle>();
            StickyRect.platformType = platformType;
        }
        /// <summary>
        /// add/replaces AntiLockPlatform. if there already is a AntiLockPlatform it replaces it. if theres a VectorFieldPlatform it removes it.
        /// </summary>
        /// <param name="OrbitForce">Force aplyed to the platform to make it move.</param>
        /// <param name="OrbitPath">Path of the platform.</param>
        /// <param name="DelaySeconds">DelaySeconds. note that it starts upon scene load. not upon player spawn.</param>
        /// <param name="isBird">if true, ignores sudden death. (no clue why this exsits.)</param>
        public static void AddAntiLockPlatform(GameObject platform, Fix OrbitForce, Vec2[] OrbitPath, Fix DelaySeconds, bool isBird = false)
        {

            AntiLockPlatform AntiLockPlatformComp;
            if (platform.GetComponent<VectorFieldPlatform>() != null)
            {
                var plat = platform.GetComponent<VectorFieldPlatform>();
                plat.IsDestroyed = true;
                Destroy(plat);
            }
            if (platform.GetComponent<AntiLockPlatform>() != null)
            {
                AntiLockPlatformComp = platform.GetComponent<AntiLockPlatform>();
            }
            else
            {
                AntiLockPlatformComp = platform.AddComponent(typeof(AntiLockPlatform)) as AntiLockPlatform;
            }
            AntiLockPlatformComp.OrbitForce = OrbitForce;
            AntiLockPlatformComp.OrbitPath = OrbitPath;
            AntiLockPlatformComp.DelaySeconds = DelaySeconds;
            AntiLockPlatformComp.isBird = isBird;
        }
        /// <summary>
        /// add/replaces VectorFieldPlatform. if there already is a VectorFieldPlatform it replaces it. if theres a AntiLockPlatform it removes it.
        /// </summary>
        /// <param name="orbitSpeed">Force aplyed to the platform to make it move.</param>
        /// <param name="centerPoint">the center point.</param>
        public static void AddVectorFieldPlatform(GameObject platform, Fix DelaySeconds, Fix orbitSpeed, Fix expandSpeed, Vec2 centerPoint, Fix normalSpeedFriction, Fix DeadZoneDist, Fix OrbitAccelerationMulitplier, Fix targetRadius, Fix ovalness01)
        {

            VectorFieldPlatform VectorFieldPlatformComp;
            if (platform.GetComponent<AntiLockPlatform>() != null)
            {
                var plat = platform.GetComponent<AntiLockPlatform>();
                plat.IsDestroyed = true;
                Destroy(plat);
            }
            if (platform.GetComponent<VectorFieldPlatform>() != null)
            {
                VectorFieldPlatformComp = platform.GetComponent<VectorFieldPlatform>();
            }
            else
            {
                VectorFieldPlatformComp = platform.AddComponent(typeof(VectorFieldPlatform)) as VectorFieldPlatform;
            }
            VectorFieldPlatformComp.centerPoint = centerPoint;
            VectorFieldPlatformComp.DeadZoneDist = DeadZoneDist;
            VectorFieldPlatformComp.DelaySeconds = DelaySeconds;
            VectorFieldPlatformComp.expandSpeed = expandSpeed;
            VectorFieldPlatformComp.normalSpeedFriction = normalSpeedFriction;
            VectorFieldPlatformComp.OrbitAccelerationMulitplier = OrbitAccelerationMulitplier;
            VectorFieldPlatformComp.orbitSpeed = orbitSpeed;
            VectorFieldPlatformComp.ovalness01 = ovalness01;
            VectorFieldPlatformComp.targetRadius = targetRadius;
        }
        /// <summary>
        /// sets the color of the platform.
        public static void SetMaterial(GameObject platform, Material material)
        {
            var StickyRect = platform.GetComponent<StickyRoundedRectangle>();
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            spriteRenderer.material = material;
            var ShakeablePlatform = platform.GetComponent<ShakablePlatform>();
            AccessTools.Field(typeof(ShakablePlatform), "originalMaterial").SetValue(ShakeablePlatform, spriteRenderer.material);
        }
        public static void AddForce(GameObject platform, Vec2 f, ForceMode2D forceMode = ForceMode2D.Force)
        {
            BoplBody body = platform.GetComponent<BoplBody>();
            body.AddForce(f, forceMode);
        }
        public static void AddForceAtPosition(GameObject platform, Vec2 f, Vec2 pos, ForceMode2D forceMode = ForceMode2D.Force)
        {
            BoplBody body = platform.GetComponent<BoplBody>();
            body.AddForceAtPosition(f, pos, forceMode);
        }
        /// <summary>
        /// sets the platforms home (where it trys to be)
        public static void SetHome(GameObject platform, Vec2 NewHome)
        {
            AnimateVelocity component = platform.GetComponent<AnimateVelocity>();
            component.HomePosition = NewHome;
        }
        public static Vec2 GetPos(GameObject platform)
        {
            return platform.GetComponent<BoplBody>().position;
        }
        public static void SetPos(GameObject platform, Vec2 NewPos)
        {
            platform.GetComponent<BoplBody>().position = NewPos;
        }
        [HarmonyPatch(typeof(ResizablePlatform))]
        public class Patches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            public static void Patch(ResizablePlatform __instance)
            {
                Debug.Log("platform awake");
                PlatformList.Add(__instance.gameObject);

            }
        }


    }
}
