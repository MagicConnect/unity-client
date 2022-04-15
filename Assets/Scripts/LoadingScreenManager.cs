using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using TMPro;

public class LoadingScreenManager : MonoBehaviour
{
    public GameObject image;
    public TMP_Text statusText;

    public TMP_Text progressPercentage;

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

    // Start is called before the first frame update
    void Start()
    {
        SubscribeToCacheEvents();
    }

    // Update is called once per frame
    void Update()
    {
        timeToRefresh += Time.deltaTime;

        if(timeToRefresh >= messageRefreshRate)
        {
            ellipses += ".";

            if(ellipses == "....")
            {
                ellipses = ".";
            }

            loadingMessage.text = "Loading, Please Wait" + ellipses;
            timeToRefresh = 0.0f;
        }
    }

    private void SubscribeToCacheEvents()
    {
        WebAssetCache.OnAssetAddedToDownloadQueue += FileQueuedForDownload;
        //WebAssetCache.OnDownloadStarted += FileDownloaded;
        WebAssetCache.OnDownloadFinished += FileDownloaded;

        WebAssetCache.OnAssetAddedToLoadQueue += FileQueuedForLoad;
        WebAssetCache.OnLoadingFileComplete += FileLoaded;

        WebAssetCache.OnAssetAddedToCacheQueue += FileQueuedForCache;
        WebAssetCache.OnCachingFileComplete += FileCached;

        WebAssetCache.OnAssetAddedToDeleteQueue += FileQueuedForDeletion;
        WebAssetCache.OnDeletingFileComplete += FileDeleted;
    }

    private void UnsubscribeFromCacheEvents()
    {}

    private void FileQueuedForDownload(string path)
    {
        numFilesToDownload += 1;
        UpdateProgressGraphic();
    }

    private void FileDownloaded(string path)
    {
        totalDownloads += 1;
        UpdateProgressGraphic();
    }

    private void FileQueuedForLoad(string path)
    {
        numFilesToLoad += 1;
        UpdateProgressGraphic();
    }

    private void FileLoaded(string path)
    {
        totalFilesLoaded += 1;
        UpdateProgressGraphic();
    }

    private void FileQueuedForCache(string path)
    {
        numFilesToCache += 1;
        UpdateProgressGraphic();
    }

    private void FileCached(string path)
    {
        totalFilesCached += 1;
        UpdateProgressGraphic();
    }

    private void FileQueuedForDeletion(string path)
    {
        numFilesToDelete += 1;
        UpdateProgressGraphic();
    }

    private void FileDeleted(string path)
    {
        totalFilesDeleted += 1;
        UpdateProgressGraphic();
    }

    // Whenever some event happens that affects the loading progress, update the progress graphic objects to reflect the changes.
    private void UpdateProgressGraphic()
    {
        statusText.text = "";

        // Calculate the overall progress of the loading process, then display it.
        // Get the total number of file related operations the cache needs to do before being ready.
        int totalTasks = numFilesToDownload + numFilesToCache + numFilesToDelete + numFilesToLoad;

        // Calculate how much of the work each individual task actually takes up and use it to find the current progress.
        // Example: 50 files to download, 50 files to cache -> downloads are 50% of the total progress. If 25 files have been
        // downloaded, which is 50% of the download progress, only 25% of the overall work has been done.
        float downloadProgress = 0.0f;
        if(numFilesToDownload > 0)
        {
            downloadProgress = (numFilesToDownload / totalTasks) * (totalDownloads / numFilesToDownload);
        }
        
        float cacheProgress = 0.0f;
        if(numFilesToCache > 0)
        {
            cacheProgress = (numFilesToCache / totalTasks) * (totalFilesCached / numFilesToCache);
        }
        
        float deleteProgress = 0.0f;
        if(numFilesToDelete > 0)
        {
            deleteProgress = (numFilesToDelete / totalTasks) * (totalFilesDeleted / numFilesToDelete);
        }
        
        float loadProgress = 0.0f;
        if(numFilesToLoad > 0)
        {
            loadProgress = (numFilesToLoad / totalTasks) * (totalFilesLoaded / numFilesToLoad);
        }

        float totalProgress = downloadProgress + cacheProgress + deleteProgress + loadProgress;

        Debug.LogFormat("Progress: {0}", totalProgress * 100);

        progressPercentage.text = string.Format("{0}%", totalProgress * 100);
    }

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
