using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BestHTTP;

public class LoginScreenController : MonoBehaviour
{
    // User's email for sign-in.
    public TMP_InputField signInEmail;
    
    // User's password for sign-in.
    public TMP_InputField signInPassword;

    // User's email for registration.
    public TMP_InputField registrationEmail;

    // User's password for registration.
    public TMP_InputField registrationPassword;

    // Reference to our firebase handler, which handles interfacing with the firebase SDK.
    public FirebaseHandler firebase;

    public TMP_Text displayNameText;

    public GameObject signInPanel;

    public GameObject registrationPanel;

    public GameObject connectionPanel;

    public GameObject accountInfoPanel;

    // Start is called before the first frame update
    void Start()
    {
        firebase = FirebaseHandler.Instance;

        SubscribeToFirebaseEvents();

        StartCoroutine(FirebaseCoordinationCoroutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Destroy()
    {
        UnsubscribeFromFirebaseEvents();
    }

    // This coroutine waits until Firebase is initialized, then checks if a preexisting sign in is carried over.
    // If not, the sign in interface is displayed. Otherwise it does nothing, because the sign in event listener can handle it.
    public IEnumerator FirebaseCoordinationCoroutine()
    {
        yield return new WaitUntil(() => firebase.IsReadyForUse);

        if(!firebase.previousSignInFound && !firebase.isUserSignedIn)
        {
            signInPanel.SetActive(true);
            connectionPanel.SetActive(false);
            accountInfoPanel.SetActive(false);
        }
    }

    public void SubscribeToFirebaseEvents()
    {
        firebase.UserSignedIn += OnFirebaseUserSignedIn;
        firebase.UserSignedOut += OnFirebaseUserSignedOut;
    }

    public void UnsubscribeFromFirebaseEvents()
    {
        firebase.UserSignedIn -= OnFirebaseUserSignedIn;
        firebase.UserSignedOut -= OnFirebaseUserSignedOut;
    }

    public void OnFirebaseUserSignedIn()
    {
        connectionPanel.SetActive(true);
        accountInfoPanel.SetActive(true);
        signInPanel.SetActive(false);

        displayNameText.text = string.Format("Logged in as {0}", firebase.displayName);
    }

    public void OnFirebaseUserSignedOut()
    {
        signInPanel.SetActive(true);
        connectionPanel.SetActive(false);
        accountInfoPanel.SetActive(false);
    }

    public void OnRegisterUserButtonClicked()
    {
        if(firebase.IsReadyForUse)
        {
            firebase.RegisterUser(registrationEmail.text, registrationPassword.text);
        }
        else
        {
            Debug.LogError("OnRegisterUserButtonClicked(): Firebase Authentication is not ready to be used. No registration attempt will be made.");
        }
    }

    public void OnCancelRegistrationButtonClicked()
    {
        registrationPanel.SetActive(false);
        signInPanel.SetActive(true);
    }

    public void OnSignInButtonClicked()
    {
        if(firebase.IsReadyForUse)
        {
            firebase.SignInUser(signInEmail.text, signInPassword.text);
        }
        else
        {
            Debug.LogError("OnSignInButtonClicked(): Firebase Authentication is not ready to be used. No sign in attempt will be made.");
        }
    }

    public void OnNewAccountButtonClicked()
    {
        signInPanel.SetActive(false);
        registrationPanel.SetActive(true);
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

    // When the 'Connect' button is pressed in the UI, this method starts the process of connecting to the server and loading into the home screen.
    public void OnConnectButtonClicked()
    {
        StartCoroutine(LoadMainScreenSceneAsync());
    }

    // This coroutine loads the Home Screen scene in the background once started.
    private IEnumerator LoadMainScreenSceneAsync()
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("Home Screen");

        while(!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
