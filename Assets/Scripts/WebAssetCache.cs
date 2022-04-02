using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

using BestHTTP;
using Newtonsoft.Json;
using WebP;

public class WebAssetCache : MonoBehaviour
{
    // This is the singleton instance of our web asset cache. We only ever want one cache, which the entire game can access.
    public static WebAssetCache Instance {get; private set;}

    #region JSON parsed data classes
    // This is the object containing meta information we parse from the JSON manifest.
    [System.Serializable]
    public class MetaInformation
    {
        public string Hash {get; set;}
    }

    // Object containing asset information parsed from the JSON manifest.
    [System.Serializable]
    public class Asset
    {
        public string Name {get; set;}
        public string Path {get; set;}
        public string Hash {get; set;}
    }

    // This is the object representing the parsed JSON manifest.
    [System.Serializable]
    public class AssetManifest
    {
        public MetaInformation Meta {get; set;}
        public Asset[] Assets {get; set;}
    }

    // This is the object representing the parsed JSON version file.
    [System.Serializable]
    public class VersionNumber
    {
        public string Version {get; set;}
    }
    #endregion

    public class LoadedImageAsset
    {
        // The Texture2D object created from the loaded image.
        public Texture2D texture {get; private set;}

        // We might want to create a shared Sprite object from the texture that multiple game objects can use.
        // It's unclear how Unity handles references to things like sprites, and whether or not this saves memory
        // or if there's a risk of the sprite being modified by different game objects.
        public Sprite sprite {get; private set;}

        // These attributes are from the images asset manifest entry and are kept for convenient access.
        public string name {get; private set;}

        public string hash {get; private set;}

        public string path {get; private set;}

        public LoadedImageAsset()
        {
            // Empty constructor intended to be used with the Load() method to initialize the object instance.
        }

        public LoadedImageAsset(string name, string path, string hash, Texture2D texture)
        {
            this.name = name;
            this.path = path;
            this.hash = hash;
            this.texture = texture;
        }

        // Save the image asset data to the cache.
        public void Save(string cacheDirectory)
        {
            // We should already have the asset's attributes saved in the manifest. Focus on saving the image itself.

            // The Texture2D class is not serializable by Unity, so we're just going to save the image as a png and load it back as a Texture2D later.
            if(texture)
            {
                string filePath = Path.Combine(cacheDirectory, path);
                byte[] pngTexture = texture.EncodeToPNG();

                File.WriteAllBytes(filePath, pngTexture);
            }
            else
            {
                Debug.LogError("ERROR: Attempted to save nonexistent Texture2D.");
            }
        }

        // Load the image asset data from the cache using the given cache directory and file path.
        public LoadedImageAsset Load(string cacheDirectory, string name, string path, string hash)
        {
            this.name = name;
            this.path = path;
            this.hash = hash;

            string filePath = Path.Combine(cacheDirectory, path);
            if(File.Exists(filePath))
            {
                byte[] pngTexture = File.ReadAllBytes(filePath);
                texture.LoadImage(pngTexture);

                return this;
            }
            else
            {
                Debug.LogErrorFormat("CACHE LOAD ERROR: File at path '{0}' does not exist.", filePath);
                return null;
            }
        }
    }

    // The currently loaded version information.
    private VersionNumber currentVersion;

    // The currently loaded asset manifest.
    private AssetManifest currentManifest;

    // This is the version information retrieved from the server.
    private VersionNumber serverVersion;

    // This is the asset manifest retrieved from the server.
    private AssetManifest serverManifest;

    // Our http requests we make to the server. We want the references at this scope so they can be aborted (if necessary)
    // anywhere in the script.
    private HTTPRequest versionRequest;
    private HTTPRequest manifestRequest;

    // We don't want our cached files to be mixed in with the rest of our game's persistent data, so this is the folder where we should dump it all.
    public string cacheDirectory;

    // The list of currently active http requests we're making to the server.
    private List<HTTPRequest> activeRequests = new List<HTTPRequest>();

    // INSERT HERE: Data structure(s) representing the in-memory cache of our web assets.
    public Dictionary<string, LoadedImageAsset> loadedAssets {get; private set;}

    void Awake()
    {
        // Set up our singleton instance of the class.
        if(Instance != null)
        {
            // If we already have an instance of this class, destroy this instance.
            Destroy(gameObject);
            return;
        }

        // If there is no instance of this class, set it and mark it so Unity doesn't destroy it between scene changes.
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // We're not allowed to call Application.persistentDataPath from a Monobehavior constructor (Unity's words, not mine) so we need to
        // set the path either here or in the Awake() function
        cacheDirectory = Path.Combine(Application.persistentDataPath, "WebCache/");

        // First we're going to do a brief cache integrity check. If our version and manifest files exist, load them in.
        // If not, set the current version to null (if it isn't already). A null version will tell our loader that there is
        // no cache and we should start from scratch.
        string versionFilePath = Path.Combine(cacheDirectory, "manifest_version.dat");
        if(File.Exists(versionFilePath))
        {
            LoadVersion();
            Debug.Log("Cached version data was found.");
        }
        else
        {
            // If no cached version was found then we can't check to make sure the cache is up to date (technically we can since the version data
            // exists in the manifest itself, but that's not the intended behavior sooo).
            Debug.Log("No cached version data was found.");
        }

        string manifestFilePath = Path.Combine(cacheDirectory, "manifest.dat");
        if(File.Exists(manifestFilePath))
        {
            LoadManifest();
            Debug.Log("Cached manifest data was found.");
        }
        else
        {
            // If there is no cached manifest for some reason (like maybe it was deleted) there is no point trying to load any files,
            // because we don't know any information about them. Set the version to null to let the loader know what to do.
            Debug.Log("No cached manifest data was found.");
            currentVersion = null;
        }

        // Perform an integrity check to make sure the cached assets exist and/or load them in and update the cache later as necessary.


        // To test our diff function(s) we're going to override the loaded version and manifest data with some test data we've prepared.
        // NOTE: This should not be used to download actual files, since many of them will not exist. Only test up to the point the download
        //      batch is prepared.
        /*
        string testVersion = File.ReadAllText(Path.Combine(Application.persistentDataPath, "TestFiles/", "test_version.json"));
        currentVersion = JsonConvert.DeserializeObject<VersionNumber>(testVersion);

        string testManifest = File.ReadAllText(Path.Combine(Application.persistentDataPath, "TestFiles/", "test_manifest.json"));
        currentManifest = JsonConvert.DeserializeObject<AssetManifest>(testManifest);
        */


        // Next we need to check the manifest version against the one we have on file.
        versionRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/version.json"), OnVersionRequestFinished);
        versionRequest.Send();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void VerifyCacheIntegrity()
    {

    }

    // Method handling updating the cache with new data from the server.
    private void UpdateCache()
    {
        Debug.Log("Beginning cache update.");
        // If we don't have a cached manifest then we just go ahead and download everything from the server.
        // NOTE: May need to delete the cache, just in case we have a bunch of garbage data.

        // If we have a local manifest as well as a server manifest then we need to find:
        // 1) Assets that exist on the server but not on the local machine, and thus need to be downloaded, loaded into memory, and cached on the local machine
        // 2) Assets that exist on the local machine but not on the server, and should be deleted because they are no longer necessary (or were given a completely new reference in the manifest)
        // 3) Assets that exist both locally and remotely but there's a difference (based on hash values), and so the server asset should replace the local one
        // 4) Assets that exist on both machines but have no differing hash values can be loaded from the local cache as normal

        // 1) Assets that exist on the server but not on the local machine
        var newAssets = serverManifest.Assets.Where(s => !currentManifest.Assets.Any(l => s.Path == l.Path)).ToList();
        Debug.Log("New assets:");
        foreach(Asset a in newAssets)
        {
            Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
        }

        // 2) Assets that exist on the local machine but not on the server
        var orphanedAssets = currentManifest.Assets.Where(l => !serverManifest.Assets.Any(s => s.Path == l.Path)).ToList();
        Debug.Log("Orphaned assets:");
        foreach(Asset a in orphanedAssets)
        {
            Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
        }

        // 3) Assets that exist on both machines but there's a hashcode mismatch
        var changedAssets = serverManifest.Assets.Where(s => currentManifest.Assets.Any(l => s.Path == l.Path && s.Hash != l.Hash)).ToList();
        Debug.Log("Modified assets:");
        foreach(Asset a in changedAssets)
        {
            Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
        }

        // 4) Any local asset that doesn't appear on any of the above lists should exist in the local cache, and can be loaded as normal.
        var cachedAssets = currentManifest.Assets.Where(l => !orphanedAssets.Concat(changedAssets).Any(n => l.Path == n.Path)).ToList();
        Debug.Log("Cached assets:");
        foreach(Asset a in cachedAssets)
        {
            Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
        }

        // Since the changed assets and the new assets both need to be downloaded from the server we'll go ahead and bundle them together so the downloader
        // can get them at the same time.
        var assetsToDownload = newAssets.Concat(changedAssets).ToList();

        // Send out our batched asset manifest entries to be taken care of.
        BatchLoadFromCache(cachedAssets);
        BatchDeleteFromCache(orphanedAssets);
        BatchDownload(assetsToDownload);

        // Cache the new version and asset manifest locally. We do the version file last because that's how we know the cache was successfully created.
        // A missing or out of date version file means the cache may be corrupt/incomplete and needs to be rebuilt.
        CacheVersion();
        CacheManifest();
    }

    // Given a list of asset manifest entries, makes a group of http requests to download the assets from the server.
    private void BatchDownload(List<Asset> batch)
    {
        foreach(Asset asset in batch)
        {
            HTTPRequest assetRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/" + asset.Path), OnAssetRequestFinished);
            // Send the asset manifest entry as a tag so we can identify this request later.
            assetRequest.Tag = asset;
            assetRequest.Send();

            activeRequests.Add(assetRequest);

            Debug.LogFormat("Download started for '{0}'.", asset.Path);
        }
    }

    // The method called when our asset download request gets a complete response back.
    // NOTE: Right now it only works for webp assets. If other types of assets are added in the future then we need either a new method,
    // or some way of identifying the type of asset requested and returned.
    private void OnAssetRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        var bytes = resp.Data;
        Texture2D webpTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: true, lLinear: true, lError: out Error lError);

        if (lError == Error.Success)
        {
            Asset assetData = req.Tag as Asset;
            LoadedImageAsset newAsset = new LoadedImageAsset(assetData.Name, assetData.Path, assetData.Hash, webpTexture);
            loadedAssets.Add(newAsset.path, newAsset);
            newAsset.Save(cacheDirectory);

            Debug.LogFormat("Download of '{0}' complete. Asset has been cached and loaded into memory.", assetData.Path);
        }
        else
        {
            Debug.LogError("Webp Load Error : " + lError.ToString());
        }

        // Regardless of the results, this request is no longer active.
        activeRequests.Remove(req);
    }

    // Given a list of asset manifest entries, loads each asset file from the cache and into memory.
    private void BatchLoadFromCache(List<Asset> batch)
    {
        foreach(Asset asset in batch)
        {
            LoadedImageAsset newAsset = new LoadedImageAsset().Load(cacheDirectory, asset.Name, asset.Path, asset.Hash);

            if(newAsset != null)
            {
                loadedAssets.Add(newAsset.path, newAsset);

                Debug.LogFormat("Loaded '{0}' from cache and into memory.", newAsset.path);
            }
        }
    }

    // Given a list of asset manifest entries, deletes each asset file from the cache.
    // Note: This is only for deleting assets stored locally on disk. Any assets already in memory will be left untouched.
    private void BatchDeleteFromCache(List<Asset> batch)
    {
        foreach(Asset asset in batch)
        {
            string filePath = Path.Combine(cacheDirectory, asset.Path);
            if(File.Exists(filePath))
            {
                File.Delete(filePath);

                Debug.LogFormat("Deleted '{0}' from the cache.", asset.Path);
            }
            else
            {
                Debug.LogWarningFormat("Cannot delete file at '{0}' because it doesn't exist.", filePath);
            }
        }
    }

    // Delete everything from the cache and leave an empty folder.
    private void ClearCache()
    {

    }

    // Callback for our http version request.
    private void OnVersionRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // TODO: Error checking
        // Now that we got our response from the server, parse the results into a usable object.
        serverVersion = JsonConvert.DeserializeObject<VersionNumber>(resp.DataAsText);
        Debug.Log("Server manifest version downloaded. Version: " + serverVersion.Version);

        if(currentVersion == null || serverVersion.Version != currentVersion.Version)
        {
            // There's no version on file, or the versions are different.
            Debug.Log("Server manifest version doesn't match version on file. Downloading new manifest.");
            manifestRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/manifest.json"), OnManifestRequestFinished);
            manifestRequest.Send();
        }
    }

    private void OnManifestRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // Parse the results into a usable object.
        serverManifest = JsonConvert.DeserializeObject<AssetManifest>(resp.DataAsText);
        Debug.Log("Server manifest downloaded.");

        UpdateCache();
    }

    private void CacheVersion()
    {
        string serializedData = JsonUtility.ToJson(serverVersion);
        SaveDataToFile("manifest_version.dat", serializedData);

        currentVersion = serverVersion;
    }

    private void LoadVersion()
    {
        currentVersion = JsonUtility.FromJson<VersionNumber>(LoadDataFromFile("manifest_version.dat"));
    }

    private void CacheManifest()
    {
        string serializedData = JsonUtility.ToJson(serverManifest);
        SaveDataToFile("manifest.dat", serializedData);

        currentManifest = serverManifest;
    }

    private void LoadManifest()
    {
        currentManifest = JsonUtility.FromJson<AssetManifest>(LoadDataFromFile("manifest.dat"));
    }

    // Method which handles writing serialized classes to the persistent data folder.
    private void SaveDataToFile(string path, string json)
    {
        var filePath = Path.Combine(cacheDirectory, path);
        File.WriteAllText(filePath, json);
    }

    // Method which handles loading serialized classes from the persistent data folder.
    private string LoadDataFromFile(string path)
    {
        var filePath = Path.Combine(cacheDirectory, path);

        if(File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        else
        {
            Debug.LogErrorFormat("There is no file under the path {0}", filePath);
            return "";
        }
    }

    // This method tells the caller if there is a cached asset manifest on file.
    private bool CachedManifestExists()
    {
        return false;
    }

    // Check with the server to make sure the assets stored in our cache are up to date.
    private void PerformVersionCheck()
    {}

    // Update an individual asset with a new version from the server.
    private void UpdateAsset()
    {}

    // If we have a cache of web assets saved on disk, load them into memory.
    private void LoadCacheFromDisk()
    {}

    // Save all web assets in memory to disk.
    private void SaveCacheToDisk()
    {}

    // Load an individual asset from disk. This may be more extensible than a single cache load function,
    // because a caller can perform its own checking of cache integrity, versioning, etc. and decide if
    // and when to load an asset.
    private void LoadAssetFromDisk()
    {}

    // Save an individual asset to disk. This may be more extensible than a single cache save function,
    // because a caller can decide if and when to save an asset to the on-disk cache rather than save all at once.
    private void SaveAssetToDisk()
    {}
}
