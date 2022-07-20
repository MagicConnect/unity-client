using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BestHTTP;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CharacterDetailsUIController : MonoBehaviour
{
    // TODO: The Character and Essences class definitions are identical to the ones in the Character List controller.
    // It would make a lot more sense to have JSON parsing class definitions defined in a separate script/file,
    // where they can all be shared between the different scripts (especially since the definitions mirror
    // standardized schemas on the backend and will be reused again and again).

    // For some dumb reason the JSON parser needs this to properly parse a response.
    public class ParsedCharacter
    {
        public Character character;
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

    FirebaseHandler firebase = FirebaseHandler.Instance;

    public TMP_Text nameText;

    public TMP_Text levelText;

    public TMP_Text weaponTypeText;

    public TMP_Text archetypeText;

    // Text objects for displaying essences/stats.
    public TMP_Text attackText;
    public TMP_Text specialAttackText;
    public TMP_Text magicText;
    public TMP_Text hpText;
    public TMP_Text defenseText;
    public TMP_Text criticalText;

    public Image characterArt;

    // The horizontal layout where the star sprites are displayed.
    public GameObject starContainer;

    // Prefabs for the star sprites displayed for a character's star rating.
    public GameObject fullStarPrefab;
    public GameObject emptyStarPrefab;

    // The layout object where skills will be displayed.
    public GameObject skillListContainer;

    // Prefab of the skill icons to be displayed in the skill list.
    public GameObject skillIconPrefab;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ParseCharacterResponse(string data)
    {
        // Parse the JSON response into usable information that can be displayed.
        Character serverData = JsonConvert.DeserializeObject<ParsedCharacter>(data).character;

        // Use the current content id to find the character information in the cached game data.
        JToken characters = GameDataCache.Instance.parsedGameData["characters"];

        foreach(JToken character in characters.Children())
        {
            if(character["id"].Value<string>() == serverData.contentId)
            {
                // Use the character art name to get the character's profile art from the art cache, then create and set the sprite.
                string artName = character["art"].Value<string>();

                if(artName != "")
                {
                    Debug.LogFormat("Character Details: Using name '{0}' to create a new sprite.", artName);
                    WebAssetCache.LoadedImageAsset asset = WebAssetCache.Instance.GetLoadedImageAssetByName(artName);

                    if(asset != null)
                    {
                        Sprite newSprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);
                        characterArt.sprite = newSprite;
                    }
                    else
                    {
                        // If we get a null reference back then the art doesn't exist in the cache (by the given name, anyway).
                        Debug.LogErrorFormat(this, "Character Details: There was a problem retrieving the texture '{0}' from the asset cache.", artName);
                    }
                }
                else
                {
                    // There was no art listed in the game data. Use a default sprite instead.
                    Debug.LogErrorFormat(this, "Character Details: No art name was given for the attachment content. Using default sprite.");
                }

                // Set the character's basic information (name, level, etc.).
                nameText.text = character["name"].Value<string>();
                levelText.text = string.Format("Level: {0}", serverData.currentLevel);
                weaponTypeText.text = character["weapon"].Value<string>();
                archetypeText.text = character["archetype"].Value<string>();

                // Set the character's stats/essences.
                // TODO: Once the character response is updated replace this with the calculated stat values from the server.
                attackText.text = string.Format("{0}", serverData.currentEssences.attack);
                specialAttackText.text = string.Format("{0}", serverData.currentEssences.special);
                magicText.text = string.Format("{0}", serverData.currentEssences.magic);
                hpText.text = string.Format("{0}", serverData.currentEssences.hp);
                defenseText.text = string.Format("{0}", serverData.currentEssences.defense);
                criticalText.text = string.Format("{0}", serverData.currentEssences.critical);

                // Clear the skill list of any preexisting skill icons.
                foreach(Transform skill in skillListContainer.transform)
                {
                    Destroy(skill.gameObject);
                }

                // Get character skill information/art and display it.
                foreach(JToken characterSkill in character["skills"].Children())
                {
                    Debug.LogFormat(this, "{0}", characterSkill["name"].Value<string>());

                    // Instantiate a new skill icon object and pass it the skill's content id.
                    GameObject skillIcon = Instantiate(skillIconPrefab, skillListContainer.transform);
                    skillIcon.GetComponent<SkillIcon>().SetSkill(characterSkill["name"].Value<string>());
                }

                // Display the character's limit break level.

                /*
                // Get the character's archetype and set the type icon sprite.
                string archetype = character["archetype"].Value<string>();

                switch(archetype)
                {
                    case "Healer": 
                        archetypeIcon.sprite = characterList.healerIcon;
                        this.archetype = CharacterListUIController.Archetype.Healer;
                        break;
                    case "Defender": 
                        archetypeIcon.sprite = characterList.defenderIcon;
                        this.archetype = CharacterListUIController.Archetype.Defender;
                        break;
                    case "Caster": 
                        archetypeIcon.sprite = characterList.casterIcon;
                        this.archetype = CharacterListUIController.Archetype.Caster;
                        break;
                    case "Attacker": 
                        archetypeIcon.sprite = characterList.attackerIcon;
                        this.archetype = CharacterListUIController.Archetype.Attacker;
                        break;
                    case "Ranger": 
                        archetypeIcon.sprite = characterList.rangerIcon; 
                        this.archetype = CharacterListUIController.Archetype.Ranger;
                        break;
                    default: break;
                }
                */

                // Display the character's star rating.

                // Clear the star container if it isn't empty.
                foreach(Transform star in starContainer.transform)
                {
                    Destroy(star.gameObject);
                }

                int rating = character["stars"].Value<int>();

                // Spawn full stars to match the character's rating.
                for(int i = 0; i < rating; i += 1)
                {
                    Instantiate(fullStarPrefab, starContainer.transform);
                }

                // If the rating is less than 5, spawn empty stars to fill out the graphic.
                for(int i = 0; i < 5 - rating; i += 1)
                {
                    Instantiate(emptyStarPrefab, starContainer.transform);
                }
            }
        }
    }

    // Given a character's id, request relevant information from the server and the cache so it can be displayed.
    public void LoadCharacterDetails(string characterId)
    {
        // Send a /users/{userId}/characters/{characterId} request to the server.
        string userId = PlayerPrefs.GetString("user_id", "");
        if(userId == "")
        {
            Debug.LogErrorFormat(this, "Character Details Screen: The 'user_id' setting couldn't be found in PlayerPrefs. Unable to send a character request.");
            return;
        }

        string formattedUrl = string.Format("http://testserver.magic-connect.com/users/{0}/characters/{1}", userId, characterId);
        HTTPRequest request = new HTTPRequest(new Uri(formattedUrl), HTTPMethods.Get, OnCharacterRequestFinished);

        request.AddHeader("Authorization", string.Format("Bearer {0}", firebase.userToken));

        request.Send();
    }

    public void OnCharacterRequestFinished(HTTPRequest request, HTTPResponse response)
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

                    ParseCharacterResponse(response.DataAsText);
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
}
