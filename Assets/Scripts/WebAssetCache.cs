using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

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

    // INSERT HERE: Data structure(s) representing the in-memory cache of our web assets.

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // First we're going to do a brief cache integrity check. If our version and manifest files exist, load them in.
        // If not, set the current version to null (if it isn't already). A null version will tell our loader that there is
        // no cache and we should start from scratch.
        string versionFilePath = Path.Combine(Application.persistentDataPath, "manifest_version.dat");
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

        string manifestFilePath = Path.Combine(Application.persistentDataPath, "manifest.dat");
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

        // Perform a check to make sure the cached assets exist and/or load them in and update the cache later as necessary.

        // Next we need to check the manifest version against the one we have on file.
        versionRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/version.json"), OnVersionRequestFinished);
        versionRequest.Send();



        // Before we do anything we need our asset manifests.
        // INSERT HERE: Load previous asset manifest from the on-disk cache. There isn't one right now because the on-disk cache isn't implemented.

        // INSERT HERE: Download the current asset manifest from the server.

        // If we don't have a cached manifest, or there are version mismatches on the manifest, download the necessary assets from the server.

        // NOTE: Be sure to save any updated files to the cache.

        // Otherwise, load the files from the cache.
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Method handling updating the cache with new data from the server.
    private void UpdateCache()
    {
        CacheVersion();
        CacheManifest();
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
    }

    private void LoadVersion()
    {
        currentVersion = JsonUtility.FromJson<VersionNumber>(LoadDataFromFile("manifest_version.dat"));
    }

    private void CacheManifest()
    {
        string serializedData = JsonUtility.ToJson(serverManifest);
        SaveDataToFile("manifest.dat", serializedData);
    }

    private void LoadManifest()
    {
        currentManifest = JsonUtility.FromJson<AssetManifest>(LoadDataFromFile("manifest.dat"));
    }

    // Method which handles writing serialized classes to the persistent data folder.
    private void SaveDataToFile(string path, string json)
    {
        var filePath = Path.Combine(Application.persistentDataPath, path);
        File.WriteAllText(filePath, json);
    }

    // Method which handles loading serialized classes from the persistent data folder.
    private string LoadDataFromFile(string path)
    {
        var filePath = Path.Combine(Application.persistentDataPath, path);

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
