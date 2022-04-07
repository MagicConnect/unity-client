using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

using BestHTTP;
using Newtonsoft.Json;
using WebP;

using System.Threading;
using System.Threading.Tasks;

public class WebAssetCache : MonoBehaviour
{
    // This is the singleton instance of our web asset cache. We only ever want one cache, which the entire game can access.
    public static WebAssetCache Instance {get; private set;}

    #region JSON parsed data classes
    // This is the object containing meta information we parse from the JSON manifest.
    [System.Serializable]
    public class MetaInformation
    {
        [SerializeField]
        public string Hash {get; set;}
    }

    // Object containing asset information parsed from the JSON manifest.
    [System.Serializable]
    public class Asset
    {
        [SerializeField]
        public string Name {get; set;}
        [SerializeField]
        public string Path {get; set;}
        [SerializeField]
        public string Hash {get; set;}
    }

    // This is the object representing the parsed JSON manifest.
    [System.Serializable]
    public class AssetManifest
    {
        [SerializeField]
        public MetaInformation Meta {get; set;}
        [SerializeField]
        public Asset[] Assets {get; set;}
    }

    // This is the object representing the parsed JSON version file.
    [System.Serializable]
    public class VersionNumber
    {
        [SerializeField]
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
            if(texture)
            {
                // Ensure that the directories in the given path exist, so that writing to a file will be successful.
                string filePath = Path.Combine(cacheDirectory, path);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // The Texture2D class is not serializable by Unity, so we're just going to save the image as a png and load it back as a Texture2D later.
                byte[] pngTexture = texture.EncodeToPNG();
                File.WriteAllBytes(filePath, pngTexture);
            }
            else
            {
                Debug.LogError("ERROR: Attempted to save nonexistent Texture2D.");
            }
        }

        public void ThreadsafeSave(string cacheDirectory)
        {

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
    public List<HTTPRequest> activeRequests {get; private set;} = new List<HTTPRequest>();

    // INSERT HERE: Data structure(s) representing the in-memory cache of our web assets.
    public Dictionary<string, LoadedImageAsset> loadedAssets {get; private set;} = new Dictionary<string, LoadedImageAsset>();

    // Because we want our file I/O to be asynchronous, we're going to track any incomplete file work here so it can be handled at a later frame instead of all at once.
    Queue<Asset> queuedAssetsToCache = new Queue<Asset>();
    Queue<Asset> queuedAssetsToDelete = new Queue<Asset>();
    Queue<Asset> queuedAssetsToLoad = new Queue<Asset>();
    Queue<Asset> queuedAssetsToDownload = new Queue<Asset>();

    // The handles for our I/O coroutines. We can use these to stop running coroutines, and to check if a coroutine is already running and handling tasks.
    private Coroutine assetDownloadCoroutine;
    private Coroutine assetDeletionCoroutine;
    private Coroutine assetCachingCoroutine;
    private Coroutine assetLoadCoroutine;

    // TODO: Turn this into a more comprehensive state variable so different parts of the cache can more effectively coordinate with each other.
    private bool isDownloading = false;

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

        if(!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }

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
        
        
        

        //StartCoroutine(TaskTestCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // This function puts the whole cache into motion. Should be called before attempting to use the cache.
    public void Startup()
    {
        // TODO: any http request code sent from a Unity event should have the BestHTTP.HTTPManager.Setup() function called, or it should be moved
        // outside the Unity event. Since our downloading is being done from a Start() event, this could be causing problems unless fixed.
        // Next we need to check the manifest version against the one we have on file.

        //HTTPManager.Setup();
        HTTPManager.MaxConnectionPerServer = 1;
        versionRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/version.json"), OnVersionRequestFinished);
        versionRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
        versionRequest.Timeout = TimeSpan.FromSeconds(1000);
        versionRequest.Send();
    }

    private void VerifyCacheIntegrity()
    {

    }

    private IEnumerator TaskTestCoroutine()
    {
        Task[] tasks = new Task[4];

        while(true)
        {
            for(int i = 0; i < tasks.Length; i += 1)
            {
                if (tasks[i] != null)
                {
                    Debug.LogFormat("Task {0} status: {1}", i, tasks[i].Status);
                }
                else
                {
                    tasks[i] = Task.Run(() => Thread.Sleep(1000));
                }
            }
            yield return null;
        }
    }

    // Method handling updating the cache with new data from the server.
    private void UpdateCache()
    {
        Debug.Log("Beginning cache update.");
        // If we don't have a cached manifest then we just go ahead and download everything from the server.
        // NOTE: May need to delete the cache, just in case we have a bunch of garbage data.
        if(currentManifest == null)
        {
            Debug.Log("No local asset manifest found. Downloading all server assets.");
            var assetsToDownload = serverManifest.Assets.ToList();

            foreach(Asset asset in assetsToDownload)
            {
                AddAssetToDownloadQueue(asset);
            }

            Debug.Log("Caching version and manifest data.");
            CacheVersion();
            CacheManifest();
        }
        else
        {
            Debug.Log("Local manifest found. Updating local cache.");
            // If we have a local manifest as well as a server manifest then we need to find:
            // 1) Assets that exist on the server but not on the local machine, and thus need to be downloaded, loaded into memory, and cached on the local machine
            // 2) Assets that exist on the local machine but not on the server, and should be deleted because they are no longer necessary (or were given a completely new reference in the manifest)
            // 3) Assets that exist both locally and remotely but there's a difference (based on hash values), and so the server asset should replace the local one
            // 4) Assets that exist on both machines but have no differing hash values can be loaded from the local cache as normal

            // 1) Assets that exist on the server but not on the local machine
            Debug.Log("TEST => Local manifest entries:");
            foreach(Asset a in currentManifest.Assets)
            {
                Debug.Log(a.Name);
            }

            Debug.Log("TEST => Server manifest entries:");
            foreach(Asset a in serverManifest.Assets)
            {
                Debug.Log(a.Name);
            }


            var newAssets = serverManifest.Assets.Where(s => !currentManifest.Assets.Any(l => s.Path == l.Path)).ToList();
            Debug.Log("New assets to be downloaded:");
            foreach (Asset a in newAssets)
            {
                Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
            }

            // 2) Assets that exist on the local machine but not on the server
            var orphanedAssets = currentManifest.Assets.Where(l => !serverManifest.Assets.Any(s => s.Path == l.Path)).ToList();
            Debug.Log("Orphaned assets to be deleted:");
            foreach (Asset a in orphanedAssets)
            {
                Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
            }

            // 3) Assets that exist on both machines but there's a hashcode mismatch
            var changedAssets = serverManifest.Assets.Where(s => currentManifest.Assets.Any(l => s.Path == l.Path && s.Hash != l.Hash)).ToList();
            Debug.Log("Modified assets to be replaced:");
            foreach (Asset a in changedAssets)
            {
                Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
            }

            // 4) Any local asset that doesn't appear on any of the above lists should exist in the local cache, and can be loaded as normal.
            var cachedAssets = currentManifest.Assets.Where(l => !orphanedAssets.Concat(changedAssets).Any(n => l.Path == n.Path)).ToList();
            Debug.Log("Cached assets to be loaded from disk:");
            foreach (Asset a in cachedAssets)
            {
                Debug.LogFormat("   Name: {0} Path: {1} Hash: {2}", a.Name, a.Path, a.Hash);
            }

            // Since the changed assets and the new assets both need to be downloaded from the server we'll go ahead and bundle them together so the downloader
            // can get them at the same time.
            var assetsToDownload = newAssets.Concat(changedAssets).ToList();

            // Send out our batched asset manifest entries to be taken care of.
            foreach(Asset asset in cachedAssets)
            {
                //queuedAssetsToLoad.Enqueue(asset);
                AddAssetToLoadQueue(asset);
            }
            
            foreach(Asset asset in orphanedAssets)
            {
                AddAssetToDeletionQueue(asset);
            }

            foreach(Asset asset in assetsToDownload)
            {
                AddAssetToDownloadQueue(asset);
            }

            // Cache the new version and asset manifest locally. We do the version file last because that's how we know the cache was successfully created.
            // A missing or out of date version file means the cache may be corrupt/incomplete and needs to be rebuilt.
            Debug.Log("Caching version and manifest data.");
            CacheVersion();
            CacheManifest();
        }
    }

    // This method not only adds the asset to the queue, but it activates the download scheduler coroutine which will handle downloading
    // over time instead of all at once.
    private void AddAssetToDownloadQueue(Asset asset)
    {
        queuedAssetsToDownload.Enqueue(asset);

        // Check if there's a coroutine already running. If not, start one up so it can handle the download queue.
        if(assetDownloadCoroutine == null)
        {
            assetDownloadCoroutine = StartCoroutine(AssetDownloadCoroutine());
        }
    }

    // This coroutine takes asset manifest entries off of the queue and creates new HTTP requests to download them from the server.
    // When there are no more assets to download or requests to create the coroutine shuts down.
    private IEnumerator AssetDownloadCoroutine()
    {
        // It's unclear what is causing the "closed unexpectedly" error, but at this point I can only assume the server has some issue with how much data
        // we're trying to get at once. The plugin should automatically handle this since there is a maximum number of connections it can make to the server,
        // but I don't have any other ideas.
        int maxActiveRequests = 5;

        Debug.Log("Starting download scheduler coroutine.");

        while(queuedAssetsToDownload.Count > 0)
        {
            if(activeRequests.Count < maxActiveRequests)
            {
                Asset asset = queuedAssetsToDownload.Dequeue();
                HTTPRequest assetRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/" + asset.Path), OnAssetRequestFinished);

                // Send the asset manifest entry as a tag so we can identify this request later.
                assetRequest.Tag = asset;
                assetRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
                assetRequest.Timeout = TimeSpan.FromSeconds(1000);
                assetRequest.Send();

                isDownloading = true;
                activeRequests.Add(assetRequest);
                Debug.LogFormat("Download started for '{0}'.", asset.Path);
            }
            
            yield return null;
        }

        // Now that the coroutine is finished we can let it vanish into the void.
        assetDownloadCoroutine = null;
        Debug.Log("All queued downloads have been started. Ending download scheduler coroutine.");
    }

    private void AddAssetToCachingQueue(Asset asset)
    {
        queuedAssetsToCache.Enqueue(asset);

        if(assetCachingCoroutine == null)
        {
            assetCachingCoroutine = StartCoroutine(AssetCachingCoroutine());
        }
    }

    // This coroutine gradually saves asset data to the disk. If we attempted to save the assets all at once the client might freeze up,
    // so this coroutine makes sure the amount of work to be done each frame stays manageable.
    private IEnumerator AssetCachingCoroutine()
    {
        Task[] tasks = new Task[4];
        Asset assetData = null;
        LoadedImageAsset imageAsset = null;

        while(isDownloading)
        {
            yield return null;
        }

        while(queuedAssetsToCache.Count > 0)
        {
            for(int i = 0; i < tasks.Length; i += 1)
            {
                if(tasks[i] == null || tasks[i].Status == TaskStatus.RanToCompletion)
                {
                    if(queuedAssetsToCache.Count > 0)
                    {
                        assetData = queuedAssetsToCache.Dequeue();
                        imageAsset = loadedAssets[assetData.Path];

                        // Ensure that the directories in the given path exist, so that writing to a file will be successful.
                        string filePath = Path.Combine(cacheDirectory, assetData.Path);
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                        // The Texture2D class is not serializable by Unity, so we're just going to save the image as a png and load it back as a Texture2D later.
                        byte[] pngTexture = imageAsset.texture.EncodeToPNG();

                        Debug.LogFormat("New task created. Caching '{0}' locally.", assetData.Path);

                        tasks[i] = Task.Run(() => File.WriteAllBytes(filePath, pngTexture));
                    }
                }
                else
                {
                    Debug.LogFormat("Task {0} status: {1}", i, tasks[i].Status);
                }

                yield return null;
            }

            // TODO: This might still cause stability and performance issues.
            // Change this to either a foreach loop where we write chunks of data to the disk, or create a threaded job system to handle saving of individual files.
            //imageAsset.Save(cacheDirectory);

            yield return null;
        }

        assetCachingCoroutine = null;
        Debug.Log("All queued assets have been cached. Ending caching coroutine.");
    }

    private void AddAssetToDeletionQueue(Asset asset)
    {
        queuedAssetsToDelete.Enqueue(asset);

        if(assetDeletionCoroutine == null)
        {
            assetDeletionCoroutine = StartCoroutine(AssetDeletionCoroutine());
        }
    }

    // This coroutine gradually deletes asset data from the disk. Deletion might be a much faster operation than writing, but just to make sure
    // it doesn't cause any issues we'll use this coroutine to space out the deletion operations.
    private IEnumerator AssetDeletionCoroutine()
    {
        while(queuedAssetsToDelete.Count > 0)
        {
            Asset asset = queuedAssetsToDelete.Dequeue();
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

            yield return null;
        }

        assetDeletionCoroutine = null;
        Debug.Log("All queued assets have been deleted. Ending deletion coroutine.");
    }

    private void AddAssetToLoadQueue(Asset asset)
    {
        queuedAssetsToLoad.Enqueue(asset);

        if(assetLoadCoroutine == null)
        {
            assetLoadCoroutine = StartCoroutine(AssetLoadCoroutine());
        }
    }

    // This coroutine gradually loads asset data from the disk and into memory. 
    private IEnumerator AssetLoadCoroutine()
    {
        Debug.Log("Cached asset load coroutine started.");

        while(queuedAssetsToLoad.Count > 0)
        {
            Asset asset = queuedAssetsToLoad.Dequeue();
            string filePath = Path.Combine(cacheDirectory, asset.Path);

            if(File.Exists(filePath))
            {
                byte[] pngTexture = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(0, 0);
                texture.LoadImage(pngTexture);

                LoadedImageAsset imageAsset = new LoadedImageAsset(asset.Name, asset.Path, asset.Hash, texture);
                loadedAssets.Add(asset.Path, imageAsset);

                Debug.LogFormat("Asset at {0} loaded from cache and into memory.", asset.Path);
            }
            else
            {
                Debug.LogErrorFormat("CACHE LOAD ERROR: File at path '{0}' does not exist.", filePath);
            }

            yield return null;
        }

        assetLoadCoroutine = null;
        Debug.Log("All queued assets have been loaded. Ending load coroutine.");
    }

    // The method called when our asset download request gets a complete response back.
    // NOTE: Right now it only works for webp assets. If other types of assets are added in the future then we need either a new method,
    // or some way of identifying the type of asset requested and returned.
    private void OnAssetRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // Http requests can return with a response object if successful, or a null response as a result of some error that occurred. 
        // We need to handle each possible result state of the request so we can know about and fix any issues.
        switch(req.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(resp.IsSuccess)
                {
                    // Request response was successful so we should be good to go.
                    Debug.LogFormat("Response to asset download request received without errors. Attempting to cache data to local machine.");

                    var bytes = resp.Data;
                    Texture2D webpTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: true, lLinear: true, lError: out Error lError, makeNoLongerReadable: false);

                    if (lError == Error.Success)
                    {
                        Asset assetData = req.Tag as Asset;

                        LoadedImageAsset newAsset = new LoadedImageAsset(assetData.Name, assetData.Path, assetData.Hash, webpTexture);
                        loadedAssets.Add(newAsset.path, newAsset);

                        //newAsset.Save(cacheDirectory);
                        //batchedAssetsToCache.Enqueue(assetData);
                        AddAssetToCachingQueue(assetData);

                        Debug.LogFormat("Download of '{0}' complete. Asset has been loaded into memory.", assetData.Path);
                    }
                    else
                    {
                        Debug.LogError("Webp Load Error : " + lError.ToString());
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                {
                    Debug.LogError("Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                    // I'm tired of random, unexplained errors. If most downloads complete successfully, we're just going to toss the failed ones right back on the pile until they work.
                    Asset assetData = req.Tag as Asset;
                    AddAssetToDownloadQueue(assetData);
                }
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Processing the request timed out.");
                break;
        }// end switch block

        // Regardless of the results, this request is no longer active.
        activeRequests.Remove(req);
        Debug.LogFormat("Request no longer active. {0} requests remain active.", activeRequests.Count);

        if(activeRequests.Count <= 0 && queuedAssetsToDownload.Count <= 0)
        {
            isDownloading = false;
        }
    }

    // Given a list of asset manifest entries, loads each asset file from the cache and into memory.
    private void BatchLoadFromCache(List<Asset> batch)
    {
        Debug.LogFormat("Loading {0} files from the local cache.", batch.Count);
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
        Debug.LogFormat("Deleting {0} files from the local cache.", batch.Count);
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
        // Http requests can return with a response object if successful, or a null response as a result of some error that occurred. 
        // We need to handle each possible result state of the request so we can know about and fix any issues.
        switch(req.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(resp.IsSuccess)
                {
                    // Now that we got our response from the server, parse the results into a usable object.
                    serverVersion = JsonConvert.DeserializeObject<VersionNumber>(resp.DataAsText);
                    Debug.Log("Server manifest version downloaded. Version: " + serverVersion.Version);

                    if (currentVersion == null || serverVersion.Version != currentVersion.Version)
                    {
                        // There's no version on file, or the versions are different.
                        Debug.Log("Server manifest version doesn't match version on file. Downloading new manifest.");

                        //HTTPManager.Setup();
                        manifestRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/manifest.json"), OnManifestRequestFinished);
                        manifestRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
                        manifestRequest.Timeout = TimeSpan.FromSeconds(1000);
                        manifestRequest.Send();
                    }
                    else
                    {
                        // If the version matches then we should have assets cached locally that we can load into memory.
                        foreach(Asset asset in currentManifest.Assets)
                        {
                            AddAssetToLoadQueue(asset);
                        }
                    }
                }
                else
                {
                    Debug.LogWarningFormat("Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Processing the request timed out.");
                break;
        }// end switch block
    }

    private void OnManifestRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // Http requests can return with a response object if successful, or a null response as a result of some error that occurred. 
        // We need to handle each possible result state of the request so we can know about and fix any issues.
        switch(req.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(resp.IsSuccess)
                {
                    // Parse the results into a usable object.
                    serverManifest = JsonConvert.DeserializeObject<AssetManifest>(resp.DataAsText);
                    Debug.Log("Server manifest downloaded.");

                    UpdateCache();
                }
                else
                {
                    Debug.LogWarningFormat("Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("Processing the request timed out.");
                break;
        }// end switch block
    }

    private void CacheVersion()
    {
        //string serializedData = JsonUtility.ToJson(serverVersion);
        string serializedData = JsonConvert.SerializeObject(serverVersion);
        SaveDataToFile("manifest_version.dat", serializedData);

        currentVersion = serverVersion;
    }

    private void LoadVersion()
    {
        //currentVersion = JsonUtility.FromJson<VersionNumber>(LoadDataFromFile("manifest_version.dat"));
        currentVersion = JsonConvert.DeserializeObject<VersionNumber>(LoadDataFromFile("manifest_version.dat"));
    }

    private void CacheManifest()
    {
        //string serializedData = JsonUtility.ToJson(serverManifest);
        string serializedData = JsonConvert.SerializeObject(serverManifest);
        SaveDataToFile("manifest.dat", serializedData);

        currentManifest = serverManifest;
    }

    private void LoadManifest()
    {
        //currentManifest = JsonUtility.FromJson<AssetManifest>(LoadDataFromFile("manifest.dat"));
        currentManifest = JsonConvert.DeserializeObject<AssetManifest>(LoadDataFromFile("manifest.dat"));
    }

    // Method which handles writing serialized classes to the persistent data folder.
    // TODO: This method is a lot shorter now than I expected it to be and it's only used for caching the version and manifest data,
    // so consider just deleting this.
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
