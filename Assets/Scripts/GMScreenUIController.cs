using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using BestHTTP;

public class GMScreenUIController : MonoBehaviour
{
    public event Action<string> OnResponseBodyReceived;

    // These are the input field ui objects where the 'contentId' is set by the user. After caching gamedata is implemented
    // these should be replaced by drop downs, which would be far easier to use.
    public TMP_InputField accessoryId;

    public TMP_InputField characterId;

    public TMP_InputField shopId;

    public TMP_InputField weaponId;

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
        switch(request.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(response.IsSuccess)
                {
                    LogRequest(request);
                    LogResponse(response);
                }
                else
                {
                    Debug.LogWarningFormat("Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", response.StatusCode, response.Message, response.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("Request finished with an error: " + (request.Exception != null ? (request.Exception.Message + "\n" + request.Exception.StackTrace) : "No Exception"));
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

    public void OnPostResponseReceived(HTTPRequest request, HTTPResponse response)
    {

    }

    public void OnPatchResponseReceived(HTTPRequest request, HTTPResponse response)
    {

    }

    public void OnGmAddAccessoryButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-accessory"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", accessoryId.text);

        request.Send();
    }

    public void OnGmAddCharacterButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-character"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", characterId.text);

        request.Send();
    }

    public void OnGmAddShopButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-shop"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", shopId.text);

        request.Send();
    }

    public void OnGmAddWeaponButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-weapon"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", weaponId.text);

        request.Send();
    }

    public void OnGmGainCrystalsButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/gain-crystals"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));

        request.Send();
    }

    public void OnGmGainLimitedShardsButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/gain-limited-shards"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));

        request.Send();
    }

    public void OnGmGainPermanentShardsButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/gain-permanent-shards"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));

        request.Send();
    }

    public void OnGmResetShopButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/reset-shop"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));

        request.Send();
    }

    public void OnPostGmMaintenanceButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/maintenance"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.MethodType = HTTPMethods.Post;

        request.Send();
    }

    public void OnGetGmMaintenanceButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/maintenance"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.MethodType = HTTPMethods.Get;

        request.Send();
    }

    public void OnGetGmSettingsButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/settings"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.MethodType = HTTPMethods.Get;

        request.Send();
    }

    public void OnPostGmSettingsButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/settings"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.MethodType = HTTPMethods.Post;

        request.Send();
    }

    public void OnPatchGmSettingsButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/settings"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.MethodType = HTTPMethods.Patch;

        request.Send();
    }

    // Takes a BestHTTP request and dumps information about it to the console for testing.
    public void LogRequest(HTTPRequest request, bool verbose = false)
    {
        string formattedOutput = "[Logging HTTP Request]";

        // Basic essential information about the request. Usually where the request was made to and what kind of data was sent.
        formattedOutput = string.Concat(formattedOutput, string.Format("Original URI: {0}\n", request.Uri));
        formattedOutput = string.Concat(formattedOutput, string.Format("Headers:\n {0}\n", request.DumpHeaders()));

        formattedOutput = string.Concat(formattedOutput, string.Format("Form Fields:\n"));
        foreach(BestHTTP.Forms.HTTPFieldData fieldData in request.GetFormFields())
        {
            formattedOutput = string.Concat(formattedOutput, string.Format("{0} : {1}\n", fieldData.Name, fieldData.Text));
        }
        
        // A bunch of information that is probably not important unless specifically asked for, such as metadata or constraints.
        if(verbose)
        {
            formattedOutput = string.Concat(formattedOutput, string.Format("Max Redirects: {0}\n", request.MaxRedirects));
            formattedOutput = string.Concat(formattedOutput, string.Format("Request Redirected: {0}\n", request.IsRedirected));

            if (request.IsRedirected)
            {
                formattedOutput = string.Concat(formattedOutput, string.Format("Current URI: {0}\n", request.CurrentUri));
                formattedOutput = string.Concat(formattedOutput, string.Format("Redirect URI: {0}\n", request.RedirectUri));
                formattedOutput = string.Concat(formattedOutput, string.Format("Times Redirected: {0}\n", request.RedirectCount));
            }

            formattedOutput = string.Concat(formattedOutput, string.Format("Cache Only: {0}\n", request.CacheOnly));
            formattedOutput = string.Concat(formattedOutput, string.Format("Cache Disabled: {0}\n", request.DisableCache));
            formattedOutput = string.Concat(formattedOutput, string.Format("Request Completion Timeout: {0} seconds\n", request.Timeout));
            formattedOutput = string.Concat(formattedOutput, string.Format("Connect to Server Timeout: {0} seconds\n", request.ConnectTimeout));
        }
        
        Debug.Log(formattedOutput);
    }

    // Takes a BestHTTP response and dumps information about it to the console for testing.
    public void LogResponse(HTTPResponse response, bool verbose = false)
    {
        string formattedOutput = "[Logging HTTP Response]";

        // Basic essential information about the response, such as the data returned and the status code.
        formattedOutput = string.Concat(formattedOutput, string.Format("Response Code: {0} Message: {1}\n", response.StatusCode, response.Message));
        formattedOutput = string.Concat(formattedOutput, string.Format("Response Data: {0}\n", response.DataAsText));

        formattedOutput = string.Concat(formattedOutput, string.Format("Headers:\n"));
        foreach(KeyValuePair<string, List<string>> header in response.Headers)
        {
            formattedOutput = string.Concat(formattedOutput, string.Format("{0} : {1}\n", header.Key, header.Value));
        }

        // Less important information that shouldn't be shown unless specifically asked for, like caching or metadata.
        if(verbose)
        {

        }

        Debug.Log(formattedOutput);
    }
}
