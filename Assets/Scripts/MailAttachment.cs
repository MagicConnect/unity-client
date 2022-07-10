using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class MailAttachment : MonoBehaviour
{
    // The different types of content this attachment can be. Used for determining which part of the content
    // database the item should be searched for.
    public enum AttachmentType
    {
        Weapon,
        Character,
        Accessory,
        Item
    }

    // The type of the attachment, defined above.
    public AttachmentType type;

    // The id of the object attached to the mail. Used to find the object in the content database.
    public string contentId;

    // How many of this item is attached to the mail.
    public int quantity;

    // The name of the content attached to the mail.
    public TMP_Text attachmentName;

    // The sprite art of the content attached to the mail.
    public Image attachmentImage;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // The attachment information passed in here is used to search it in the database, and update the ui
    // using the cached art and game data.
    public void SetData(string contentId, int quantity, AttachmentType type)
    {
        Debug.LogFormat("Mail Attachment: Content Id: {0} Quantity: {1}", contentId, quantity);
        this.contentId = contentId;
        this.quantity = quantity;
        this.type = type;

        // Use the content id to search the gamedata cache.
        JToken content = null;
        switch(this.type)
        {
            case AttachmentType.Weapon: content = GameDataCache.Instance.parsedGameData["weapons"]; break;
            case AttachmentType.Accessory: content = GameDataCache.Instance.parsedGameData["accessories"]; break;
            case AttachmentType.Item: content = GameDataCache.Instance.parsedGameData["items"]; break;
            case AttachmentType.Character: content = GameDataCache.Instance.parsedGameData["characters"]; break;
        }

        foreach(JToken contentObject in content.Children())
        {
            if(contentObject["id"].Value<string>() == this.contentId)
            {
                Debug.LogFormat("Content Object: {0}", contentObject.ToString());

                // Get the name of the content and use it to set the label text.
                attachmentName.text = string.Format("{0} x{1}", contentObject["name"].Value<string>(), this.quantity);

                // Get the name of the sprite to be used and then retrieve it from the web asset cache.
                string artName = contentObject["art"].Value<string>();

                if(artName != "")
                {
                    Debug.LogFormat("Mail Attachment: Using name '{0}' to create a new sprite.", artName);
                    WebAssetCache.LoadedImageAsset asset = WebAssetCache.Instance.GetLoadedImageAssetByName(artName);

                    if(asset != null)
                    {
                        Sprite newSprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);
                        attachmentImage.sprite = newSprite;
                    }
                    else
                    {
                        // If we get a null reference back then the art doesn't exist in the cache (by the given name, anyway).
                        Debug.LogErrorFormat(this, "Mail Attachment: There was a problem retrieving the texture '{0}' from the asset cache.", artName);
                    }
                }
                else
                {
                    // There was no art listed in the game data. Use a default sprite instead.
                    Debug.LogErrorFormat(this, "Mail Attachment: No art name was given for the attachment content. Using default sprite.");
                }
            }
        }
    }
}
