using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using UnityEngine.UI;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    public GameObject image;
    public TMP_Text statusText;
    public TMP_Text progressPercentage;
    public Slider progressBar;

    // This string indicates what the status text should say each frame. The status text can't
    // always be set directly because some cache events are invoked on separate threads, where Unity code is not allowed to be executed.
    string statusMessage = "";

    public int numFilesToDownload = 0;
    public int totalDownloads = 0;
    public int numFilesToCache = 0;
    public int totalFilesCached = 0;
    public int numFilesToLoad = 0;
    public int totalFilesLoaded = 0;
    public int numFilesToDelete = 0;
    public int totalFilesDeleted = 0;

    // TODO: If the animated loading screen message becomes an official part of the design, refactor this into a
    // dedicated script.
    public TMP_Text loadingMessage;
    public float messageRefreshRate = 1.0f;
    public float timeToRefresh = 0.0f;
    private string ellipses = "...";

    // This lets the manager know it's time to load the next scene. The manager can handle when to actually load the next scene.
    private bool isReadyToLoadGame = false;

    // Whether or not to start loading the game automatically.
    // TODO: There is no mechanism for manually starting the load process. Add something like a "start" button or input listener
    // if we want this flag to be useful.
    public bool autoStart = true;

    // Whether or not to load the next scene once the game assets have been loaded.
    // TODO: There is no way for the user to manually load the next scene yet. Add one.
    public bool autoLoadScene = true;

    // How long to wait before starting to load the game automatically.
    public float autoStartTime = 1.0f;

    // Name of the cutscene scene to be loaded.
    public string cutsceneScene;

    // Name of the login scene to be loaded.
    public string loginScene;

    Coroutine yarnSceneLoader;

    Coroutine loadingCoroutine;

    // Testing cutscene only builds in the editor isn't fun, so this should help.
    public bool loadCutscene = false;

    // The prefab of the ingame debug console plugin to be instantiated.
    public GameObject ingameDebugConsolePrefab;

    public IngameDebugConsole.DebugLogManager ingameDebugConsole;

    // Start is called before the first frame update
    void Start()
    {
        SubscribeToCacheEvents();

        // Get the Ingame Debug Console instance and configure it for our needs.
        ingameDebugConsole = IngameDebugConsole.DebugLogManager.Instance;

        // Disable the event system gameobject within this instance. Unity doesn't like there being 2 or more event systems,
        // and the other event systems we have placed in scenes are going to be more important in the majority of situations.
        ingameDebugConsole.transform.Find("EventSystem").gameObject.SetActive(false);

        // Make sure the console isn't destroyed on scene transitions.
        DontDestroyOnLoad(ingameDebugConsole);

#if USE_CUSTOM_INGAME_DEBUG_CONSOLE || DEVELOPMENT_BUILD
        // If the above precompiler definition is included, make sure the ingame console is active but keep it hidden by default.
        ingameDebugConsole.gameObject.SetActive(true);
        ingameDebugConsole.HideLogWindow();
#else
        // If this is a standalone non-development build, just get rid of the console.
        ingameDebugConsole.gameObject.SetActive(false);
#endif

        // Check the command line arguments to see if the ingame debug console should be used.
        if(HasArg("-console"))
        {
            string useIngameConsole = GetArg("-console");
            if (useIngameConsole == "on")
            {
                ingameDebugConsole.gameObject.SetActive(true);
                ingameDebugConsole.HideLogWindow();
            }
            else if (useIngameConsole == "off")
            {
                ingameDebugConsole.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogErrorFormat("LoadingScreenManager: Unrecognized value '{0}' for command line argument '-console'. Accepted values are: 'on', 'off'", useIngameConsole);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // If enough time has passed for the loading process to begin, launch the loading coroutine to handle it.
        if(Time.realtimeSinceStartup >= autoStartTime && loadingCoroutine == null)
        {
            loadingCoroutine = StartCoroutine(LoadingCoroutine());
        }

        // Update the loading visuals.
        timeToRefresh += Time.deltaTime;

        if(timeToRefresh >= messageRefreshRate)
        {
            ellipses += ".";

            if(ellipses == "....")
            {
                ellipses = "";
            }

            loadingMessage.text = "Loading, Please Wait" + ellipses;
            timeToRefresh = 0.0f;
        }

        UpdateProgressGraphic();
    }

    // Helper method for getting a desired command line argument.
    public string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();

        for(int i = 0; i < args.Length; i += 1)
        {
            if(args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    // Helper method for getting the existence of a command line argument, for when arguments can have no values.
    public bool HasArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();

        for(int i = 0; i < args.Length; i += 1)
        {
            if(args[i] == name)
            {
                return true;
            }
        }

        return false;
    }

    // Steps through the loading process so the update method isn't doing unnecessary checks each frame.
    public IEnumerator LoadingCoroutine()
    {
        // Load the web assets.
        WebAssetCache.Instance.Startup();

        yield return new WaitUntil(() => WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse);
        
        // Load the game data.
        GameDataCache.Instance.Startup();

        yield return new WaitUntil(() => GameDataCache.Instance.isReady);

        // Load whichever scene is next.
#if CUTSCENE_ONLY_BUILD
        yarnSceneLoader = StartCoroutine(LoadCutsceneSceneAsync());
#else
        if(!loadCutscene)
        {
            yarnSceneLoader = StartCoroutine(LoadLoginSceneAsync());
        }
        else
        {
            yarnSceneLoader = StartCoroutine(LoadCutsceneSceneAsync());
        }
#endif
    }

    // Called when the object is destroyed and when the scene changes.
    void OnDestroy()
    {
        UnsubscribeFromCacheEvents();
    }

    private void SubscribeToCacheEvents()
    {
        WebAssetCache.OnCacheReady += CacheReady;

        WebAssetCache.OnAssetAddedToDownloadQueue += FileQueuedForDownload;
        WebAssetCache.OnDownloadStarted += DownloadingFile;
        WebAssetCache.OnDownloadFinished += FileDownloaded;

        WebAssetCache.OnAssetAddedToLoadQueue += FileQueuedForLoad;
        WebAssetCache.OnLoadingFileStarted += LoadingFile;
        WebAssetCache.OnLoadingFileComplete += FileLoaded;

        WebAssetCache.OnAssetAddedToCacheQueue += FileQueuedForCache;
        WebAssetCache.OnCachingFileStarted += CachingFile;
        WebAssetCache.OnCachingFileComplete += FileCached;

        WebAssetCache.OnAssetAddedToDeleteQueue += FileQueuedForDeletion;
        WebAssetCache.OnDeletingFileStarted += DeletingFile;
        WebAssetCache.OnDeletingFileComplete += FileDeleted;

        GameDataCache.OnGameDataCacheStartupBegin += GameDataCacheStartupBegin;
        GameDataCache.OnGameDataCacheStartupEnd += OnGameDataCacheStartupEnd;
    }

    private void UnsubscribeFromCacheEvents()
    {
        WebAssetCache.OnCacheReady -= CacheReady;

        WebAssetCache.OnAssetAddedToDownloadQueue -= FileQueuedForDownload;
        WebAssetCache.OnDownloadStarted -= DownloadingFile;
        WebAssetCache.OnDownloadFinished -= FileDownloaded;

        WebAssetCache.OnAssetAddedToLoadQueue -= FileQueuedForLoad;
        WebAssetCache.OnLoadingFileStarted -= LoadingFile;
        WebAssetCache.OnLoadingFileComplete -= FileLoaded;

        WebAssetCache.OnAssetAddedToCacheQueue -= FileQueuedForCache;
        WebAssetCache.OnCachingFileStarted -= CachingFile;
        WebAssetCache.OnCachingFileComplete -= FileCached;

        WebAssetCache.OnAssetAddedToDeleteQueue -= FileQueuedForDeletion;
        WebAssetCache.OnDeletingFileStarted -= DeletingFile;
        WebAssetCache.OnDeletingFileComplete -= FileDeleted;

        GameDataCache.OnGameDataCacheStartupBegin -= GameDataCacheStartupBegin;
        GameDataCache.OnGameDataCacheStartupEnd -= OnGameDataCacheStartupEnd;
    }

    private void GameDataCacheStartupBegin()
    {
        statusMessage = "Loading game data...";
    }

    private void OnGameDataCacheStartupEnd()
    {
        statusMessage = "Game data loaded.";
    }

    private void FileQueuedForDownload(string name, string path, string hash)
    {
        numFilesToDownload += 1;
    }

    private void DownloadingFile(string name, string path, string hash)
    {
        statusMessage = string.Format("Downloading '{0}'.", name);
    }

    private void FileDownloaded(string name, string path, string hash)
    {
        totalDownloads += 1;
        statusMessage = string.Format("'{0}' downloaded.", name);
    }

    private void FileQueuedForLoad(string name, string path, string hash)
    {
        numFilesToLoad += 1;
    }

    private void LoadingFile(string name, string path, string hash)
    {
        statusMessage = string.Format("Loading '{0}'.", name);
    }

    private void FileLoaded(string name, string path, string hash)
    {
        totalFilesLoaded += 1;
        statusMessage = string.Format("'{0}' loaded.", name);
    }

    private void FileQueuedForCache(string name, string path, string hash)
    {
        numFilesToCache += 1;
    }

    private void CachingFile(string name, string path, string hash)
    {
        statusMessage = string.Format("Caching '{0}'.", name);
    }

    private void FileCached(string name, string path, string hash)
    {
        totalFilesCached += 1;
        statusMessage = string.Format("'{0}' cached.", name);
    }

    private void FileQueuedForDeletion(string name, string path, string hash)
    {
        numFilesToDelete += 1;
    }

    private void DeletingFile(string name, string path, string hash)
    {
        statusMessage = string.Format("Deleting '{0}'.", name);
    }

    private void FileDeleted(string name, string path, string hash)
    {
        totalFilesDeleted += 1;
        statusMessage = string.Format("'{0}' deleted.", name);
    }

    private void StartupComplete()
    {}

    private void CacheReady()
    {
        isReadyToLoadGame = true;
    }

    // Whenever some event happens that affects the loading progress, update the progress graphic objects to reflect the changes.
    private void UpdateProgressGraphic()
    {
        statusText.text = statusMessage;

        // Calculate the overall progress of the loading process, then display it.
        // Get the total number of file related operations the cache needs to do before being ready.
        int totalTasks = numFilesToDownload + numFilesToCache + numFilesToDelete + numFilesToLoad + 1;

        // Calculate how much of the work each individual task actually takes up and use it to find the current progress.
        // Example: 50 files to download, 50 files to cache -> downloads are 50% of the total progress. If 25 files have been
        // downloaded, which is 50% of the download progress, only 25% of the overall work has been done.
        float downloadProgress = 0.0f;
        if(numFilesToDownload > 0)
        {
            downloadProgress = ((float)numFilesToDownload / (float)totalTasks) * ((float)totalDownloads / (float)numFilesToDownload);
        }
        
        float cacheProgress = 0.0f;
        if(numFilesToCache > 0)
        {
            cacheProgress = ((float)numFilesToCache / (float)totalTasks) * ((float)totalFilesCached / (float)numFilesToCache);
        }
        
        float deleteProgress = 0.0f;
        if(numFilesToDelete > 0)
        {
            deleteProgress = ((float)numFilesToDelete / (float)totalTasks) * ((float)totalFilesDeleted / (float)numFilesToDelete);
        }
        
        float loadProgress = 0.0f;
        if(numFilesToLoad > 0)
        {
            loadProgress = ((float)numFilesToLoad / (float)totalTasks) * ((float)totalFilesLoaded / (float)numFilesToLoad);
        }

        float gamedataProgress = 0.0f;
        if(GameDataCache.Instance.isReady)
        {
            gamedataProgress = 1.0f / (float)totalTasks;
        }

        float totalProgress = downloadProgress + cacheProgress + deleteProgress + loadProgress + gamedataProgress;

        progressBar.value = totalProgress;
        progressPercentage.text = string.Format("{0}%", totalProgress * 100.0f);
    }

    // Loads the cutscene scene directly. To be used with Cutscene Only builds.
    private IEnumerator LoadCutsceneSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(cutsceneScene);

        while(!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // Loads the login screen scene.
    private IEnumerator LoadLoginSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(loginScene);

        while(!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
