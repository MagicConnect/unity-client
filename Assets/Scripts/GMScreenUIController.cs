using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using BestHTTP;

public class GMScreenUIController : MonoBehaviour
{
    public event Action<string> OnResponseBodyReceived;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnGetResponseReceived(HTTPRequest request, HTTPResponse response)
    {

    }

    public void OnPostResponseReceived(HTTPRequest request, HTTPResponse response)
    {

    }

    public void OnPatchResponseReceived(HTTPRequest request, HTTPResponse response)
    {

    }

    // Takes a BestHTTP request and dumps information about it to the console for testing.
    public void LogRequest(HTTPRequest request)
    {
        Debug.Log(request.ToString());
    }

    // Takes a BestHTTP response and dumps information about it to the console for testing.
    public void LogResponse(HTTPResponse response)
    {
        Debug.Log(response.ToString());
    }
}
