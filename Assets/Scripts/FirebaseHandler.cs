using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Networking;

using Firebase;
using Firebase.Extensions;
using Firebase.Analytics;
using Firebase.Auth;

public class FirebaseHandler : MonoBehaviour
{
    // There should only be one Firebase handler, which is kept alive for the duration of the client's runtime.
    // This reference helps us keep track of it.
    public static FirebaseHandler Instance;

    // Object references for interacting with Firebase.
    private FirebaseApp app;
    private FirebaseAuth auth;
    private FirebaseUser user;

    // Information about the signed in user.
    public string displayName = "";
    public string emailAddress = "";
    public string phoneNumber = "";
    public string userId = "";
    public string providerId = "";
    public UserMetadata userMetadata;
    public Uri photoUrl;

    public string userToken = "";

    // Flag which indicates that Firebase is ready to be used by the client.
    public bool IsReadyForUse = false;

    public bool UseTestFirebaseProject = true;

    public bool isUserSignedIn = false;

    public event Action FirebaseInitialized;

    public event Action<string> UserTokenReceived;

    public event Action<string> UserSignedIn;

    public event Action<string> UserSignedOut;

    public event Action<string> NewUserRegistered;

    // Awake is called upon creation of the gameobject.
    void Awake()
    {
        // Set up our singleton instance of the class.
        if(Instance != null)
        {
            // If we already have an instance of this class, destroy this instance.
            Destroy(gameObject);
            return;
        }

        // If there is no instance of this class, set it and mark it so Unity doesn't destroy it between scene changes.
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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

        // Which Firebase project we connect to depends on whether we're testing or in production.
        // TODO: Change this to a pre-processor definition so we can make test builds and production builds without having
        // to change this flag each time.
        if(UseTestFirebaseProject)
        {
            Debug.Log("Using Test Firebase project.");

            // Prepare app options for test project.
            Firebase.AppOptions testAppOptions = new Firebase.AppOptions{
                ApiKey = "AIzaSyA9iUOmdDu4rB2pe7rTzjBZwjLvAxn3xyY",
                AppId = "com.magic.connect",
                ProjectId = "magic-connect-ecf82"
            };

            var testApp = Firebase.FirebaseApp.Create(testAppOptions, "Test");
            var testAuth = Firebase.Auth.FirebaseAuth.GetAuth(testApp);

            app = testApp;
            auth = testAuth;
        }
        else
        {
            // Prepare app options for production project.
            Firebase.AppOptions prodAppOptions = new Firebase.AppOptions{
                ApiKey = "",
                AppId = "",
                ProjectId = ""
            };

            var prodApp = Firebase.FirebaseApp.Create(prodAppOptions, "Test");
            var prodAuth = Firebase.Auth.FirebaseAuth.GetAuth(prodApp);

            app = prodApp;
            auth = prodAuth;
        }

        //app = Firebase.FirebaseApp.DefaultInstance;
        //auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);

        // Set a flag to indicate whether Firebase is ready to use by the client.
        Debug.Log("Firebase Handler: Firebase initialization succcessful. Firebase is ready to be used by the client.");
        this.IsReadyForUse = true;

        if(FirebaseInitialized != null)
        {
            FirebaseInitialized.Invoke();
        }
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

                isUserSignedIn = false;

                if(UserSignedOut != null)
                {
                    UserSignedOut.Invoke("");
                }
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
                userId = user.UserId;
                phoneNumber = user.PhoneNumber;
                providerId = user.ProviderId;
                userMetadata = user.Metadata;

                isUserSignedIn = true;

                if(UserSignedIn != null)
                {
                    UserSignedIn.Invoke("");
                }
                
                GetToken();
            }
        }
    }

    // Method for registering a user with a basic email and password pair.
    public void RegisterUser(string email, string password)
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

            if(NewUserRegistered != null)
            {
                NewUserRegistered.Invoke("");
            }
        });
    }

    // Signs a user into firebase using a given email and password.
    public void SignInUser(string email, string password)
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

    // Signs a user out of firebase.
    public void SignOutUser()
    {
        auth.SignOut();
    }

    // Launches a task which gets a user token generated by firebase.
    public void GetToken()
    {
        user.TokenAsync(true).ContinueWith(task => {
            if(task.IsCanceled)
            {
                Debug.LogError("GetToken(): TokenAsync was canceled.");
                return;
            }
            
            if(task.IsFaulted)
            {
                Debug.LogErrorFormat(this, "TokenAsync encountered an error: {0}", task.Exception);
                return;
            }

            userToken = task.Result;
            Debug.LogFormat(this, "User token retrieved: {0}", userToken);

            if(UserTokenReceived != null)
            {
                UserTokenReceived.Invoke(userToken);
            }
        });
    }
}
