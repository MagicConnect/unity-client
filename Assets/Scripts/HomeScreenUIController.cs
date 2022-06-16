using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BestHTTP;
using Newtonsoft.Json.Linq;

public class HomeScreenUIController : MonoBehaviour
{
    public FirebaseHandler firebase;

    public Image characterImage;

    public TMP_Text characterName;

    public Image backgroundImage;

    // Start is called before the first frame update
    void Start()
    {
        firebase = FirebaseHandler.Instance;

        UpdateUIViaApiCall();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Makes API calls to the server, then updates the UI with any important information.
    public void UpdateUIViaApiCall()
    {
        // Send a /me request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me"), OnMeRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
    }

    public void OnMeRequestFinished(HTTPRequest request, HTTPResponse response)
    {
        switch(request.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(response.IsSuccess)
                {
                    // Dump all headers and their values into the console.
                    Debug.LogFormat(this, "API Response Headers/Values:");
                    foreach(string header in response.Headers.Keys)
                    {
                        Debug.LogFormat(this, "Header: {0} Value(s): {1}", header, response.Headers[header]);
                    }

                    Debug.LogFormat(this, "Response Data: {0}", response.DataAsText);
                    Debug.LogFormat(this, "Status Code: {0} Message: {1}", response.StatusCode, response.Message);
                    Debug.LogFormat(this, "Major Version: {0} Minor Version: {1}", response.VersionMajor, response.VersionMinor);
                    Debug.LogFormat(this, "Was from cache: {0}", response.IsFromCache);

                    ParseResponseIntoUIInfo(response.DataAsText);
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

    public void ParseResponseIntoUIInfo(string data)
    {
        // Parse the JSON response into usable data.
        var responseJsonObject = JObject.Parse(data);

        // The structure of the JSON response could change over time, so wrap attempts to retrieve a value
        // in try/catch blocks to make sure each exception is caught and reported to be fixed later.

        // Get the character we should display on the screen.
        string mainScreenCharacter = "";
        try
        {
            mainScreenCharacter = responseJsonObject["account"]["personalization"]["mainScreenCharacter"].Value<string>();

            if(mainScreenCharacter != "")
            {
                // If a character was specified, load and display that character's sprite and their name.
            }
            else
            {
                // If no character was specified then just display a default character for demo purposes.
                // Later we can change it to a debug image for testing.
            }
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'mainScreenCharacter' value -> {0}", e);
        }
    }

    public void OnOverviewButtonClicked()
    {}

    public void OnQuestsButtonClicked()
    {}

    public void OnFormationButtonClicked()
    {}

    public void OnEventsButtonClicked()
    {}
}
