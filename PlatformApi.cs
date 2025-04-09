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
using static UnityEngine.InputSystem.Layouts.InputControlLayout;
using Object = UnityEngine.Object;

namespace PlatformApi
{
    [BepInPlugin("com.David_Loves_JellyCar_Worlds.PlatformApi", "PlatformApi", "1.1.0")]
    public class PlatformApi : BaseUnityPlugin
    {
        private static StickyRoundedRectangle platformPrefab;
        public static Object SlimeCamObject;
        public static Material PlatformMat;
        public static Logger logger = new Logger();
        public static AssetBundle MyAssetBundle;
        public static List<GameObject> PlatformList = new List<GameObject>();
        public static ScaleChanger scaleChanger;
        public static bool gameInProgress;
        public static GameSessionHandler GameSessionHandler2;
        public static MachoThrow2 throw2;
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
        }

        public void Start()
        {
            //get the platform prefab out of the Platform ability gameobject (david) DO NOT REMOVE!
            //chatgpt code to get the Platform ability object
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            Logger.LogInfo("getting platform object");
            GameObject PlatformAbility = null;
            var found = 0;
            var NumberOfObjectsToFind = 2;
            //finding difrent objects
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Platform")
                {
                    //store its reference
                    PlatformAbility = obj;
                    Logger.LogInfo("Found the Platform object");
                    found++;
                    if (found == NumberOfObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Throw")
                {
                    //store its reference
                    throw2 = obj.GetComponent<MachoThrow2>();
                    Logger.LogInfo("Found the MachoThrow2");
                    found++;
                    if (found == NumberOfObjectsToFind)
                    {
                        break;
                    }
                }
                ShootScaleChange[] ScaleChanges = Resources.FindObjectsOfTypeAll(typeof(ShootScaleChange)) as ShootScaleChange[];
                Debug.Log("getting platform object");
                foreach (ShootScaleChange ScaleChange in ScaleChanges)
                {
                    scaleChanger = ScaleChange.ScaleChangerPrefab;
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

        public void Update()
        {
            if (GameSessionHandler2)
            {
                gameInProgress = (bool)AccessTools.Field(typeof(GameSessionHandler), "gameInProgress").GetValue(GameSessionHandler2) && Updater.SimTimeSinceLevelLoaded > (Fix)2.5;
            }
            else
            {
                gameInProgress = false;
            }
        }
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            
            if (IsLevelName(scene.name))
            {
                Debug.Log("OnSceneLoaded");
                var platforms = new List<GameObject>();
                foreach (var platform in PlatformList)
                {
                    if (platform != null)
                    {
                        platforms.Add(platform);
                    }
                }
                PlatformList = platforms;
                //GetChild(int index);
                var level = GameObject.Find("Level");
                if (level == null)
                {
                    //this should fix the list not being created on some levels.
                    level = GameObject.Find("Level (1)");
                }
                if (level)
                {
                    //steal mat
                    var plat = level.transform.GetChild(0);
                    if (plat)
                    {
                        PlatformMat = plat.gameObject.GetComponent<SpriteRenderer>().material;
                    }
                    else
                    {
                        Logger.LogWarning("Couldnt Find Platfrom to steal Platform Mat from. this can happen if you remove all platforms on scene load. pls manualy steal a platfrom mat and set PlatformApi.PlatformMat to it.");
                        Debug.LogWarning("Couldnt Find Platfrom to steal Platform Mat from. this can happen if you remove all platforms on scene load. pls manualy steal a platfrom mat and set PlatformApi.PlatformMat to it.");
                    }
                    Logger.LogInfo("getting GameSessionHandler");
                    var PlayerList = GameObject.Find("PlayerList");
                    if (PlayerList == null)
                    {
                        PlayerList = GameObject.Find("PlayerList (1)");
                    }
                    GameSessionHandler2 = PlayerList.GetComponent<GameSessionHandler>();
                }
            }
        }
        public static bool IsLevelName(String input)
        {
            Regex regex = new Regex("Level[0-9]+", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }
        public static GameObject SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson, double MassPerArea = 0.05, Vector4[] color = null, PlatformType platformType = PlatformType.slime, bool UseSlimeCam = false, Sprite sprite = null, PathType pathType = PathType.None, double OrbitForce = 1, Vec2[] OrbitPath = null, double DelaySeconds = 1, double orbitSpeed = 100, double expandSpeed = 100, Vec2[] centerPoint = null, double normalSpeedFriction = 1, double DeadZoneDist = 1, double OrbitAccelerationMulitplier = 1, double targetRadius = 5, double ovalness01 = 1)
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
            body.up = new Vec2(rot % Fix.PiTimes2);
        }
        /// <summary>
        /// returns the rotatson of the platform in radiens.
        public static Fix GetRot(GameObject platform)
        {
            var body = platform.GetComponent<BoplBody>();
            return Vec2.NormalizedVectorAngle(body.up);
        }
        /// <summary>
        /// sets the rotatson of the home in radiens.
        public static void SetHomeRot(GameObject platform, Fix rot)
        {
            var home = platform.GetComponent<AnimateVelocity>();
            home.homeRotation = rot % Fix.PiTimes2;
        }
        /// <summary>
        /// returns the rotatson of the home in radiens.
        public static Fix GetHomeRot(GameObject platform)
        {
            var home = platform.GetComponent<AnimateVelocity>();
            return home.homeRotation;
        }
        /// <summary>
        /// sets mass per area. mass is calculated with the following formula. 10 + MassPerArea * PlatformArea. only works on platforms with ResizablePlatform. returns false if it fails
        public static bool SetMassPerArea(GameObject platform, Fix MassPerArea)
        {
            var platform2 = platform.GetComponent<ResizablePlatform>();
            if (platform2)
            {
                platform2.MassPerArea = MassPerArea;
                return true;
            }
            return false;
        }
        /// <summary>
        /// sets mass. only works on platforms WITHOUT ResizablePlatform. returns false if it fails.
        public static bool SetMass(GameObject platform, Fix Mass)
        {
            if (!platform.GetComponent<ResizablePlatform>())
            {
                BoplBody body = platform.GetComponent<BoplBody>();
                body.InverseMass = Fix.One / Mass;
                return true;
            }
            return false;
        }
        /// <summary>
        /// returns mass. works on all platform types.
        public static Fix GetMass(GameObject platform)
        {
            BoplBody body = platform.GetComponent<BoplBody>();
            return Fix.One / body.InverseMass;
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
        public static void AddAntiLockPlatform(GameObject platform, Fix OrbitForce, Vec2[] OrbitPath, Fix DelaySeconds)
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
        }
        public static AntiLockPlatform GetAntiLockPlatform(GameObject platform)
        {
            return platform.GetComponent<AntiLockPlatform>();
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
        public static VectorFieldPlatform GetVectorFieldPlatform(GameObject platform)
        {
            return platform.GetComponent<VectorFieldPlatform>();
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
        /// if the platform has ResizablePlatform on it, it will remove the platform by strinking it and making it disapear. retruns false if the gameobject doesnt have ResizablePlatform on it.
        public static bool RemovePlatformFancy(GameObject platform)
        {
            ResizablePlatform component = platform.GetComponent<ResizablePlatform>();
            if (component)
            {
                component.RemovePlatform();
                return true;
            }
            return false;
        }
        /// <summary>
        /// just deleates the platform.
        public static void RemovePlatform(GameObject platform)
        {
            Updater.DestroyFix(platform);
        }
        /// <summary>
        /// spawns a MatchoMan Boulder. note that most of the funcsons wont work on boulders. 
        public static Boulder SpawnBoulder(Vec2 Pos, Fix Scale, PlatformType platformType, Color color, Sprite sprite = null)
        {
            //get the boulder prefab
            Boulder boulderPrefab = (Boulder)AccessTools.Field(typeof(MachoThrow2), "boulderPrefab").GetValue(throw2);
            var boulder = FixTransform.InstantiateFixed<Boulder>(boulderPrefab, Pos);
            var dphysicsRoundedRect = boulder.hitbox;
            dphysicsRoundedRect.Scale = Scale;
            dphysicsRoundedRect.ManualInit();
            dphysicsRoundedRect.Scale = Scale;
            dphysicsRoundedRect.GetComponent<StickyRoundedRectangle>().platformType = platformType;
            SpriteRenderer component = dphysicsRoundedRect.GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                component.sprite = sprite;
            }
            else
            {
                component.sprite = throw2.boulders.sprites[(int)platformType].sprite;
            }
            component.color = color;
            return boulder;
        }
        /// <summary>
        /// returns the platforms home (where it trys to be).
        public static Vec2 GetHome(GameObject platform)
        {
            AnimateVelocity component = platform.GetComponent<AnimateVelocity>();
            return component.HomePosition;
        }
        /// <summary>
        /// sets the platforms home (where it trys to be).
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
        /// <summary>
        /// returns the platforms scale. returns 0 if the DPhysicsRoundedRect is disabled
        public static Fix GetScale(GameObject platform)
        {
            if (platform.GetComponent<DPhysicsRoundedRect>())
            {
                var physics = platform.GetComponent<DPhysicsRoundedRect>().pp.monobehaviourCollider;
                return physics.Scale;

            }
            return Fix.Zero;
        }
        /// <summary>
        /// sets the platforms scale. returns false if it fails
        public static bool SetScale(GameObject platform, Fix NewScale)
        {
            
            if (platform.GetComponent<DPhysicsRoundedRect>())
            {
                var physics = platform.GetComponent<DPhysicsRoundedRect>().pp.monobehaviourCollider;

                physics.Scale = NewScale;
                return true;
            }
            return false;
        }
        /// <summary>
        /// scales the platform smoothly. returns the resulting ScaleChanger object.
        public static ScaleChanger ScaleSmooth(GameObject platform, Fix multiplier)
        {
            ScaleChanger scaleChanger2 = FixTransform.InstantiateFixed<ScaleChanger>(scaleChanger, Vec2.zero);
            var physics = platform.GetComponent<DPhysicsRoundedRect>().pp.monobehaviourCollider;
            scaleChanger2.victim = physics;
            scaleChanger2.multiplier = multiplier;
            scaleChanger2.smallNonPlayersMultiplier = multiplier;
            return scaleChanger2;
        }
        /// <summary>
        /// returns the platforms area in bopl squrared units.
        public static Fix PlatformArea(GameObject platform)
        {
            return platform.GetComponent<DPhysicsRoundedRect>().PlatformArea();
        }
        /// <summary>
        /// returns a platforms area with the given prams in bopl squrared units.
        public static Fix PlatformArea(Fix Width, Fix Height, Fix Radius)
        {
            var DPhysicsRoundedRect2 = new DPhysicsRoundedRect();
            return DPhysicsRoundedRect2.PlatformArea(Width, Height, Radius);
        }
        /// <summary>
        /// shakes the platform.
        public void AddShake(GameObject platform , Fix duration, Fix shakeAmount, AnimationCurveFixed shakeCurve = null)
        {
            var shake = platform.GetComponent<ShakablePlatform>();
            shake.AddShake(duration, shakeAmount, 1, null, shakeCurve);
        }
        public static DPhysicsRoundedRect GetDPhysicsRoundedRect(GameObject platform)
        {
            return platform.GetComponent<DPhysicsRoundedRect>();
        }
        public static ShakablePlatform GetShakablePlatform(GameObject platform)
        {
            return platform.GetComponent<ShakablePlatform>();
        }
        public static StickyRoundedRectangle GetStickyRoundedRectangle(GameObject platform)
        {
            return platform.GetComponent<StickyRoundedRectangle>();
        }
        public static BoplBody GetBoplBody(GameObject platform)
        {
            return platform.GetComponent<BoplBody>();
        }
        public static AnimateVelocity GetAnimateVelocity(GameObject platform)
        {
            return platform.GetComponent<AnimateVelocity>();
        }
        public static FixTransform GetFixTransform(GameObject platform)
        {
            return platform.GetComponent<FixTransform>();
        }
        public static SpriteRenderer GetSpriteRenderer(GameObject platform)
        {
            return platform.GetComponent<SpriteRenderer>();
        }
        [HarmonyPatch(typeof(StickyRoundedRectangle))]
        public class Patches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            public static void Patch(StickyRoundedRectangle __instance)
            {
                if (__instance.gameObject.GetComponent<Boulder>() == null)
                {
                    PlatformList.Add(__instance.gameObject);
                } 
            }
        }


    }
}
