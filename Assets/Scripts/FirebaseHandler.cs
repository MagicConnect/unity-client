using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Firebase;
using Firebase.Extensions;
using Firebase.Analytics;
using Firebase.Auth;

public class FirebaseHandler : MonoBehaviour
{
    // Object references for interacting with Firebase.
    private FirebaseApp app;
    private FirebaseAuth auth;
    private FirebaseUser user;

    // Information about the signed in user.
    public string displayName = "";
    public string emailAddress = "";
    public Uri photoUrl;

    // Flag which indicates that Firebase is ready to be used by the client.
    public bool IsReadyForUse = false;

    // Start is called before the first frame update
    void Start()
    {
        // NOTE: Potentially replace ContinueWithOnMainThread() to ContinueWith(). The auth sample project uses the former,
        // the quickstart guide in the documentation uses the ladder. Find out the difference and see which one works better
        // for our use cases.
        Debug.Log("Firebase Handler: Launching task to check and fix all Firebase dependencies.");
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
            var dependencyStatus = task.Result;

            if(dependencyStatus == Firebase.DependencyStatus.Available)
            {
                Debug.Log("Firebase Handler: All Firebase dependencies have been met.");
                InitializeFirebase();

                
            }
            else
            {
                Debug.LogErrorFormat("Could not resolve all Firebase dependencies: {0}", dependencyStatus);
                // Firebase Unity SDK is not safe to use here.
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void InitializeFirebase()
    {
        Debug.Log("Firebase Handler: Initializing Firebase...");
        app = Firebase.FirebaseApp.DefaultInstance;

        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);

        // Set a flag to indicate whether Firebase is ready to use by the client.
        Debug.Log("Firebase Handler: Firebase initialization succcessful. Firebase is ready to be used by the client.");
        this.IsReadyForUse = true;
    }

    // Event handler for when state changes for the signed in user. Includes sign-in and sign-out events. It is also the best place to get
    // information about the signed in user (according to the Firebase tutorial).
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if(auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if(!signedIn && user != null)
            {
                Debug.LogFormat("Signed out {0}", user.UserId);
            }

            user = auth.CurrentUser;
            if(signedIn)
            {
                Debug.LogFormat("Signed in {0}", user.UserId);
                displayName = user.DisplayName ?? "";
                emailAddress = user.Email ?? "";
                // TODO: The tutorial tried to use a raw string here instead of a Uri object. Probably a typo but check this against the sample project
                // to see what's up.
                //photoUrl = user.PhotoUrl ?? new Uri("");
            }
        }
    }

    // A test function for registering user(s) to the Firebase system. Don't use in actual code.
    public void TestRegisterUser(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task =>{
            if(task.IsCanceled)
            {
                Debug.LogError("TestRegisterUser(): CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }

            if(task.IsFaulted)
            {
                Debug.LogErrorFormat("TestRegisterUser(): CreateUserWithEmailAndPasswordAsync encountered an error: {0}", task.Exception);
                return;
            }

            // Firebase user has been created.
            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("TestRegisterUser(): Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
        });
    }

    public void TestSignInUser(string email, string password)
    {
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
            if(task.IsCanceled)
            {
                Debug.LogError("TestSignInUser(): SignInWithEmailAndPasswordAsync was canceled.");
                return;
            }

            if(task.IsFaulted)
            {
                Debug.LogErrorFormat("TestSignInUser(): SignInWithEmailAndPasswordAsync encountered an error: {0}", task.Exception);
                return;
            }

            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("TestSignInUser(): User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
        });
    }

    public void TestDeleteUser()
    {}
}
