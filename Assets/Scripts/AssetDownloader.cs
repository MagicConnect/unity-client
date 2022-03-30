using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

using BestHTTP;
using Newtonsoft.Json;
using WebP;

using UnityEngine.UI;
using TMPro;

public class AssetDownloader : MonoBehaviour
{
    // The HTTP request we're making to the server. We want this to be at the top so it can be aborted, if necessary.
    protected HTTPRequest request;

    // The fragment size that we will set to the HTTP request.
    protected int fragmentSize = HTTPResponse.MinReadBufferSize;

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

    public GameObject image;

    public GameObject uiImage;

    public GameObject bestHTTPUIImage;

    public Slider progressBar;

    public TMP_Text progressText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDownloadButtonClicked()
    {
        if(HTTPManager.IsCachingDisabled)
        {
            Debug.Log("HTTP Request caching is disabled.");
        }
        else
        {
            Debug.Log("HTTP Request caching is enabled. Cache is stored at " + HTTPManager.GetRootCacheFolder());
        }

        progressBar.value = 0.0f;
        progressText.text = "Download progress: 0 / 100%";



        request = new HTTPRequest(new Uri("https://art.magic-connect.com/manifest.json"), OnRequestFinished);
        //request.DisableCache = false;
        request.Send();
        //Debug.Log("Manifest request headers: " + request.DumpHeaders());

        HTTPRequest webpRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/assets/art/accessories/Magic_Emblem.webp"), OnWebpRequestFinished);
        webpRequest.OnDownloadProgress += OnDownloadProgress;
        webpRequest.Send();

        HTTPRequest webhookRequest = new HTTPRequest(new Uri("https://media.wired.com/photos/5fab11172cc0d6153d3f973b/master/w_1600,c_limit/Gear-PS5-2-src-Sony.jpg"), OnWebhookRequestFinished);
        webhookRequest.Send();
    }

    protected virtual void SetupHTTPRequest()
    {
        request = new HTTPRequest(new Uri("https://jsonplaceholder.typicode.com/posts"), OnRequestFinished);
        /*
        request.StreamFragmentSize = fragmentSize;
        request.Tag = DateTime.Now;

        request.OnHeadersReceived += OnHeadersReceived;
        request.OnDownloadProgress += OnDownloadProgress;
        request.OnStreamingData += OnDataDownloaded;
        */
    }

    private void OnHeadersReceived(HTTPRequest req, HTTPResponse resp, Dictionary<string, List<string>> newHeaders)
    {
        var range = resp.GetRange();
        if (range != null)
        {
            //this.DownloadLength = range.ContentLength;
        }
        else
        {
            var contentLength = resp.GetFirstHeaderValue("content-length");
            if (contentLength != null)
            {
                long length = 0;
                if (long.TryParse(contentLength, out length))
                {
                    //this.DownloadLength = length;
                }
            }
        }
    }

    public void OnWebhookRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        if(resp.IsFromCache)
        {
            Debug.Log("Loaded webhook response from cache.");
        }
        else
        {
            Debug.Log("Downloaded webhook response from server.");
        }

        foreach(KeyValuePair<string, List<string>> header in resp.Headers)
        {
            string headerOutput = "Header: " + header.Key + " Value(s): ";
            
            foreach(string value in header.Value)
            {
                headerOutput += (" " + value);
            }

            Debug.Log(headerOutput);
        }

        Debug.Log(resp.DataAsText);
        Texture2D webTexture = resp.DataAsTexture2D;
        Sprite httpSprite = Sprite.Create(webTexture, new Rect(0.0f, 0.0f, webTexture.width, webTexture.height), new Vector2(0.0f, 0.0f), 100.0f);
        uiImage.GetComponent<Image>().sprite = httpSprite;
    }

    public void OnRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        foreach(KeyValuePair<string, List<string>> header in resp.Headers)
        {
            string headerOutput = "Header: " + header.Key + " Value(s): ";
            
            foreach(string value in header.Value)
            {
                headerOutput += (" " + value);
            }

            Debug.Log(headerOutput);
        }

        //Debug.Log(resp.DataAsText);
        if(resp.IsFromCache)
        {
            Debug.Log("Loaded asset manifest from cache.");
        }
        else
        {
            Debug.Log("Downloaded asset manifest from server.");
        }
        
        var newManifest = JsonConvert.DeserializeObject<AssetManifest>(resp.DataAsText);
        Debug.Log("Manifest Hashcode: " + newManifest.Meta.Hash);

        foreach (Asset a in newManifest.Assets)
        {
            //Debug.Log("Asset: " + a.Hash + " " + a.Name + " " + a.Path);
        }
    }

    public void OnWebpRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        foreach(KeyValuePair<string, List<string>> header in resp.Headers)
        {
            string headerOutput = "Header: " + header.Key + " Value(s): ";
            
            foreach(string value in header.Value)
            {
                headerOutput += (" " + value);
            }

            Debug.Log(headerOutput);
        }

        //Debug.Log(resp.DataAsText);
        if(resp.IsFromCache)
        {
            Debug.Log("Loaded WebP asset from cache.");
        }
        else
        {
            Debug.Log("Downloaded WebP asset from server.");
        }

        progressBar.value = 100.0f;
        progressText.text = "Download complete.";

        var bytes = resp.Data;
        
        Texture2D webpTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: true, lLinear: true, lError: out Error lError);

        // Just for testing's sake, try loading the data as a Texture2D so we aren't tethered to the WebP plugin.
        Texture2D downloadedTexture = resp.DataAsTexture2D;
        Sprite httpSprite = Sprite.Create(downloadedTexture, new Rect(0.0f, 0.0f, downloadedTexture.width, downloadedTexture.height), new Vector2(0.0f, 0.0f), 100.0f);
        bestHTTPUIImage.GetComponent<Image>().sprite = httpSprite;
        
        if (lError == Error.Success)
        {
            //image.texture = webpTexture;
            Sprite sprite = Sprite.Create(webpTexture, new Rect(0.0f, 0.0f, webpTexture.width, webpTexture.height), new Vector2(0.0f, 0.0f), 100.0f);
            image.GetComponent<SpriteRenderer>().sprite = sprite;
            uiImage.GetComponent<Image>().sprite = sprite;
        }
        else
        {
            Debug.LogError("Webp Load Error : " + lError.ToString());
        }
    }

    protected virtual void OnDownloadProgress(HTTPRequest originalRequest, long downloaded, long downloadLength)
    {
        double downloadPercent = (downloaded / (double)downloadLength) * 100;
        progressBar.value = (float)downloadPercent;
        progressText.SetText("Download progress: " + downloadPercent + " / 100%");
        //this._downloadProgressSlider.value = (float)downloadPercent;
        //this._downloadProgressText.text = string.Format("{0:F1}%", downloadPercent);
    }

    protected virtual bool OnDataDownloaded(HTTPRequest request, HTTPResponse response, byte[] dataFragment, int dataFragmentLength)
    {
        //this.ProcessedBytes += dataFragmentLength;
        //SetDataProcessedUI(this.ProcessedBytes, this.DownloadLength);

        // Use downloaded data

        // Return true if dataFrament is processed so the plugin can recycle the byte[]
        return true;
    }

    protected void SetDataProcessedUI(long processed, long length)
    {
        float processedPercent = (processed / (float)length) * 100f;

        //this._processedDataSlider.value = processedPercent;
        //this._processedDataText.text = GUIHelper.GetBytesStr(processed, 0);
    }

    protected virtual void ResetProcessedValues()
    {
        //this.ProcessedBytes = 0;
        //this.DownloadLength = 0;

        //SetDataProcessedUI(this.ProcessedBytes, this.DownloadLength);
    }
}
