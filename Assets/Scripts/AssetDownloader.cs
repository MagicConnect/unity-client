using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;

using System.Net.Http;
using System.Net;
using System.Threading.Tasks;

public class AssetDownloader : MonoBehaviour
{
    static readonly HttpClient client = new HttpClient();

    // This is the object containing meta information we parse from the JSON manifest.
    public class Meta
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
        public Meta MetaInformation {get; set;}
        public Asset[] Assets {get; set;}
    }

    // This is the object representing the parsed JSON version file.
    public class VersionNumber
    {
        public string Version {get; set;}
    }

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
        MakeWebRequest();
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
