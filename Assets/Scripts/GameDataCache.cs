using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BestHTTP;

public class GameDataCache : MonoBehaviour
{
    // Singleton instance of this class to ensure we only download all of this once, and every object knows where to find it.
    public static GameDataCache Instance {get; private set;}

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
        public string Id {get; set;}
    }

    // This is the object representing the parsed JSON manifest.
    [System.Serializable]
    public class GameDataManifest
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

    // Object representing an item parsed from JSON data.
    public class Item
    {
        public string Name;

        public string Art;

        public int SellValue;

        public string Description;

        public string ItemType;

        public string Id;
    }

    // Object representing a condition parsed from JSON data.
    // NOTE: This is currently not anywhere in the game data that I can find. It exists as a class so
    // Abilities can be parsed from JSON without issues.
    public class Condition
    {}

    // Object representing properties parsed from JSON data.
    public class Props
    {
        public bool IsPercent;

        public int BaseValue;

        public string BaseStat;
    }

    // Object representing an effect parsed from JSON data.
    public class Effect
    {
        public string Value;

        public string Target;

        public Props Props;
    }

    public class LbChange
    {
        public bool ShouldHide;

        public string Name;

        public string Description;

        public Condition[] Conditions;

        public Effect[] Effects;

        public string Trigger;

        // TODO: Figure out what this is supposed to be, because there's some sort of ambiguity or redundancy, I don't know which yet.
        public LbChange LbChanges;
    }

    public class Ability
    {
        public string Name;

        public string Description;

        public bool IsAbilityUsedAtLB0;

        public Condition[] Conditions;

        public Effect[] Effects;

        public string Trigger;

        public LbChange[] LbChanges;

        public string Id;
    }

    public class GameDataContent
    {
        public Item[] Items;

        public Ability[] Abilities;
    }
    #endregion

    private VersionNumber currentVersion;
    
    private VersionNumber serverVersion;

    public Coroutine startupCoroutine;

    public string cacheDirectory;

    // Boolean flag indicating if the gamedata cache is ready to be used yet.
    public bool isReady = false;

    // The original JSON file of the server version.
    public string versionJSON;

    // The original JSON file of the gamedata content.
    public string contentJSON;

    public static event Action OnCacheReady;

    // There is far too much data for me to realistically make classes for all of it, and it appears that
    // much of the data is still in development. Instead of parsing into easily usable objects, we're just gonna
    // have to access an automatically generated JSON object instead.
    public JObject parsedGameData {get; private set;}

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
        cacheDirectory = Path.Combine(Application.persistentDataPath, "GameDataCache/");

        // Ensure that there is a cache directory we can save files to.
        if(!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Begins the startup process once called. Startup runs in the background because it could take a while to complete.
    public void Startup()
    {
        if(startupCoroutine == null)
        {
            startupCoroutine = StartCoroutine(StartupRoutine());
        }
        else
        {
            Debug.LogErrorFormat("GameDataCache: Attempted to launch startup routine when startup routine is already in progress.");
        }
    }

    // This coroutine automates the startup sequence for the cache so the cache functionality can be made more modular and independent.
    // Any complex, macroscopic startup logic should be handled here.
    private IEnumerator StartupRoutine()
    {
        Debug.Log("GameDataCache: Startup coroutine launched.");

        Debug.Log("GameDataCache: Checking for cached version data...");

        // First we try and load any locally cached version data.
        string versionFilePath = Path.Combine(cacheDirectory, "gamedata_version.dat");
        if(File.Exists(versionFilePath))
        {
            currentVersion = JsonConvert.DeserializeObject<VersionNumber>(LoadDataFromFile("gamedata_version.dat"));
            Debug.Log("GameDataCache: Cached version data was found.");
        }
        else
        {
            Debug.Log("GameDataCache: No cached version data was found.");
        }

        Debug.Log("GameDataCache: Requesting version information from the server...");

        // Any HTTP request made during a Unity event function (such as Start or Update) needs to be setup first. Since we can't
        // guarantee the caller won't be a Unity event function, we might as well put this here just to make sure.
        HTTPManager.Setup();
        HTTPManager.MaxConnectionPerServer = 1;
        HTTPRequest versionRequest = new HTTPRequest(new Uri("https://gamedata.magic-connect.com/version.json"), OnVersionRequestFinished);
        versionRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
        versionRequest.Timeout = TimeSpan.FromSeconds(1000);
        versionRequest.MaxRetries = 10;
        versionRequest.Send();

        // Wait until we get the response back.
        // TODO: convert this into some kind of switch statement. There are other possible states aside from Finished that need to be handled.
        // Either that or just let the version request callback handle it, since it's already setup to handle the different possible responses.
        yield return new WaitUntil(() => versionRequest.State == HTTPRequestStates.Finished && serverVersion != null);

        // If the local version and the server version don't match then we'll need to download new content from the server.
        bool versionMismatch = (currentVersion == null) || (currentVersion.Version != serverVersion.Version);

        // We also need to make sure that a local cache of the content actually exists. If one doesn't, then we'll need to download a new one from the server
        // anyway.
        bool cachedContentExists = File.Exists(Path.Combine(cacheDirectory, "content.dat"));

        // If there is a version mismatch or the cache doesn't exist, go ahead and download content from the server. Otherwise, load the cached copy.
        if(versionMismatch || !cachedContentExists)
        {
            Debug.LogFormat("GameDataCache: Cached game data doesn't match server data. New data will be downloaded.");
            HTTPRequest contentRequest = new HTTPRequest(new Uri("https://gamedata.magic-connect.com/content.json"), OnContentRequestFinished);
            contentRequest.Send();

            yield return new WaitUntil(() => contentRequest.State == HTTPRequestStates.Finished && parsedGameData != null);
            Debug.LogFormat("GameDataCache: New game data has been downloaded from the server.");
        }
        else
        {
            Debug.LogFormat("GameDataCache: Local gamedata version matches server version. Loading gamedata from cache.");
            parsedGameData = JObject.Parse(LoadDataFromFile("content.dat"));
        }

        // Cache the new version and asset manifest locally. We do the version file last because that's how we know the cache was successfully created.
        // A missing or out of date version file means the cache may be corrupt/incomplete and needs to be rebuilt.
        Debug.Log("GameDataCache: Caching version and content data.");
        SaveDataToFile("content.dat", contentJSON);
        SaveDataToFile("gamedata_version.dat", versionJSON);
        currentVersion = serverVersion;

        // The startup coroutine is complete and the cache should be ready.
        startupCoroutine = null;
        isReady = true;
        Debug.Log("GameDataCache: Startup routine complete. Cache is ready for use. Closing down startup coroutine.");

        if(OnCacheReady != null)
        {
            OnCacheReady();
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
                    versionJSON = resp.DataAsText;
                    Debug.LogFormat("GameDataCache: Server manifest version downloaded. Version: {0}", serverVersion.Version);
                }
                else
                {
                    Debug.LogWarningFormat("GameDataCache: Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("GameDataCache: Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("GameDataCache: Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("GameDataCache: Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("GameDataCache: Processing the request timed out.");
                break;
        }// end switch block
    }

    // Callback for our http content request.
    private void OnContentRequestFinished(HTTPRequest req, HTTPResponse resp)
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
                    parsedGameData = JObject.Parse(resp.DataAsText);
                    contentJSON = resp.DataAsText;
                    Debug.LogFormat("GameDataCache: Server game data downloaded.");
                }
                else
                {
                    Debug.LogWarningFormat("GameDataCache: Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("GameDataCache: Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("GameDataCache: Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("GameDataCache: Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("GameDataCache: Processing the request timed out.");
                break;
        }// end switch block
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
