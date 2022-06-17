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

    // The user's authentication token used for making API calls to the game server.
    public string userToken = "";

    // Flag which indicates that Firebase is ready to be used by the client.
    public bool IsReadyForUse = false;

    // Used while testing to make sure we connect to the right Firebase project.
    // TODO: Replace with some custom precompiler definition(s) and ifdef blocks.
    public bool UseTestFirebaseProject = true;

    // Will be true if the user is currently signed in to the Firebase server.
    public bool isUserSignedIn = false;

    // Will be true if a user session was carried over from the last time the app was opened.
    public bool previousSignInFound = false;

    // Events which tell listeners about changes in the Firebase state.
    public event Action FirebaseInitialized;

    public event Action<string> UserTokenReceived;

    public event Action UserSignedIn;

    public event Action UserSignedOut;

    public event Action NewUserRegistered;

    public event Action NoPreviousSignInFound;

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

    public void InitializeFirebase()
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

        if(auth.CurrentUser == null)
        {
            Debug.LogFormat(this, "Firebase Handler: No preexisting user session found. User will have to sign in manually.");
            previousSignInFound = false;
        }
        else
        {
            Debug.LogFormat(this, "Firebase Handler: Preexisting user session found. User will be signed in automatically.");
            previousSignInFound = true;
        }

        //app = Firebase.FirebaseApp.DefaultInstance;
        //auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

        // Set a flag to indicate whether Firebase is ready to use by the client.
        Debug.Log("Firebase Handler: Firebase initialization successful. Firebase is ready to be used by the client.");
        this.IsReadyForUse = true;

        if(FirebaseInitialized != null)
        {
            FirebaseInitialized.Invoke();
        }

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    // Event handler for when state changes for the signed in user. Includes sign-in and sign-out events. It is also the best place to get
    // information about the signed in user (according to the Firebase tutorial).
    public void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        Debug.LogFormat(this, "Firebase Handler: Auth State Changed -> Sender: {0} EventArgs: {1}", sender, eventArgs.ToString());
        
        if(auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;
            if(!signedIn && user != null)
            {
                Debug.LogFormat("Signed out {0}", user.UserId);

                isUserSignedIn = false;

                if(UserSignedOut != null)
                {
                    UserSignedOut.Invoke();
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
                    UserSignedIn.Invoke();
                }
                
                GetToken();
            }
        }

        // For some reason firebase is silently crashing, hanging, or just outright stopping execution. There are no exceptions being
        // thrown or outputs in the log. It just stops.
        Debug.LogFormat(this, "Firebase Handler: Exiting AuthStateChanged method. CurrentUser: {0}", auth.CurrentUser);
    }

    // Method for registering a user with a basic email and password pair.
    public void RegisterUser(string email, string password)
    {
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task =>{
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
                NewUserRegistered.Invoke();
            }
        });
    }

    // Signs a user into firebase using a given email and password.
    public void SignInUser(string email, string password)
    {
        Debug.LogFormat(this, "Firebase Handler: Attempting to sign into Firebase using email ({0}) and password ({1}).", email, password);

        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
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
        Debug.LogFormat(this, "Firebase Handler: Attempting to sign out the current user.");

        auth.SignOut();
    }

    // Launches a task which gets a user token generated by firebase.
    public void GetToken()
    {
        Debug.LogFormat(this, "Firebase Handler: Requesting user token.");

        user.TokenAsync(true).ContinueWithOnMainThread(task => {
            if(task.IsCanceled)
            {
                Debug.LogErrorFormat(this, "Firebase Handler: GetToken(): TokenAsync was canceled.");
                return;
            }
            
            if(task.IsFaulted)
            {
                Debug.LogErrorFormat(this, "Firebase Handler: TokenAsync encountered an error: {0}", task.Exception);
                return;
            }

            userToken = task.Result;
            Debug.LogFormat(this, "Firebase Handler: User token retrieved: {0}", userToken);

            if(UserTokenReceived != null)
            {
                UserTokenReceived.Invoke(userToken);
            }
        });
    }
}
