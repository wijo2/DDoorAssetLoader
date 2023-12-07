using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using BepInEx;
using UnityEngine.SceneManagement;
using BepInEx.Logging;

namespace DDoorAssetLoader
{
    [BepInPlugin(pluginGuid, plugiName, pluginVersion)]
    public class DDoorAssetLoader : BaseUnityPlugin
    {
        const string plugiName = "DDoorAssetLoader";
        const string pluginGuid = "ddoor.assetLoader.wijo";
        const string pluginVersion = "1.0";

        private static Dictionary<LoadableAsset, List<CallbackMethodDeligate>> callbacks = new Dictionary<LoadableAsset, List<CallbackMethodDeligate>>(); //loadable asset -> callbacks that need to be called
        private static Dictionary<string, List<string>> AssetsToLoadByMap = new Dictionary<string, List<string>>(); //map -> assets to load on that map
        public static Dictionary<LoadableAsset, GameObject> loadedAssets = new Dictionary<LoadableAsset, GameObject>(); //loadable asset -> copy of that asset
        
        public delegate void CallbackMethodDeligate(GameObject asset, LoadableAsset source);

        public static List<LoadableAsset> failedAssets = new List<LoadableAsset>();

        public static bool isLoadingDone { get; private set; }
        private static bool hasStarted = false; //used in StartMod to only start once for obvious reasons

        internal static ManualLogSource Log;

        internal static List<string> scenesInGame = new List<string>();

        private void Awake()
        {
            SceneManager.sceneLoaded += SceneLoaded;
            isLoadingDone = false;
            Log = base.Logger;

            for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                int lastSlash = scenePath.LastIndexOf("/");
                scenesInGame.Add(scenePath.Substring(lastSlash + 1, scenePath.LastIndexOf(".") - lastSlash - 1));
            }

            new Harmony(pluginGuid).PatchAll(typeof(DDoorAssetLoader));
        }

        private void Start()
        {
            if (AssetsToLoadByMap.Keys.Count() == 0)
            {
                hasStarted = true;
                isLoadingDone = true;
                SceneManager.sceneLoaded -= SceneLoaded; 
                return;
            }
            LoadingScreen.totalMaps = AssetsToLoadByMap.Keys.Count();
            LoadingScreen.mapsDone = 0;
        }

        private void OnGUI()
        {
            if (!isLoadingDone)
            {
                LoadingScreen.OnGUI();
            }
        }

        /// <summary>
        /// Add a single asset to be loaded. Callback will be called when it is found.
        /// </summary>
        public static void AddAsset(LoadableAsset asset, CallbackMethodDeligate callback = null)
        {
            //Log.LogWarning("try add asset");
            if (asset.IsNull())
            {
                Log.LogWarning("LoadableAsset had null values, will be discarded");
                return;
            }

            if (callback != null)
            {
                if (callbacks.ContainsKey(asset))
                {
                    callbacks[asset].Add(callback);
                }
                else 
                {
                    callbacks[asset] = new List<CallbackMethodDeligate>() { callback };
                }
            }

            if (AssetsToLoadByMap.ContainsKey(asset.scene))
            {
                if (!AssetsToLoadByMap[asset.scene].Contains(asset.path))
                {
                    AssetsToLoadByMap[asset.scene].Add(asset.path);
                }
            }
            else 
            {
                AssetsToLoadByMap[asset.scene] = new List<string>() { asset.path };
            }
        }

        /// <summary>
        /// Add multiple assets to be loaded at once. Callback will be called on every asset loaded.
        /// </summary>
        public static void AddAsset(LoadableAsset[] assets, CallbackMethodDeligate callback = null)
        {
            foreach (var asset in assets)
            {
                AddAsset(asset, callback);
            }
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            //Log.LogWarning("scene loaded " + scene.name);
            if (scene.name == "_GAMEMANAGER" || scene.name == "_PLAYER" || scene.name == "TitleScreen") { return; }
            if (!AssetsToLoadByMap.ContainsKey(scene.name)) 
            { 
                LoadNextMap(scene.name);
                return; 
            }
            var assetList = AssetsToLoadByMap[scene.name];
            
            foreach (var path in assetList)
            {
                LoadableAsset asset = new LoadableAsset(scene.name, path);
                GameObject foundObject = FindObjectByPath(path);
                if (foundObject == null)
                {
                    failedAssets.Add(asset);
                    continue;
                }
                foundObject.SetActive(false);
                foundObject.transform.parent = null;
                ((StayAliveForever)foundObject.AddComponent(typeof(StayAliveForever))).Init();

                loadedAssets.Add(asset, foundObject);

                if (callbacks.ContainsKey(asset))
                {
                    foreach (var call in callbacks[asset])
                    {
                        call.Invoke(foundObject, asset);
                    }
                }
            }
            LoadNextMap(scene.name);
        }

        private static GameObject FindObjectByPath(string path)
        {
            //Log.LogWarning("SEARCH IS ON!! " + path);
            var pathList = path.Split("/"[0]);
            pathList = pathList.Reverse().ToArray();
            var objName = pathList[0];
            foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                //Log.LogWarning(objName + ", " + obj.name);
                if (obj.name != objName) { continue; }
                var loopTrans = obj.transform.parent;
                var found = true; // used to break loop if possible
                var count = 0; // used to find corresponding name of path
                while (loopTrans != null) 
                {
                    count++;
                    //Log.LogWarning(loopTrans.gameObject.name + ", " + pathList[count]);
                    if (loopTrans.gameObject.name != pathList[count])
                    {
                        found = false;
                        break;
                    }
                    loopTrans = loopTrans.parent;
                }
                if (found)
                {
                    //Log.LogWarning("found!");
                    return obj;
                }
            }
            return null;
        }

        private static void LoadNextMap(string currentScene)
        {
            //Log.LogWarning("loadNextMap " + AssetsToLoadByMap.Keys.Count().ToString());
            //Log.LogWarning("remove? " + currentScene);
            if (AssetsToLoadByMap.ContainsKey(currentScene))
            {
                AssetsToLoadByMap.Remove(currentScene);
                //Log.LogWarning("tried remove! len: " + AssetsToLoadByMap.Keys.Count().ToString());
            }
            if (AssetsToLoadByMap.Count() == 0) 
            { 
                SceneManager.sceneLoaded -= SceneLoaded; 
                GameSceneManager.ReturnToTitle(); 
                AssetsToLoadByMap.Clear();
                callbacks.Clear();
                isLoadingDone = true;
                Time.timeScale = 1f;
                return;
            }
            //Log.LogWarning(AssetsToLoadByMap.Keys.Last().ToString());
            LoadingScreen.mapsDone++;
            GameSceneManager.LoadScene(AssetsToLoadByMap.Keys.First(), false);
        }

        [HarmonyPatch(typeof(ScreenFade), "UnLockFade")]
        [HarmonyPostfix]
        private static void StartMod()
        {
            if (!hasStarted)
            {
                hasStarted = true;
                Time.timeScale = 0;
                LoadNextMap("TitleScreen");
                return;
            }
        }

        [HarmonyPatch(typeof(GameSceneManager), "OnSceneLoaded")]
        [HarmonyPrefix]
        private static bool DisableStartScene()
        {
            return isLoadingDone;
        }
    }

    //don't laugh at me there! I thought since this could be plugin used by many mods for god
    //for god knows what and it could need to load a lot of objects I should make the basic 
    //element to iterate through as performent at that as I know how to achieve c:
    //(though for all I know all of these are useless but oh well what's 0.2kb of filesize)
    /// <summary> 
    /// Describes an asset.
    /// </summary>
    public struct LoadableAsset : IEquatable<LoadableAsset>
    {
        public string scene;
        public string path;

        /// <summary> 
        /// path is given in this format "root/child1/.../parent/object"
        /// </summary>
        public LoadableAsset(string scene, string path)
        {
            if (!DDoorAssetLoader.scenesInGame.Contains(scene))
            {
                throw new ArgumentException("Invalid scene");
            }
            this.scene = scene;
            this.path = path;
        }

        public bool IsNull()
        {
            return scene == null || path == null;
        }

        public static bool operator ==(LoadableAsset a1, LoadableAsset a2)
        {
            return a1.Equals(a2);
        }

        public static bool operator !=(LoadableAsset a1, LoadableAsset a2)
        {
            return !a1.Equals(a2);
        }

        public bool Equals(LoadableAsset a)
        {
            return a.scene == scene && a.path == path;
        }

        public override bool Equals(object obj)
        {
            if (obj is null || !(obj is LoadableAsset))
            {
                return false;
            }

            return Equals((LoadableAsset)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return scene.GetHashCode() * path.GetHashCode();
            }
        }
    }
}
