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
        string artName = "";
        string objectName = "";

        //JToken content = null;
        switch(this.type)
        {
            case AttachmentType.Weapon: 
                GameDataCache.Instance.weaponsById.TryGetValue(contentId, out var weapon);
                if(weapon != null)
                {
                    artName = weapon.art;
                    objectName = weapon.name;
                }
                else
                {
                    Debug.LogErrorFormat(this, "Mail Attachment: No weapon exists with this content id -> {0}", contentId);
                }
                break;
            case AttachmentType.Accessory: 
                GameDataCache.Instance.accessoriesById.TryGetValue(contentId, out var accessory);
                if(accessory != null)
                {
                    artName = accessory.art;
                    objectName = accessory.name;
                }
                else
                {
                    Debug.LogErrorFormat(this, "Mail Attachment: No accessory exists with this content id -> {0}", contentId);
                }
                break;
            case AttachmentType.Item: 
                GameDataCache.Instance.itemsById.TryGetValue(contentId, out var item);
                if(item != null)
                {
                    artName = item.art;
                    objectName = item.name;
                }
                else
                {
                    Debug.LogErrorFormat(this, "Mail Attachment: No item exists with this content id -> {0}", contentId);
                }
                break;
            case AttachmentType.Character: 
                GameDataCache.Instance.charactersById.TryGetValue(contentId, out var character);
                if(character != null)
                {
                    artName = character.art;
                    objectName = character.name;
                }
                else
                {
                    Debug.LogErrorFormat(this, "Mail Attachment: No character exists with this content id -> {0}", contentId);
                }
                break;
        }

        // Get the name of the content and use it to set the label text.
        attachmentName.text = string.Format("{0} x{1}", objectName, this.quantity);

        if(artName != "")
        {
            Debug.LogFormat("Mail Attachment: Using name '{0}' to create a new sprite.", artName);
            WebAssetCache.LoadedImageAsset asset = WebAssetCache.Instance.GetLoadedImageAssetByName(artName);

            if (asset != null)
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
            Debug.LogWarningFormat(this, "Mail Attachment: No art name was given for the attachment content. Using default sprite.");
        }
    }
}
