using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class SkillIcon : MonoBehaviour
{
    // The skill's art.
    public Image skillImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Given a skill's content id, this method sets this object up for displaying a skill's art and other information.
    public void SetSkill(string id)
    {
        // Find the skill in the cache and use its data to display useful information.
        foreach(JToken cachedSkill in GameDataCache.Instance.parsedGameData["skills"].Children())
        {
            if (id == cachedSkill["id"].Value<string>())
            {
                Debug.LogFormat(this, "Skill match: {0}", cachedSkill["name"].Value<string>());

                // Use the skill art name to get the skill's icon from the art cache, then create and set the sprite.
                string artName = cachedSkill["art"].Value<string>();

                if(artName != "")
                {
                    Debug.LogFormat("Skill Icon: Using name '{0}' to create a new sprite.", artName);
                    WebAssetCache.LoadedImageAsset asset = WebAssetCache.Instance.GetLoadedImageAssetByName(artName);

                    if(asset != null)
                    {
                        Sprite newSprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);
                        skillImage.sprite = newSprite;
                    }
                    else
                    {
                        // If we get a null reference back then the art doesn't exist in the cache (by the given name, anyway).
                        Debug.LogErrorFormat(this, "Skill Icon: There was a problem retrieving the texture '{0}' from the asset cache.", artName);
                    }
                }
                else
                {
                    // There was no art listed in the game data. Use a default sprite instead.
                    Debug.LogErrorFormat(this, "Skill Icon: No art name was given for the attachment content. Using default sprite.");
                }
            }
        }
    }
}
