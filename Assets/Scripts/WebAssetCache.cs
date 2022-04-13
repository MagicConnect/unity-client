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

using UnityEngine.Networking;

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
    private Coroutine startupCoroutine;

    // This value limits the number of HTTP web requests the downloader coroutine is allowed to make at the same time.
    public int maxNumberOfRequests = 1;

    // Flag that tells the startup coroutine whether it should run the worker coroutines one at a time or simultaneously.
    public bool runStartupSynced = true;

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

        // We're not allowed to call Application.persistentDataPath from a Monobehavior constructor (Unity's words, not mine) so we need to
        // set the path either here or in the Start() function
        cacheDirectory = Path.Combine(Application.persistentDataPath, "WebCache/");

        // Ensure that there is a cache directory we can save files to.
        if(!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // To test our diff function(s) we're going to override the loaded version and manifest data with some test data we've prepared.
        // NOTE: This should not be used to download actual files, since many of them will not exist. Only test up to the point the download
        //      batch is prepared.
        /*
        string testVersion = File.ReadAllText(Path.Combine(Application.persistentDataPath, "TestFiles/", "test_version.json"));
        currentVersion = JsonConvert.DeserializeObject<VersionNumber>(testVersion);

        string testManifest = File.ReadAllText(Path.Combine(Application.persistentDataPath, "TestFiles/", "test_manifest.json"));
        currentManifest = JsonConvert.DeserializeObject<AssetManifest>(testManifest);
        */
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Method for retrieving a loaded texture asset using the asset's path as an identifier.
    public Texture2D GetTexture2D(string path)
    {
        if(loadedAssets.ContainsKey(path))
        {
            return loadedAssets[path].texture;
        }
        else
        {
            // Potentially return an empty texture instead, or a default 'error' texture like the purple/black texture from source games.
            return null;
        }
    }

    // This function acts as a public launching point for the startup coroutine.
    public void Startup()
    {
        startupCoroutine = StartCoroutine(StartupRoutine());
    }

    // This coroutine automates the startup sequence for the cache so the cache functionality can be made more modular and independent.
    // Any complex, macroscopic startup logic should be handled here.
    private IEnumerator StartupRoutine()
    {
        Debug.Log("Startup coroutine launched.");

        Debug.Log("Checking for cached version data...");

        // First we try and load any locally cached version data.
        string versionFilePath = Path.Combine(cacheDirectory, "manifest_version.dat");
        if(File.Exists(versionFilePath))
        {
            LoadVersion();
            Debug.Log("Cached version data was found.");
        }
        else
        {
            // If no cached version was found then check for a temporary version file made during an attempted download.
            if(File.Exists(Path.Combine(cacheDirectory, "temp_version.dat")))
            {
                // If there's a temporary version file then load it and make that the new current version. The integrity check should clean up
                // and determine if there are any missing files.
                Debug.LogFormat("Temporary version file found.");
                currentVersion = JsonConvert.DeserializeObject<VersionNumber>(LoadDataFromFile("temp_version.dat"));
            }
            else
            {
                // If there's no cached version and there's no temporary version file, then we can't verify the version of any locally
                // cached files (technically we can use the manifest to do that but that isn't the intended behavior sooo).
                Debug.Log("No cached version data was found.");
            }
        }

        Debug.Log("Checking for cached manifest data...");

        // Then we go ahead and load the locally cached manifest so we can do an integrity check, and compare it to the server manifest to see what
        // changes need to be made to the cache.
        string manifestFilePath = Path.Combine(cacheDirectory, "manifest.dat");
        if(File.Exists(manifestFilePath))
        {
            LoadManifest();
            Debug.Log("Cached manifest data was found.");
        }
        else
        {
            // If there is no cached manifest, check to see if there is a temporary manifest from an in-progress download.
            if(File.Exists(Path.Combine(cacheDirectory, "temp_manifest.dat")))
            {
                // If there's a temporary manifest file then load it and make that the new current manifest.
                Debug.LogFormat("Temporary manifest file found.");
                currentManifest = JsonConvert.DeserializeObject<AssetManifest>(LoadDataFromFile("temp_manifest.dat"));
            }
            else
            {
                // If there is no cached manifest for some reason (like maybe it was deleted) there is no point trying to load any files,
                // because we don't know any information about them. Set the version to null to let the loader know what to do.
                Debug.Log("No cached manifest data was found.");
                currentVersion = null;
            }
        }

        Debug.Log("Requesting version information from the server...");

        // Any HTTP request made during a Unity event function (such as Start or Update) needs to be setup first. Since we can't
        // guarantee the caller won't be a Unity event function, we might as well put this here just to make sure.
        HTTPManager.Setup();
        HTTPManager.MaxConnectionPerServer = 1;
        versionRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/version.json"), OnVersionRequestFinished);
        versionRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
        versionRequest.Timeout = TimeSpan.FromSeconds(1000);
        versionRequest.MaxRetries = 10;
        versionRequest.Send();

        // Wait until we get the response back.
        // TODO: convert this into some kind of switch statement. There are other possible states aside from Finished that need to be handled.
        // Either that or just let the version request callback handle it, since it's already setup to handle the different possible responses.
        yield return new WaitUntil(() => versionRequest.State == HTTPRequestStates.Finished && serverVersion != null);

        // Perform an integrity check on the local cache, if there is one. The state of the local cache affects how we handle version mismatches.
        // If there are problems with the cache integrity, prepare an updated manifest that has all the cached files. Anything missing can be downloaded
        // from the server.
        bool isCacheValid = VerifyCacheIntegrity();

        // Check if the versions match. If the versions match and the local cache is intact, load all files from the cache.
        // If the cache is not intact, or there is a version mismatch, download a new manifest from the server so we can see what's up.
        Debug.Log("Checking local version against server...");
        if(currentVersion != null && currentVersion.Version == serverVersion.Version && isCacheValid)
        {
            // Versions match, so add all local assets to the loading queue.
            Debug.Log("Local cache version and server database versions match. Cache integrity verified. No downloads needed. Loading all assets from the local cache.");
            foreach(Asset asset in currentManifest.Assets)
            {
                AddAssetToLoadQueue(asset);
            }
        }
        else
        {
            // There's no version on file, or the versions are different, or the local cache is missing files.
            Debug.Log("Server manifest version doesn't match version on file. Downloading new manifest.");

            manifestRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/manifest.json"), OnManifestRequestFinished);
            manifestRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
            manifestRequest.Timeout = TimeSpan.FromSeconds(1000);
            manifestRequest.MaxRetries = 10;
            manifestRequest.Send();

            // Wait until the manifest download request completes.
            yield return new WaitUntil(() => manifestRequest.State == HTTPRequestStates.Finished && serverManifest != null);

            // Check the local manifest against the server manifest and queue up any work that needs to be done.
            PrepareUpdateAndLoadTasks();
        }

        // Launch coroutines to handle the various tasks that need to be done.
        if(queuedAssetsToDownload.Count > 0)
        {
            // Before starting the downloads, cache the server version and manifest in temporary files so we know that a
            // download attempt was made. If there are no cached local version or manifest files we'll need those to resume an interrupted download.
            string serializedData = JsonConvert.SerializeObject(serverVersion);
            SaveDataToFile("temp_version.dat", serializedData);

            serializedData = JsonConvert.SerializeObject(serverManifest);
            SaveDataToFile("temp_manifest.dat", serializedData);

            Debug.Log("Assets found in the download queue. Launching downloader coroutine.");
            assetDownloadCoroutine = StartCoroutine(AssetDownloadCoroutine());

            // If we have assets to download then we can assume we'll have files to cache. Create a caching coroutine and then
            // wait on both to finish.
            Debug.Log("Launching coroutine to cache downloaded assets.");
            assetCachingCoroutine = StartCoroutine(AssetCachingCoroutine());
        }

        // Note: Always wait for the downloads to be done because if nothing is downloaded then there is nothing to cache.
        //yield return new WaitUntil(() => assetDownloadCoroutine == null && activeRequests.Count <= 0);

        // Wait for both the download and caching coroutines to finish.
        if(runStartupSynced)
        {
            yield return new WaitUntil(() => assetDownloadCoroutine == null && assetCachingCoroutine == null);
        }
        /*
        if(queuedAssetsToCache.Count > 0)
        {
            Debug.Log("Assets found in the cache queue. Launching caching coroutine.");
            assetCachingCoroutine = StartCoroutine(AssetCachingCoroutine());
        }
        
        if(runStartupSynced)
        {
            yield return new WaitUntil(() => assetCachingCoroutine == null && queuedAssetsToCache.Count <= 0);
        }
        */
        if(queuedAssetsToLoad.Count > 0)
        {
            Debug.Log("Assets found in the load queue. Launching loader coroutine.");
            assetLoadCoroutine = StartCoroutine(AssetLoadCoroutine());
        }

        if(runStartupSynced)
        {
            yield return new WaitUntil(() => assetLoadCoroutine == null);
        }

        if(queuedAssetsToDelete.Count > 0)
        {
            Debug.Log("Assets found in the deletion queue. Launching deletion coroutine.");
            assetDeletionCoroutine = StartCoroutine(AssetDeletionCoroutine());
        }

        if(runStartupSynced)
        {
            yield return new WaitUntil(() => assetDeletionCoroutine == null);
        }

        // While the above coroutines could be run in parellel, we want to make sure all work is done before we finalize the cache.
        // Do a final wait on the above coroutines and then proceed.
        yield return new WaitUntil(() => {
            return assetDownloadCoroutine == null && assetCachingCoroutine == null && assetLoadCoroutine == null && assetDeletionCoroutine == null;
        });

        // Cache the new version and asset manifest locally. We do the version file last because that's how we know the cache was successfully created.
        // A missing or out of date version file means the cache may be corrupt/incomplete and needs to be rebuilt.
        Debug.Log("Caching version and manifest data.");
        CacheVersion();
        CacheManifest();

        // Delete the temporary version and manifest files. They aren't needed if the cache was successfully created/updated.
        Debug.Log("Deleting temporary files.");
        File.Delete(Path.Combine(cacheDirectory, "temp_version.dat"));
        File.Delete(Path.Combine(cacheDirectory, "temp_manifest.dat"));

        // The startup coroutine is complete and the cache should be ready.
        startupCoroutine = null;
        Debug.Log("Startup routine complete. Cache is ready for use. Closing down startup coroutine.");
    }

    // This method not only adds the asset to the queue, but it activates the download scheduler coroutine which will handle downloading
    // over time instead of all at once.
    private void AddAssetToDownloadQueue(Asset asset)
    {
        queuedAssetsToDownload.Enqueue(asset);

        // Check if there's a startup or download coroutine already running. If not, start one up so it can handle the download queue.
        if(assetDownloadCoroutine == null && startupCoroutine == null)
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
        int maxActiveRequests = 1;

        Debug.Log("Starting download scheduler coroutine.");

        while(queuedAssetsToDownload.Count > 0)
        {
            //Debug.LogFormat("[DEBUG] Downloader Line 435"); // DELETE AFTER TESTING
            if(activeRequests.Count < maxActiveRequests)
            {
                Asset asset = queuedAssetsToDownload.Dequeue();
                Debug.LogFormat("[DEBUG] Downloader: '{0}' Line 439", asset.Path); // DELETE AFTER TESTING
                HTTPRequest assetRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/" + asset.Path), OnAssetRequestFinished);
                Debug.LogFormat("[DEBUG] Downloader: '{0}' Line 441", asset.Path); // DELETE AFTER TESTING
                // Send the asset manifest entry as a tag so we can identify this request later.
                assetRequest.Tag = asset;
                assetRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
                assetRequest.Timeout = TimeSpan.FromSeconds(1000);
                assetRequest.MaxRetries = 10;
                assetRequest.Send();
                Debug.LogFormat("[DEBUG] Downloader: '{0}' Line 448", asset.Path); // DELETE AFTER TESTING
                activeRequests.Add(assetRequest);
                Debug.LogFormat("Download started for '{0}'.", asset.Path);
                Debug.LogFormat("[DEBUG] Downloader: '{0}' Line 451", asset.Path); // DELETE AFTER TESTING
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

        if(assetCachingCoroutine == null && startupCoroutine == null)
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

        while(queuedAssetsToCache.Count > 0 || queuedAssetsToDownload.Count > 0)
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

        if(assetDeletionCoroutine == null && startupCoroutine == null)
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

        if(assetLoadCoroutine == null && startupCoroutine == null)
        {
            assetLoadCoroutine = StartCoroutine(AssetLoadCoroutine());
        }
    }

    // This coroutine gradually loads asset data from the disk and into memory. 
    private IEnumerator AssetLoadCoroutine()
    {
        Debug.Log("Cached asset load coroutine started.");

        // This list should keep track of which asset loader coroutines are still running.
        List<Asset> runningCoroutines = new List<Asset>();

        while(queuedAssetsToLoad.Count > 0)
        {
            Asset asset = queuedAssetsToLoad.Dequeue();
            string filePath = Path.Combine(cacheDirectory, asset.Path);

            if(File.Exists(filePath))
            {
                /*byte[] pngTexture = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(0, 0);

                UnityWebRequest www = UnityWebRequestTexture.GetTexture("file:///" + filePath);
                yield return www.SendWebRequest();

                if(www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

                    LoadedImageAsset imageAsset = new LoadedImageAsset(asset.Name, asset.Path, asset.Hash, texture);
                    loadedAssets.Add(asset.Path, imageAsset);

                    Debug.LogFormat("Asset at {0} loaded from cache and into memory.", asset.Path);
                }*/

                //texture.LoadImage(pngTexture);
                
                StartCoroutine(LoadHelperCoroutine(filePath, asset, runningCoroutines));
                
                //Debug.Log("Starting new file load task.");
                //AssetLoadTask(asset);
            }
            else
            {
                Debug.LogErrorFormat("CACHE LOAD ERROR: File at path '{0}' does not exist.", filePath);
            }

            yield return null;
        }

        // Wait until all child coroutines have completed running before closing down this coroutine.
        yield return new WaitUntil(() => runningCoroutines.Count <= 0);

        assetLoadCoroutine = null;
        Debug.Log("All queued assets have been loaded. Ending load coroutine.");
    }

    // This coroutine is created to interface with the Unity Web Request handler, which we're using to asynchronously load
    // texture data from the local machine.
    private IEnumerator LoadHelperCoroutine(string filePath, Asset asset, List<Asset> runningCoroutines)
    {
        Debug.LogFormat("Starting coroutine for asynchronously loading '{0}' from disk.", asset.Path);

        // Adding this asset entry to the runningCoroutines list tells the parent coroutine that this coroutine is still running.
        // (status of Unity coroutines cannot be tracked otherwise because the Coroutine class exposes no properties or methods)
        runningCoroutines.Add(asset);

        // The 'file:///' string tells the web request to load a local file instead of trying to connect to a URL.
        UnityWebRequest www = UnityWebRequestTexture.GetTexture("file:///" + filePath);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            //Texture2D texture = new Texture2D(0, 0);
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

            LoadedImageAsset imageAsset = new LoadedImageAsset(asset.Name, asset.Path, asset.Hash, texture);
            loadedAssets.Add(asset.Path, imageAsset);

            Debug.LogFormat("Asset at {0} loaded from cache and into memory.", asset.Path);
        }
        
        // This coroutine is finished. Remove it from the provided list so the primary loading coroutine knows it's safe to shut down.
        runningCoroutines.Remove(asset);
        //yield return null;
        //AssetLoadTask(asset);
    }

    private async Task AssetLoadTask(Asset asset)
    {
        string filePath = Path.Combine(cacheDirectory, asset.Path);
        byte[] pngTexture = await LoadAssetAsync(filePath);
        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(pngTexture);

        LoadedImageAsset imageAsset = new LoadedImageAsset(asset.Name, asset.Path, asset.Hash, texture);
        loadedAssets.Add(asset.Path, imageAsset);

        Debug.LogFormat("Asset at {0} loaded from cache and into memory.", asset.Path);
    }

    private async Task<byte[]> LoadAssetAsync(string filePath)
    {
        return await Task.Run(() => File.ReadAllBytes(filePath));
    }

    // This method determines whether or not the local cache is complete. If any files are missing, either because they
    // were deleted or because they were never successfully downloaded, a new local manifest will be created to let the
    // cache manager know what needs to be fixed.
    // TODO: Create a second method for recovering files if the manifest is missing or was never created. Right now
    // someone could download most of the assets, then quit the game before the manifest is cached, and the client would
    // start the downloads from the beginning.
    private bool VerifyCacheIntegrity()
    {
        if(currentManifest != null)
        {
            Debug.Log("Cache Integrity Check: Checking for missing or corrupted files.");
            List<Asset> tempManifest = new List<Asset>();

            foreach(Asset asset in currentManifest.Assets)
            {
                string filePath = Path.Combine(cacheDirectory, asset.Path);

                if(File.Exists(filePath))
                {
                    // If the file is found in the cache, add it to the temporary asset list.
                    tempManifest.Add(asset);
                }
            }

            // If the temporary asset manifest is smaller than the current one, then one or more files were missing and the temp manifest should be our
            // new manifest. The startup routine will then use this manifest to determine which files need to be downloaded.
            if(tempManifest.Count < currentManifest.Assets.Length)
            {
                Debug.LogFormat("Cache Integrity Check: {0} local files missing. Preparing new asset manifest.", currentManifest.Assets.Length - tempManifest.Count);
                currentManifest.Assets = tempManifest.ToArray();

                return false;
            }
            else
            {
                Debug.Log("Cache Integrity Check: No local files missing.");

                return true;
            }
        }
        else
        {
            Debug.Log("Cache Integrity Check: No local manifest found. Unable to verify cache integrity.");
            return false;
        }
    }

    // Method which checks the local manifest against the server manifest to determine what changes (if any) need to be made to the
    // cache to bring it up to the current version, as well as which files can be safely loaded from the local cache. Any work that needs to be
    // done is queued up for the background coroutines to handle.
    private void PrepareUpdateAndLoadTasks()
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
        }
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

                    /*
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
                    */
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

                    //UpdateCache();
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
                    Asset assetData = req.Tag as Asset;
                    Debug.LogFormat("Response to asset download request received without errors. Attempting to cache '{0}' to local machine.", assetData.Path);
                    Debug.LogFormat("[DEBUG] Request: '{0}' Line 956", assetData.Path); // DELETE AFTER TESTING
                    var bytes = resp.Data;
                    Debug.LogFormat("[DEBUG] Request: '{0}' Line 958", assetData.Path); // DELETE AFTER TESTING
                    Texture2D webpTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: false, lLinear: true, lError: out Error lError, makeNoLongerReadable: false);
                    Debug.LogFormat("[DEBUG] Request: '{0}' Line 960", assetData.Path); // DELETE AFTER TESTING

                    if (lError == Error.Success)
                    {
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
        Debug.LogFormat("[DEBUG] Request: Line 1010"); // DELETE AFTER TESTING
        activeRequests.Remove(req);
        Debug.LogFormat("Request no longer active. {0} requests remain active.", activeRequests.Count);
    }

    private void CacheVersion()
    {
        // If there is a server version, cache it. If not, we have nothing to do.
        if(serverVersion != null)
        {
            string serializedData = JsonConvert.SerializeObject(serverVersion);
            SaveDataToFile("manifest_version.dat", serializedData);

            currentVersion = serverVersion;
        }
    }

    private void LoadVersion()
    {
        //currentVersion = JsonUtility.FromJson<VersionNumber>(LoadDataFromFile("manifest_version.dat"));
        currentVersion = JsonConvert.DeserializeObject<VersionNumber>(LoadDataFromFile("manifest_version.dat"));
    }

    private void CacheManifest()
    {
        // If there is a server manifest, cache it. If not, we have nothing to do.
        if(serverManifest != null)
        {
            string serializedData = JsonConvert.SerializeObject(serverManifest);
            SaveDataToFile("manifest.dat", serializedData);

            currentManifest = serverManifest;
        }
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
}
