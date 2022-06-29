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
    //public TMP_InputField accessoryId;

    //public TMP_InputField characterId;

    //public TMP_InputField shopId;

    //public TMP_InputField weaponId;

    // These are the dropdown ui objects where the 'contentId' is set by the user.
    public TMP_Dropdown accessoryIdDropdown;

    public TMP_Dropdown characterIdDropdown;

    public TMP_Dropdown shopIdDropdown;

    public TMP_Dropdown weaponIdDropdown;

    // When the name of an accessory, shop, etc. is chosen in the dropdown list, these dictionaries
    // can be used to translate that into a content id that would be sent to the server as part of the api.
    public Dictionary<string, string> accessoryContentIds = new Dictionary<string, string>();

    public Dictionary<string, string> shopContentIds = new Dictionary<string, string>();

    public Dictionary<string, string> characterContentIds = new Dictionary<string, string>();

    public Dictionary<string, string> weaponContentIds = new Dictionary<string, string>();

    // Start is called before the first frame update
    void Start()
    {
        // Get the accessory information from the gamedatacache.
        try
        {
            var accessories = GameDataCache.Instance.parsedGameData["accessories"];

            // If there's a better way to convert a JToken into a list, do it. Otherwise this is all I got.
            List<string> accessoryIds = new List<string>();
            foreach (var accessory in accessories)
            {
                //accessoryIds.Add(string.Format("{0}: {1}", accessory["name"].ToString(), accessory["id"].ToString()));
                accessoryIds.Add(accessory["name"].ToString());
                accessoryContentIds.Add(accessory["name"].ToString(), accessory["id"].ToString());
            }

            // Populate the dropdown ui with all the available accessories that can be added.
            accessoryIdDropdown.ClearOptions();
            accessoryIdDropdown.AddOptions(accessoryIds);
        }
        catch(Exception e)
        {
            Debug.LogErrorFormat("GmScreenUIController: Problem occurred while getting accessory information from gamedatacache -> {0}", e);
        }

        // Get the character information from the gamedatacache.
        try
        {
            var characters = GameDataCache.Instance.parsedGameData["characters"];

            // If there's a better way to convert a JToken into a list, do it. Otherwise this is all I got.
            List<string> characterIds = new List<string>();
            foreach (var character in characters)
            {
                //characterIds.Add(string.Format("{0}: {1}", character["name"].ToString(), character["id"].ToString()));
                characterIds.Add(character["name"].ToString());
                characterContentIds.Add(character["name"].ToString(), character["id"].ToString());
            }

            // Populate the dropdown ui with all the available characters that can be added.
            characterIdDropdown.ClearOptions();
            characterIdDropdown.AddOptions(characterIds);
        }
        catch(Exception e)
        {
            Debug.LogErrorFormat("GmScreenUIController: Problem occurred while getting character information from gamedatacache -> {0}", e);
        }

        // Get the shop information from the gamedatacache.
        try
        {
            var shops = GameDataCache.Instance.parsedGameData["shops"];

            // If there's a better way to convert a JToken into a list, do it. Otherwise this is all I got.
            List<string> shopIds = new List<string>();
            foreach (var shop in shops)
            {
                //shopIds.Add(string.Format("{0}: {1}", shop["name"].ToString(), shop["id"].ToString()));
                shopIds.Add(shop["name"].ToString());
                shopContentIds.Add(shop["name"].ToString(), shop["id"].ToString());
            }

            // Populate the dropdown ui with all the available shops that can be added.
            shopIdDropdown.ClearOptions();
            shopIdDropdown.AddOptions(shopIds);
        }
        catch(Exception e)
        {
            Debug.LogErrorFormat("GmScreenUIController: Problem occurred while getting shop information from gamedatacache -> {0}", e);
        }

        // Get the weapon information from the gamedatacache.
        try
        {
            var weapons = GameDataCache.Instance.parsedGameData["weapons"];

            // If there's a better way to convert a JToken into a list, do it. Otherwise this is all I got.
            List<string> weaponIds = new List<string>();
            foreach (var weapon in weapons)
            {
                //weaponIds.Add(string.Format("{0}: {1}", weapon["name"].ToString(), weapon["id"].ToString()));
                weaponIds.Add(weapon["name"].ToString());
                weaponContentIds.Add(weapon["name"].ToString(), weapon["id"].ToString());
            }

            // Populate the dropdown ui with all the available weapons that can be added.
            weaponIdDropdown.ClearOptions();
            weaponIdDropdown.AddOptions(weaponIds);
        }
        catch(Exception e)
        {
            Debug.LogErrorFormat("GmScreenUIController: Problem occurred while getting shop information from gamedatacache -> {0}", e);
        }
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
        request.AddField("contentId", accessoryContentIds[accessoryIdDropdown.itemText.text]);

        request.Send();
    }

    public void OnGmAddCharacterButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-character"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", characterContentIds[characterIdDropdown.itemText.text]);

        request.Send();
    }

    public void OnGmAddShopButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-shop"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", shopContentIds[shopIdDropdown.itemText.text]);

        request.Send();
    }

    public void OnGmAddWeaponButtonClicked()
    {
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/gm/debug/add-weapon"), OnGetResponseReceived);

        request.AddHeader("Authorization", string.Format("Bearer {0}", FirebaseHandler.Instance.userToken));
        request.AddField("contentId", weaponContentIds[weaponIdDropdown.itemText.text]);

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
