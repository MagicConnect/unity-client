using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class CharacterCard : MonoBehaviour
{
    // The character headshot/bodyshot.
    public Image characterArt;

    // The sprite representing the character's archetype.
    public Image archetypeIcon;

    // The character's full name.
    public TMP_Text characterName;

    // The character's current level.
    public TMP_Text characterLevel;

    // The parsed character information retrieved from the server.
    public CharacterListUIController.Character character;

    // The reference to the character list controller, where we can find useful assets like the archetype sprites.
    public CharacterListUIController characterList;

    // These are the stars that will be spawned into the star rating area.
    public GameObject fullStarPrefab;
    public GameObject emptyStarPrefab;

    // The star rating layout group where the stars will be arranged.
    public GameObject starContainer;

    // The character's archetype information from the server. Used for filtering cards in the character list.
    public CharacterListUIController.Archetype archetype;

    // Click event that fires when the character card is clicked. The character list ui controller
    // will handle changing screens, so this card needs to let it know.
    public event Action<string> OnCharacterCardClicked;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // When the character card is created, this method should be used to give it the information needed to display/animate.
    public void SetCharacter(CharacterListUIController.Character character)
    {
        this.character = character;

        RefreshUi();
    }

    // Refreshes the character card ui elements to reflect any changes in data.
    public void RefreshUi()
    {
        // Use the current content id to find the character information in the cached game data.
        JToken characters = GameDataCache.Instance.parsedGameData["characters"];

        foreach(JToken character in characters.Children())
        {
            if(character["id"].Value<string>() == this.character.contentId)
            {
                // Use the character art name to get the character's headshot from the art cache, then create and set the sprite.
                string artName = character["headArt"].Value<string>();

                if(artName != "")
                {
                    Debug.LogFormat("Character Card: Using name '{0}' to create a new sprite.", artName);
                    WebAssetCache.LoadedImageAsset asset = WebAssetCache.Instance.GetLoadedImageAssetByName(artName);

                    if(asset != null)
                    {
                        Sprite newSprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);
                        characterArt.sprite = newSprite;
                    }
                    else
                    {
                        // If we get a null reference back then the art doesn't exist in the cache (by the given name, anyway).
                        Debug.LogErrorFormat(this, "Character Card: There was a problem retrieving the texture '{0}' from the asset cache.", artName);
                    }
                }
                else
                {
                    // There was no art listed in the game data. Use a default sprite instead.
                    Debug.LogErrorFormat(this, "Character Art: No art name was given for the attachment content. Using default sprite.");
                }

                // Set the character name text.
                characterName.text = character["name"].Value<string>().ToUpper();

                // Get the character's archetype and set the type icon sprite.
                string archetype = character["archetype"].Value<string>();

                Debug.LogFormat(this, "Character Card: {0}: Archetype = {1}", character["name"].Value<string>(), archetype);

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

                // Set the character level text.
                characterLevel.text = this.character.currentLevel;

                // Display the character's star rating.
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

    public void OnCardClicked()
    {
        if(OnCharacterCardClicked != null)
        {
            OnCharacterCardClicked.Invoke(character.id);
        }
    }
}
