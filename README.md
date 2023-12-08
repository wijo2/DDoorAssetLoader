# DDoorAssetLoader
The death's door asset loader is a mod made to make modding easier by allowing modders to easily gather any assets they might need from any scene in the game. The mod will load all the necessary scenes on game boot and provide the assets to other mods. 

## installation
1. Make sure you have bepinex installed. While installing it on your own should work, for some people it doesn't, be it user error or not, the easiest option is most likely to just install the debug mod through the first time installation process.
https://github.com/PrestonRooker/DDoorDebugMod
2. Go to the releases page linked below and download the appropriate dll. If you are unsure download 4.5.2 first, and it that doesn't work download the 4.8 version.
https://github.com/wijo2/DDoorAssetLoader/releases
3. Put the dll in game/Bepinex/plugins. (you can find the game folder through steam with right click -> manage -> browse local files.) With this done and the mod you are installing this for installed you can now boot the game.

## usage for devs
First you need to add the mod dll as a reference for your project. (you shouldn't need to worry about updating this dependency for new versions of this mod unless you need possible new features.)

The mod is used by calling AddAsset in your mod's Awake method and then giving it the asset(s) you want to load, and either also give a callback to retrieve those assets or find your asset in the public loadedAssets dictionary later. The callback is called immediately after finding the object so if you want to do something else in the scene or modify the asset within it you are free to do so.

Upon collecting the assets are set to inactive, their parent is set to null and they are given a tiny "StayAliveForever" component that makes them impervious to scene transitions. Since the mod only collects each asset once and all scripts that call for that same asset are given the same reference, it is recommended that you don't modify the asset directly, but instead Instantiate it first. If an asset are is unable to be loaded it will be put into the public failedAssets list.

Easy ways to find the scene names and object paths is to look at debug's warp menu (linked in installation) and RuntimeUnityEditor's inspect feature (likned below, it directly gives you a copyable path when inspecting something) respectively.
https://github.com/ManlyMarco/RuntimeUnityEditor

Here's an exmple of retrieving objects with the callback:
```csharp
private void Awake()
{
    DDoorAssetLoader.DDoorAssetLoader.AddAsset(new DDoorAssetLoader.LoadableAsset("boss_betty", "Icicle"), snow);
    DDoorAssetLoader.DDoorAssetLoader.AddAsset(new DDoorAssetLoader.LoadableAsset("lvl_Tutorial", "R_BOSS/_CONTENTS/LVL/BigTree2 (9)"), tree);
}

public void snow(GameObject asset, DDoorAssetLoader.LoadableAsset source)
{
    snowballPrefab = asset;
}

public void tree(GameObject asset, DDoorAssetLoader.LoadableAsset source)
{
    treePrefab = asset;
}
```

## contributing
The dependencies needed for building can be found in the Deps.zip folder in releases.<br/>
I'm very much still a beginner dev and I bet the code burns any more experienced dev's eyes so if you want to fix something you are more than welcome to throw a PR at me.
