using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BestHTTP;
using TMPro;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MainMenuUIController : MonoBehaviour
{
    public FirebaseHandler firebase;

    // Rename these once you figure out what each currency is.
    public TMP_Text blueCrystalsText;

    public TMP_Text orangeCrystalsText;

    public TMP_Text goldText;

    // The player's display name.
    public TMP_Text usernameText;

    // The player's current level.
    public TMP_Text levelText;

    // The player's current experienced represented as a progress bar.
    public Slider experienceBar;

    // When the user's gm level is greater than or equal to 5, this button appears and allows switching to the gm interface screen.
    public Button gmScreenButton;

    // The prefabs of all interfaces that will be displayed below the main menu at runtime.
    #region UI prefabs
    // The GM screen UI prefab that will be instantiated when the GM screen button is pressed.
    public GameObject gmScreenPrefab;

    public GameObject homeScreenPrefab;

    public GameObject characterListPrefab;

    public GameObject summonScreenPrefab;

    public GameObject inventoryScreenPrefab;

    public GameObject shopScreenPrefab;

    public GameObject mailScreenPrefab;

    public GameObject settingsScreenPrefab;

    public GameObject achievementListPrefab;

    public GameObject friendListPrefab;
    #endregion

    // The loaded instances of each interface prefab created and displayed at runtime.
    #region UI instances
    // The instance of the GM screen interface that has been created at runtime.
    public GameObject gmScreenInstance;

    // The instance of the home screen, which should have been created as a default scene element.
    public GameObject homeScreenInstance;

    public GameObject characterListInstance;

    public GameObject summonScreenInstance;

    public GameObject inventoryScreenInstance;

    public GameObject shopScreenInstance;

    public GameObject mailScreenInstance;

    public GameObject settingsScreenInstance;

    public GameObject achievementListInstance;

    public GameObject friendListInstance;
    #endregion

    // When an interface object is created or made active, it is placed in this object's heirarchy so it can be visible on screen.
    public GameObject screenDisplayContainer;

    // The currently active UI screen displayed underneath the main menu.
    public GameObject currentScreen;

    // The expandable dropdown list that is shown when the 'hamburger' button is pressed. Contains buttons that haven't been designed
    // for yet, or functions that aren't important enough for their own place in the ui.
    public GameObject hamburgerMenu;

    // Start is called before the first frame update
    void Start()
    {
        firebase = FirebaseHandler.Instance;

        // Send a /me request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me"), OnApiRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();

        currentScreen = homeScreenInstance;
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

        // The structure of the JSON response could change over time, so wrap attempts to retrieve a value
        // in try/catch blocks to make sure each exception is caught and reported to be fixed later.

        // Get and store the player's user id. We need this for making certain requests to the server.
        try
        {
            string userId = responseJsonObject["account"]["_id"].Value<string>();
            PlayerPrefs.SetString("user_id", userId);
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving '_id' value -> {0}", e);
        }

        // Get and store the player's account id. I don't know if we need this yet but might as well, just in case.
        try
        {
            string accountId = responseJsonObject["account"]["accountId"].Value<string>();
            PlayerPrefs.SetString("account_id", accountId);
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'accountId' value -> {0}", e);
        }

        // Get and set the player's display name.
        // (this doesn't need to be done each time but we get the displayname already with each response so whatever)
        try
        {
            usernameText.text = responseJsonObject["account"]["name"].Value<string>();
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'name' value -> {0}", e);
            usernameText.text = "???";
        }

        // Get and set the player's current level.
        try
        {
            levelText.text = string.Format("Lvl. {0}", responseJsonObject["account"]["experience"]["level"].Value<string>());
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'level' value -> {0}", e);
            levelText.text = string.Format("Lvl. {0}", 1.ToString());
        }

        // Get the player's current experience and update the experience bar to represent it.
        int currentExperience = 0;
        int maxExperience = 0;
        try
        {
            currentExperience = responseJsonObject["account"]["experience"]["currentXP"].Value<int>();
            maxExperience = responseJsonObject["account"]["experience"]["maxXP"].Value<int>();

            experienceBar.value = (float)currentExperience / (float)maxExperience;
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'currentXP' and 'maxXP' values -> {0}", e);
            experienceBar.value = 0.0f;
        }

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

        // Get the user's gm level and show the gm screen button if it is at or above the required level (5, currently).
        int gmLevel = 0;
        try
        {
            gmLevel = responseJsonObject["account"]["gmLevel"].Value<int>();

            if(gmLevel >= 5)
            {
                gmScreenButton.gameObject.SetActive(true);
            }
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat(this, "Main Menu: Exception occurred while retrieving 'gmLevel' value -> {0}", e);
        }
    }

    public void OnPlayButtonClicked()
    {}

    public void OnHomeButtonClicked()
    {
        // If the home screen ui object is null (it shouldn't be), instantiate a copy of the home screen ui prefab.
        if(!homeScreenInstance)
        {
            homeScreenInstance = Instantiate(homeScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the home screen.
        SwitchToSelectedScreen(homeScreenInstance);
    }

    // I assume the 'Heroes' button is for characters, so load the character list screen.
    public void OnHeroesButtonClicked()
    {
        // If the character list ui object is null, instantiate a copy of the prefab.
        if(!characterListInstance)
        {
            characterListInstance = Instantiate(characterListPrefab, screenDisplayContainer.transform);
        }

        // Switch to the character list screen.
        SwitchToSelectedScreen(characterListInstance);
    }

    // I assume 'Items' refers to the inventory, so load the inventory screen.
    public void OnItemsButtonClicked()
    {
        // If the inventory screen ui object is null, instantiate a copy of the prefab.
        if(!inventoryScreenInstance)
        {
            inventoryScreenInstance = Instantiate(inventoryScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the inventory screen.
        SwitchToSelectedScreen(inventoryScreenInstance);
    }

    // Load the shop screen.
    // Note: The issues imply there's a shop list and a shop buy screen. Just use the one we have and split them later.
    public void OnShopButtonClicked()
    {
        // If the shop screen ui object is null, instantiate a copy of the prefab.
        if(!shopScreenInstance)
        {
            shopScreenInstance = Instantiate(shopScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the shop screen.
        SwitchToSelectedScreen(shopScreenInstance);
    }

    // Load the summon screen.
    public void OnSummonButtonClicked()
    {
        // If the summon screen ui object is null, instantiate a copy of the prefab.
        if(!summonScreenInstance)
        {
            summonScreenInstance = Instantiate(summonScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the summon screen.
        SwitchToSelectedScreen(summonScreenInstance);
    }

    // When the GM screen button is clicked, the GM screen UI is displayed in the center of the screen.
    public void OnGMScreenButtonClicked()
    {
        // If the gm screen ui object is null, then we need to instantiate a copy of the gm screen ui prefab.
        if(!gmScreenInstance)
        {
            gmScreenInstance = Instantiate(gmScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the gm screen.
        SwitchToSelectedScreen(gmScreenInstance);
    }

    // Load the account mail interface.
    public void OnMailButtonClicked()
    {
        // If the mail screen ui object is null, instantiate a copy of the prefab.
        if(!mailScreenInstance)
        {
            mailScreenInstance = Instantiate(mailScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the mail screen.
        SwitchToSelectedScreen(mailScreenInstance);
    }

    // When the 'hamburger' button icon is pressed, expand a dropdown list of other menu buttons.
    public void OnHamburgerMenuButtonClicked()
    {
        if(hamburgerMenu.activeInHierarchy)
        {
            HideHamburgerMenu();
        }
        else
        {
            ShowHamburgerMenu();
        }
    }

    // Load the friend list interface.
    public void OnFriendsButtonClicked()
    {
        // If the friends list ui object is null, instantiate a copy of the prefab.
        if(!friendListInstance)
        {
            friendListInstance = Instantiate(friendListPrefab, screenDisplayContainer.transform);
        }

        // Switch to the friend list.
        SwitchToSelectedScreen(friendListInstance);
    }

    // Load the achievement list interface.
    public void OnAchievementsButtonClicked()
    {
        // If the achievement list ui is null, instantiate a copy of the prefab.
        if(!achievementListInstance)
        {
            achievementListInstance = Instantiate(achievementListPrefab, screenDisplayContainer.transform);
        }

        // Switch to the achievement list.
        SwitchToSelectedScreen(achievementListInstance);
    }

    // Load the settings screen.
    public void OnSettingsButtonClicked()
    {
        // If the settings screen ui object is null, instantiate a copy of the prefab.
        if(!settingsScreenInstance)
        {
            settingsScreenInstance = Instantiate(settingsScreenPrefab, screenDisplayContainer.transform);
        }

        // Switch to the settings screen.
        SwitchToSelectedScreen(settingsScreenInstance);
    }

    public void SwitchToSelectedScreen(GameObject selectedScreen)
    {
        // If the selected screen was changed, update scene information.
        if(currentScreen != selectedScreen)
        {
            selectedScreen.SetActive(true);
            selectedScreen.transform.SetAsLastSibling();

            // Disable the previously active ui screen.
            currentScreen.SetActive(false);

            // Make the new screen ui the currently active ui screen.
            currentScreen = selectedScreen;
        }

        // Hide the dropdown menu.
        HideHamburgerMenu();
    }

    // Displays the dropdown list that should appear when pressing the 'hamburger' menu button.
    // Note: Animations, if necessary, should be handled here until the menu object gets its own script.
    public void ShowHamburgerMenu()
    {
        hamburgerMenu.SetActive(true);
    }

    // Hides the above dropdown list.
    // Note: Animations, if necessary, should be handled here.
    public void HideHamburgerMenu()
    {
        hamburgerMenu.SetActive(false);
    }
}
