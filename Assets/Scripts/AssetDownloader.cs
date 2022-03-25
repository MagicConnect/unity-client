using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

using BestHTTP;
using Newtonsoft.Json;
using WebP;

using UnityEngine.UI;

public class AssetDownloader : MonoBehaviour
{
    // DELETE ME: From failed attempt to use C#'s HttpClient library.
    static readonly HttpClient client = new HttpClient();

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
        //GetPosts();
        //GetVersionNumber();
        //GetAssetManifest();
        //DownloadWebpAsset();
        //MakeWebRequest();
        request = new HTTPRequest(new Uri("https://art.magic-connect.com/manifest.json"), OnRequestFinished);
        request.Send();

        HTTPRequest webpRequest = new HTTPRequest(new Uri("https://art.magic-connect.com/assets/art/accessories/Magic_Emblem.webp"), OnWebpRequestFinished);
        webpRequest.Send();
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

    public void OnRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        //Debug.Log(resp.DataAsText);
        var newManifest = JsonConvert.DeserializeObject<AssetManifest>(resp.DataAsText);
        Debug.Log("Manifest Hashcode: " + newManifest.Meta.Hash);

        foreach (Asset a in newManifest.Assets)
        {
            Debug.Log("Asset: " + a.Hash + " " + a.Name + " " + a.Path);
        }
    }

    public void OnWebpRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        Debug.Log(resp.DataAsText);

        var bytes = resp.Data;
        
        Texture2D webpTexture = Texture2DExt.CreateTexture2DFromWebP(bytes, lMipmaps: true, lLinear: true, lError: out Error lError);
        
        if (lError == Error.Success)
        {
            //image.texture = webpTexture;
            image.GetComponent<SpriteRenderer>().sprite = Sprite.Create(webpTexture, new Rect(0.0f, 0.0f, webpTexture.width, webpTexture.height), new Vector2(0.0f, 0.0f), 100.0f);
        }
        else
        {
            Debug.LogError("Webp Load Error : " + lError.ToString());
        }
    }

    protected virtual void OnDownloadProgress(HTTPRequest originalRequest, long downloaded, long downloadLength)
    {
        double downloadPercent = (downloaded / (double)downloadLength) * 100;
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

    IEnumerator GetRequest(string url, Action<UnityWebRequest> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Send the request and wait for a response.
            yield return request.SendWebRequest();
            callback(request);
        }
    }

    // This is a test function to make sure we can make requests to a server and get JSON data back.
    public void GetPosts()
    {
        StartCoroutine(GetRequest("https://jsonplaceholder.typicode.com/posts", (UnityWebRequest request) =>
        {
            if((request.result == UnityWebRequest.Result.ConnectionError) || (request.result == UnityWebRequest.Result.ProtocolError))
            {
                Debug.Log($"{request.error}: {request.downloadHandler.text}");
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
        }));
    }

    // Get the version number of our assets.
    public void GetVersionNumber()
    {
        StartCoroutine(GetRequest("https://art.magic-connect.com/version.json", (UnityWebRequest request) =>
        {
            if((request.result == UnityWebRequest.Result.ConnectionError) || (request.result == UnityWebRequest.Result.ProtocolError))
            {
                Debug.Log($"{request.error}: {request.downloadHandler.text}");
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
        }));
    }

    // Get the asset manifest in JSON and parse it into useable data.
    public void GetAssetManifest()
    {
        StartCoroutine(GetRequest("https://art.magic-connect.com/manifest.json", (UnityWebRequest request) =>
        {
            if((request.result == UnityWebRequest.Result.ConnectionError) || (request.result == UnityWebRequest.Result.ProtocolError))
            {
                Debug.Log($"{request.error}: {request.downloadHandler.text}");
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
        }));
    }

    // A function for downloading a webp asset from the server.
    public void DownloadWebpAsset()
    {
        StartCoroutine(GetRequest("https://art.magic-connect.com/assets/art/accessories/Magic_Emblem.webp", (UnityWebRequest request) =>
        {
            if((request.result == UnityWebRequest.Result.ConnectionError) || (request.result == UnityWebRequest.Result.ProtocolError))
            {
                Debug.Log($"{request.error}: {request.downloadHandler.text}");
            }
            else
            {
                Debug.Log(request.downloadHandler.text);
            }
        }));
    }

    static async Task MakeWebRequest()
    {
        // Call asynchronous network methods in a try/catch block to handle exceptions.
        try	
        {
            HttpResponseMessage response = await client.GetAsync("https://art.magic-connect.com/version.json");
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            // Above three lines can be replaced with new helper method below
            // string responseBody = await client.GetStringAsync(uri);

            Debug.Log(responseBody);
        }
        catch(HttpRequestException e)
        {
            Debug.Log("\nException Caught!");	
            Debug.Log("Message :" + e.Message);
            Debug.Log(e.InnerException.Message);
            Debug.Log(e.InnerException.InnerException.Message);
            Debug.Log(e.InnerException.InnerException.InnerException.Message);
        }
    }
}
