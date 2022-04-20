using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class ConversationBackground : MonoBehaviour
{
    // This holds all the possible background sprites that the writer can choose between, identified by
    // the name of the background in the asset manifest.
    public Dictionary<string, Sprite> backgroundSprites = new Dictionary<string, Sprite>();

    // The image component of this background object.
    private Image image;

    void Awake()
    {
        // Set the image component and configure it for proper display.
        image = GetComponent<Image>();
        image.preserveAspect = true;

        // The cache should be loaded before the game ever gets here but it doesn't hurt to check.
        if(WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse)
        {
            List<WebAssetCache.LoadedImageAsset> assets = WebAssetCache.Instance.GetLoadedAssetsByCategory("backgrounds");

            if(assets.Count <= 0)
            {
                Debug.LogWarningFormat("No loaded assets returned by cache.");
            }

            foreach(WebAssetCache.LoadedImageAsset asset in assets)
            {
                Sprite sprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f);
                backgroundSprites.Add(asset.name, sprite);

                Debug.LogFormat("Loaded background sprite '{0}'.", asset.name);
            }
        }
        else
        {
            Debug.LogErrorFormat("Attempted to load conversation background when cache was not fully loaded.");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Change the background image to a loaded sprite of the given name.
    public void ChangeBackgroundImage(string name)
    {
        if(backgroundSprites.ContainsKey(name))
        {
            image.sprite = backgroundSprites[name];
        }
        else
        {
            Debug.LogErrorFormat("Background sprite named '{0}' does not exist.", name);
        }
    }

    // Change the background image's color to the one given.
    public void ChangeBackgroundColor(Color color)
    {
        image.color = color;
    }
}
