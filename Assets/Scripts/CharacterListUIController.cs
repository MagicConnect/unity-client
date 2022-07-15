using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using BestHTTP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CharacterListUIController : MonoBehaviour
{
    // The array of parsed character information downloaded from the server.
    public class CharacterList
    {
        public Character[] characters;
    }

    // Information about a character provided by the server.
    public class Character
    {
        public string id;

        public string contentId;

        public string currentXp;

        public string currentLevel;

        public string currentLb;

        public string currentDupe;

        public string activeDupe;

        public string currentUserWeaponIdEquipped;

        public Essences currentEssences;
    }

    // A character's parsed essence information provided by the server.
    public class Essences
    {
        public int hp;

        public int attack;

        public int defense;

        public int magic;

        public int special;

        public int critical;
    }

    // The possible archetypes the characters can be. Used for sorting the character list.
    public enum Archetype
    {
        Attacker,
        Healer,
        Defender,
        Caster,
        Ranger
    }

    // The firebase handler which we get our authentication token from.
    FirebaseHandler firebase = FirebaseHandler.Instance;

    // Reference to the character screen controller so this component can pass along important information.
    public CharacterScreenUIController characterScreenController;

    // The list content gameobject which will all character cards will be attached to.
    public GameObject characterListContainer;

    // The prefab of the character cards that will be cloned for the character list.
    public GameObject characterCardPrefab;

    // The most up to date character list returned from the server.
    public CharacterList mostRecentCharacterList;

    // The character list background image.
    public Image backgroundImage;

    // Sprite references the character card will use for displaying the archetype.
    public Sprite healerIcon;
    public Sprite attackerIcon;
    public Sprite defenderIcon;
    public Sprite casterIcon;
    public Sprite rangerIcon;

    // Sprite references for the star rating each character card will display.
    public Sprite starFull;
    public Sprite starGray;

    void Awake()
    {}

    // Start is called before the first frame update
    void Start()
    {
        SendMeCharactersRequest();

        // Set the default background image based on the design.
        WebAssetCache.LoadedImageAsset imageAsset = WebAssetCache.Instance.GetLoadedImageAssetByName("TempleBase");

        if(imageAsset != null)
        {
            Sprite newSprite = Sprite.Create(imageAsset.texture, new Rect(0.0f, 0.0f, imageAsset.texture.width, imageAsset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);
            backgroundImage.sprite = newSprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Uses the most recent character list information to populate the character list with character cards.
    public void RefreshCharacterList()
    {
        // Clear the character list ui of outdated cards.
        // TODO: Instead of deleting everything, recycle old cards and/or update existing ones.
        foreach(Transform card in characterListContainer.transform)
        {
            Destroy(card.gameObject);
        }

        // Create character cards for every character on the list.
        foreach(Character character in mostRecentCharacterList.characters)
        {
            // Instantiate a copy of the character card prefab.
            GameObject newCharacterCard = Instantiate(characterCardPrefab, characterListContainer.transform);

            // Give the character card the character information it needs to work with. The card will handle its own animations.
            CharacterCard characterCard = newCharacterCard.GetComponent<CharacterCard>();
            characterCard.characterList = this;
            characterCard.SetCharacter(character);

            // Pass along the card to the character screen controller so it can subscribe to its events.
            characterScreenController.SubscribeToCardEvents(characterCard);
        }
    }

    // Parse a /me/characters response into objects that we can use.
    public void ParseCharactersResponse(string data)
    {
        // Allow the JSON plugin to parse the information for us.
        CharacterList characterList = JsonConvert.DeserializeObject<CharacterList>(data);

        // Check the character list object to make sure everything was parsed correctly. If there was a problem parsing essential information,
        // log an error so it can be fixed.
        if(characterList != null)
        {
            if(characterList.characters == null)
            {
                Debug.LogErrorFormat(this, "Character List: There was a problem parsing the character list information retrieved from the server. JSON: {0}", data);
                return;
            }
            else
            {
                // If everything was parsed okay, use the character list to update the ui.
                mostRecentCharacterList = characterList;
                RefreshCharacterList();
            }
        }
    }

    // Make a /me/characters request to the server.
    public void SendMeCharactersRequest()
    {
        // Send a /me/characters request to the server.
        HTTPRequest request = new HTTPRequest(new Uri("http://testserver.magic-connect.com/me/characters"), OnMeCharactersRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
    }

    // Gets the results of the me/characters request which will be used to create the character list.
    public void OnMeCharactersRequestFinished(HTTPRequest request, HTTPResponse response)
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

                    ParseCharactersResponse(response.DataAsText);
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

    public void OnCharacterCardSelected(string contentId)
    {
        
    }

    public void OnAllArchetypesClicked()
    {
        // Activate all character cards in the character list since there is no filter.
        foreach(Transform card in characterListContainer.transform)
        {
            card.gameObject.SetActive(true);
        }
    }

    // Tells the filter method to filter based on the attacker archetype.
    public void OnAttackerFilterClicked()
    {
        FilterCharacterList(Archetype.Attacker);
    }

    // Tells the filter method to filter based on the defender archetype.
    public void OnDefenderFilterClicked()
    {
        FilterCharacterList(Archetype.Defender);
    }

    // Tells the filter method to filter based on the ranger archetype.
    public void OnRangerFilterClicked()
    {
        FilterCharacterList(Archetype.Ranger);
    }

    // Tells the filter method to filter based on the healer archetype.
    public void OnHealerArchetypeClicked()
    {
        FilterCharacterList(Archetype.Healer);
    }

    // Tells the filter method to filter based on the caster archetype.
    public void OnCasterArchetypeClicked()
    {
        FilterCharacterList(Archetype.Caster);
    }

    // Shows/hides character cards based on the attack type filter chosen.
    public void FilterCharacterList(Archetype archetype)
    {
        foreach(Transform card in characterListContainer.transform)
        {
            // Activate the character card if it is the chosen archetype, otherwise deactivate it.
            if(card.gameObject.GetComponent<CharacterCard>().archetype == archetype)
            {
                card.gameObject.SetActive(true);
            }
            else
            {
                card.gameObject.SetActive(false);
            }
        }
    }
}
