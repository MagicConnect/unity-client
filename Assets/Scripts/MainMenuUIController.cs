using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP;
using TMPro;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MainMenuUIController : MonoBehaviour
{
    // TODO: Refactor into its own script file, or delete because you can just parse into a generic object.
    // Data class which the /me json response is parsed into.
    public class Account
    {}

    public FirebaseHandler firebase;

    // Rename these once you figure out what each currency is.
    public TMP_Text blueCrystalsText;

    public TMP_Text orangeCrystalsText;

    public TMP_Text goldText;

    // Start is called before the first frame update
    void Start()
    {
        firebase = FirebaseHandler.Instance;

        // Send a /me request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me"), OnApiRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnApiRequestFinished(HTTPRequest request, HTTPResponse response)
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

    // Takes a JSON string corresponding to the /me api call and uses the relevant data to update the UI.
    public void ParseResponseIntoUIInfo(string data)
    {
        // Parse the JSON response into usable data.
        var responseJsonObject = JObject.Parse(data);

        Debug.LogFormat("Main Screen Character: {0}", responseJsonObject["account"]["personalization"]["mainScreenBackground"].Value<string>());

        // The structure of the JSON response could change over time, so wrap attempts to retrieve a value
        // in try/catch blocks to make sure each exception is caught and reported to be fixed later.

        // Get and set the gold currency value.
        try
        {
            goldText.text = responseJsonObject["account"]["currencies"]["Gold"].Value<string>();
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'Gold' value -> {0}", e);
            goldText.text = 0.ToString();
        }

        // Get and set the crystal currency value.
        try
        {
            blueCrystalsText.text = responseJsonObject["account"]["currencies"]["Crystal"].Value<string>();
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'Crystal' value -> {0}", e);
            blueCrystalsText.text = 0.ToString();
        }
        
        // Get and set the world shard currency value.
        try
        {
            orangeCrystalsText.text = responseJsonObject["account"]["currencies"]["World Shard"].Value<string>();
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'World Shard' value -> {0}", e);
            orangeCrystalsText.text = 0.ToString();
        }
    }
}
