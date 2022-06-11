using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BestHTTP;

public class LoginScreenController : MonoBehaviour
{
    // TODO: Will most likely need to split these input fields between registration and sign-in,
    // so that different UI's and scripting can be implemented for the different processes.
    // User's email for registration/sign-in.
    public TMP_InputField email;
    
    // User's password for registration/sign-in.
    public TMP_InputField password;

    // Reference to our firebase handler, which handles interfacing with the firebase SDK.
    public FirebaseHandler firebase;

    public TMP_Text displayNameText;

    public TMP_Text emailText;

    public TMP_Text userIdText;

    public TMP_Text userTokenText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        UpdateUserInfoDisplay();
    }

    public void OnRegisterUserButtonClicked()
    {
        if(firebase.IsReadyForUse)
        {
            firebase.TestRegisterUser(email.text, password.text);
        }
        else
        {
            Debug.LogError("OnRegisterUserButtonClicked(): Firebase Authentication is not ready to be used. No registration attempt will be made.");
        }
    }

    public void OnSignInButtonClicked()
    {
        if(firebase.IsReadyForUse)
        {
            firebase.TestSignInUser(email.text, password.text);
        }
        else
        {
            Debug.LogError("OnSignInButtonClicked(): Firebase Authentication is not ready to be used. No sign in attempt will be made.");
        }
    }

    public void OnSignOutButtonClicked()
    {
        if(firebase.IsReadyForUse)
        {
            firebase.SignOutUser();
        }
        else
        {
            Debug.LogError("OnSignOutButtonClicked(): Firebase Authentication has not been initialized. No sign out attempt will be made.");
        }
    }

    public void OnTestApiCallButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me"), OnApiRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
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

    // Method for updating any UI elements relating to user information (usernames, account id's, etc.).
    public void UpdateUserInfoDisplay()
    {
        displayNameText.text = string.Format("Display Name: {0}", firebase.displayName);
        emailText.text = string.Format("Email Address: {0}", firebase.emailAddress);
        userIdText.text = string.Format("User ID: {0}", firebase.userId);
        userTokenText.text = string.Format("User Token: {0}", firebase.userToken);
    }
}
