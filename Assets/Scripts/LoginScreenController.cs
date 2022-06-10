using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // Method for updating any UI elements relating to user information (usernames, account id's, etc.).
    public void UpdateUserInfoDisplay()
    {
        displayNameText.text = string.Format("Display Name: {0}", firebase.displayName);
        emailText.text = string.Format("Email Address: {0}", firebase.emailAddress);
        userIdText.text = string.Format("User ID: {0}", firebase.userId);
        userTokenText.text = string.Format("User Token: {0}", firebase.userToken);
    }
}
