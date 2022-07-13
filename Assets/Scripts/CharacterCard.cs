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

    // The attack type of the character, used for filtering and showing the right type symbol.
    public Image characterType;

    // The character's full name.
    public TMP_Text characterName;

    // The character's current level.
    public TMP_Text characterLevel;

    // The parsed character information retrieved from the server.
    public CharacterListUIController.Character character;

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

                // Get the character's attack type and set the type icon sprite.

                // Set the character level text.
                characterLevel.text = this.character.currentLevel;

                // Get the character's skill information and populate the skill list.

                // Display the character's star rating.
            }
        }
    }
}
