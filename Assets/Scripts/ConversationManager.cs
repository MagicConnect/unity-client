using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Yarn.Unity;

public class ConversationManager : MonoBehaviour
{
    // The prefab of the character object to be cloned.
    public GameObject characterPrefab;

    // This list will keep track of the characters spawned by the manager so it doesn't have to search for them later.
    public List<GameObject> characters;

    // Just a helpful reference to the parent of all instantiated character and npc gameobjects.
    public GameObject characterContainer;

    // Reference to the background image that will display during the conversation.
    public static GameObject staticBackground;

    void Awake()
    {
        // Create character objects from the loaded character assets.
        // The cache should be loaded before the game ever gets here but it doesn't hurt to check.
        if(WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse)
        {
            List<WebAssetCache.LoadedImageAsset> characterAssets = WebAssetCache.Instance.GetLoadedAssetsByCategory("characters");
            List<WebAssetCache.LoadedImageAsset> npcAssets = WebAssetCache.Instance.GetLoadedAssetsByCategory("npcs");
            List<WebAssetCache.LoadedImageAsset> assets = characterAssets.Concat(npcAssets).ToList();

            if(assets.Count <= 0)
            {
                Debug.LogWarningFormat("No loaded assets returned by cache.");
            }

            foreach(WebAssetCache.LoadedImageAsset asset in assets)
            {
                GameObject newCharacter = Instantiate(characterPrefab, characterContainer.transform);

                newCharacter.name = asset.name;
                newCharacter.GetComponent<Image>().sprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f);

                characters.Add(newCharacter);
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
        // Find the background gameobject.
        staticBackground = GameObject.Find("ConversationBackground");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [YarnCommand("change_background_image")]
    public static void SetBackgroundImage(string name)
    {
        staticBackground?.GetComponent<ConversationBackground>().ChangeBackgroundImage(name);
    }

    [YarnCommand("change_background_color")]
    public static void SetBackgroundColor(float r, float g, float b, float a = 1.0f)
    {
        Color color = new Color(r, g, b, a);
        staticBackground?.GetComponent<ConversationBackground>().ChangeBackgroundColor(color);
    }

    //[YarnCommand("change_background_transparency")]
    public static void SetBackgroundAlpha(int a)
    {

    }
}
