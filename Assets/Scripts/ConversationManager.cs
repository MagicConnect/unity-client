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

    public static ConversationManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        characters = new List<GameObject>();

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
                // Note: SpriteMeshType.FullRect loads faster because it creates a simple quad, but has worse runtime performance. 
                // SpriteMeshType.Tight incurs a huge loading time penalty with high resolution assets though so this'll have to be configured/benchmarked later.
                newCharacter.GetComponent<Image>().sprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);

                characters.Add(newCharacter);
                Debug.LogFormat("Added character/npc '{0}' to game.", asset.name);
            }
        }
        else
        {
            Debug.LogErrorFormat("Attempted to load conversation background when cache was not fully loaded.");
        }

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

    // To make it easier for the writers, this method allows dimming multiple characters at once.
    // Note: Yarn Spinner doesn't support arrays as arguments, so I can't make this into a GameObject[].
    // As far as I know, it has to be this awful.
    [YarnCommand("dim_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, GameObject c6 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        List<ConversationCharacter> charactersToDim = new List<ConversationCharacter>();

        if(c1)
        {
            ConversationCharacter character = c1.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be dimmed.", c1.name);
            }
        }

        if(c2)
        {
            ConversationCharacter character = c2.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be dimmed.", c2.name);
            }
        }

        if(c3)
        {
            ConversationCharacter character = c3.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be dimmed.", c3.name);
            }
        }

        if(c4)
        {
            ConversationCharacter character = c4.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be dimmed.", c4.name);
            }
        }

        if(c5)
        {
            ConversationCharacter character = c5.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be dimmed.", c5.name);
            }
        }

        if(c6)
        {
            ConversationCharacter character = c6.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be dimmed.", c6.name);
            }
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => {
                foreach(ConversationCharacter character in charactersToDim)
                {
                    if(character.isDimming)
                    {
                        return false;
                    }
                }
                return true;
            });
        }
    }

    [YarnCommand("dim_two_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, null, null, null, null, timeToComplete, waitForAnimation));
    }

    [YarnCommand("dim_three_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, c3, null, null, null, timeToComplete, waitForAnimation));
    }

    [YarnCommand("dim_four_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, c3, c4, null, null, timeToComplete, waitForAnimation));
    }

    [YarnCommand("dim_five_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, c3, c4, c5, null, timeToComplete, waitForAnimation));
    }

    // Same for 'dim_characters' but in reverse.
    [YarnCommand("undim_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, GameObject c6 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        List<ConversationCharacter> charactersToUndim = new List<ConversationCharacter>();

        if(c1)
        {
            ConversationCharacter character = c1.GetComponent<ConversationCharacter>();

            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be undimmed.", c1.name);
            }
        }

        if(c2)
        {
            ConversationCharacter character = c2.GetComponent<ConversationCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be undimmed.", c2.name);
            }
        }

        if(c3)
        {
            ConversationCharacter character = c3.GetComponent<ConversationCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be undimmed.", c3.name);
            }
        }

        if(c4)
        {
            ConversationCharacter character = c4.GetComponent<ConversationCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be undimmed.", c4.name);
            }
        }

        if(c5)
        {
            ConversationCharacter character = c5.GetComponent<ConversationCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be undimmed.", c5.name);
            }
        }

        if(c6)
        {
            ConversationCharacter character = c6.GetComponent<ConversationCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(timeToComplete));
            }
            else
            {
                Debug.LogErrorFormat("'{0}' is not a character, npc, or item and cannot be undimmed.", c6.name);
            }
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => {
                foreach(ConversationCharacter character in charactersToUndim)
                {
                    if(character.isUndimming)
                    {
                        return false;
                    }
                }
                return true;
            });
        }
    }

    [YarnCommand("undim_two_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, null, null, null, null, timeToComplete, waitForAnimation));
    }

    [YarnCommand("undim_three_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, c3, null, null, null, timeToComplete, waitForAnimation));
    }

    [YarnCommand("undim_four_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, c3, c4, null, null, timeToComplete, waitForAnimation));
    }

    [YarnCommand("undim_five_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, float timeToComplete = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, c3, c4, c5, null, timeToComplete, waitForAnimation));
    }

    // Convenience command for writers to quickly switch which of two characters are dimmed or undimmed. Order of characters given
    // doesn't matter; the method handles that logic automatically.
    [YarnCommand("switch_dimmed_character")]
    public static IEnumerator SwitchDimmedCharacter(GameObject firstCharacter, GameObject secondCharacter, float timeToComplete = 0.2f, bool waitForAnimation = false, bool undimFirstCharacter = false, bool undimSecondCharacter = false)
    {
        // Don't accept null references. If the writer puts in the wrong name or for some reason a character
        // wasn't loaded or something else happens to make this not work, we want to know about it.
        if(!firstCharacter || !secondCharacter)
        {
            if(!firstCharacter)
            {
                Debug.LogErrorFormat("Invalid first parameter for command 'switch_dimmed_character': Yarn Spinner was unable to find a game object by the given name.");
            }

            if(!secondCharacter)
            {
                Debug.LogErrorFormat("Invalid second parameter for command 'switch_dimmed_character': Yarn Spinner was unable to find a game object by the given name.");
            }

            yield break;
        }
        
        // Only actual characters/npcs/whatever can be dimmed or undimmed. If the given object doesn't support the command, abort.
        ConversationCharacter c1 = firstCharacter.GetComponent<ConversationCharacter>();
        ConversationCharacter c2 = secondCharacter.GetComponent<ConversationCharacter>();

        if(!c1 || !c2)
        {
            if(!c1)
            {
                Debug.LogErrorFormat("Invalid parameter for command 'switch_dimmed_character': '{0}' is not an existing character, npc, or item name.", c1.name);
            }

            if(!c2)
            {
                Debug.LogErrorFormat("Invalid parameter for command 'switch_dimmed_character': '{0}' is not an existing character, npc, or item name.", c2.name);
            }
            
            yield break;
        }

        if (c1.isDimmed)
        {
            c1.StartCoroutine(c1.UndimCharacter(timeToComplete));
        }
        else
        {
            if(!undimFirstCharacter)
            {
                c1.StartCoroutine(c1.DimCharacter(timeToComplete));
            }
        }

        if (c2.isDimmed)
        {
            c2.StartCoroutine(c2.UndimCharacter(timeToComplete));
        }
        else
        {
            if(!undimSecondCharacter)
            {
                c2.StartCoroutine(c2.DimCharacter(timeToComplete));
            }
        }

        if (waitForAnimation)
        {
            yield return new WaitUntil(() => !c1.isDimming && !c1.isUndimming && !c2.isDimming && !c2.isUndimming);
        }
    }

    //[YarnCommand("change_background_transparency")]
    public static void SetBackgroundAlpha(int a)
    {

    }
}
