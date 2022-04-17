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

    // Name of the yarn spinner scene to be loaded.
    public string yarnDemoScene;

    Coroutine yarnSceneLoader;

    // Start is called before the first frame update
    void Start()
    {
        SubscribeToCacheEvents();
    }

    // Update is called once per frame
    void Update()
    {
        // If it's time to autoload and the cache is idle and unready, launch the startup process.
        if(Time.realtimeSinceStartup >= autoStartTime && WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.Unready)
        {
            WebAssetCache.Instance.Startup();
        }

        // If the cache is done loading, do any cleanup or preparation necessary and load the next scene.
        if(WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse && yarnSceneLoader == null)
        {
            yarnSceneLoader = StartCoroutine(LoadYarnSceneAsync());
        }

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
    }

    private void FileQueuedForDownload(string path)
    {
        numFilesToDownload += 1;
    }

    private void DownloadingFile(string path)
    {
        statusMessage = string.Format("Downloading '{0}'.");
    }

    private void FileDownloaded(string path)
    {
        totalDownloads += 1;
        statusMessage = string.Format("'{0}' downloaded.", path);
    }

    private void FileQueuedForLoad(string path)
    {
        numFilesToLoad += 1;
    }

    private void LoadingFile(string path)
    {
        statusMessage = string.Format("Loading '{0}'.", path);
    }

    private void FileLoaded(string path)
    {
        totalFilesLoaded += 1;
        statusMessage = string.Format("'{0}' loaded.", path);
    }

    private void FileQueuedForCache(string path)
    {
        numFilesToCache += 1;
    }

    private void CachingFile(string path)
    {
        statusMessage = string.Format("Caching '{0}'.", path);
    }

    private void FileCached(string path)
    {
        totalFilesCached += 1;
        statusMessage = string.Format("'{0}' cached.", path);
    }

    private void FileQueuedForDeletion(string path)
    {
        numFilesToDelete += 1;
    }

    private void DeletingFile(string path)
    {
        statusMessage = string.Format("Deleting '{0}'.");
    }

    private void FileDeleted(string path)
    {
        totalFilesDeleted += 1;
        statusMessage = string.Format("'{0}' deleted.");
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
        int totalTasks = numFilesToDownload + numFilesToCache + numFilesToDelete + numFilesToLoad;

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

        float totalProgress = downloadProgress + cacheProgress + deleteProgress + loadProgress;

        progressBar.value = totalProgress;
        progressPercentage.text = string.Format("{0}%", totalProgress * 100.0f);
    }

    private IEnumerator LoadYarnSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(yarnDemoScene);

        while(!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    // TODO: Delete this once there's a practical example of referencing assets loaded in the cache. This is here just to prove it works.
    public void TestDisplayImage()
    {
        Texture2D displayTexture = WebAssetCache.Instance.GetTexture2D("assets/art/accessories/Magic_Emblem.webp");

        if(displayTexture != null)
        {
            Sprite sprite = Sprite.Create(displayTexture, new Rect(0.0f, 0.0f, displayTexture.width, displayTexture.height), new Vector2(0.0f, 0.0f), 100.0f);
            image.GetComponent<Image>().sprite = sprite;
        }
    }
}
