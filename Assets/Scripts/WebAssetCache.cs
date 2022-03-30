using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using BestHTTP;
using Newtonsoft.Json;
using WebP;

public class WebAssetCache : MonoBehaviour
{
    // This is the singleton instance of our web asset cache. We only ever want one cache, which the entire game can access.
    public static WebAssetCache Instance {get; private set;}

    #region JSON parsed data classes
    // This is the object containing meta information we parse from the JSON manifest.
    public class MetaInformation
    {
        public string Hash {get; set;}
    }

    // Object containing asset information parsed from the JSON manifest.
    public class Asset
    {
        public string Name {get; set;}
        public string Path {get; set;}
        public string Hash {get; set;}
    }

    // This is the object representing the parsed JSON manifest.
    public class AssetManifest
    {
        public MetaInformation Meta {get; set;}
        public Asset[] Assets {get; set;}
    }

    // This is the object representing the parsed JSON version file.
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
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // First we should load our version and manifest files from disk, if they exist, so that we can compare them
        // to the results we get back from the server.

        // First we need to check the manifest version against the one we have on file.
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

    // Callback for our http version request.
    private void OnVersionRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // TODO: Error checking
        // Now that we got our response from the server, parse the results into a usable object.
        serverVersion = JsonConvert.DeserializeObject<VersionNumber>(resp.DataAsText);
        Debug.Log("Server manifest version downloaded. Version: " + serverVersion.Version);

        // Load the version file from disk, if it exists.

        // Check the server version against the manifest version we have on file. If there is no manifest version, or there is a version
        // mismatch, go ahead and download the manifest from the server and proceed from there.
        // TODO: Check if cached manifest is on disk and perform a check.
        if((currentVersion != null) && (serverVersion.Version == currentVersion.Version))
        {
            // We're good.
        }
        else
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

        // We shouldn't have to load a manifest from disk, because this callback only happens if there was a version issue.
        // But if something goes wrong, this would be one of the places to check.

        
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
