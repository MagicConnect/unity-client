using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Yarn.Unity;

public class CutsceneManager : MonoBehaviour
{
    // The prefab of the character object to be cloned.
    public GameObject characterPrefab;

    // The prefab of the speedline UI effect to be spawned.
    public GameObject speedlineEffectPrefab;

    // The prefab of the cutscene object to be spawned.
    public GameObject cutsceneObjectPrefab;

    // The prefab of the background object to be spawned.
    public GameObject backgroundPrefab;

    // The CutsceneUIController component of this game object which we use to interact with the UI.
    public CutsceneUIController uiController;

    // Reference to the dialogue system which is the core of the Yarn Spinner scripting plugin.
    public DialogueRunner dialogueSystem;

    // Just a helpful reference to the parent of all instantiated character and npc gameobjects.
    public GameObject characterContainer;

    // Just a helpful reference to the parent of all weapon, item, and accessory gameobjects.
    // TODO: Possibly combine this with the character container. Do we really need to split characters and objects from each other?
    public GameObject objectContainer;

    public GameObject backgroundsContainer;

    // This list will keep track of the characters spawned by the manager so it doesn't have to search for them later.
    public List<GameObject> characters = new List<GameObject>();

    public Dictionary<string, GameObject> cutsceneCharacters = new Dictionary<string, GameObject>();

    // Dictionary of active cutscene objects.
    // TODO: Replace with a pool that manages the lifetime of cutscene objects.
    public Dictionary<string, GameObject> cutsceneObjects = new Dictionary<string, GameObject>();

    // Dictionary of active effects created by the Yarn Spinner scripts, identified by their given tags.
    // TODO: Replace this with an actual pool which manages the lifetime of effects.
    public Dictionary<string, GameObject> effects = new Dictionary<string, GameObject>();

    // Once an object has been created, this dictionary can be used as a makeshift pool of sprites that have been loaded into memory.
    // If the dictionary doesn't have the sprite, it can be created and added when necessary.
    // TODO: Replace this with an actual sprite pool which keeps track of all sprites created over the client's runtime.
    public Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

    public Dictionary<string, GameObject> cutsceneBackgrounds = new Dictionary<string, GameObject>();

    // The cache identifies loaded assets by their path, not their name. If we want to check the cache of an asset of a certain name
    // exists, without performing some LINQ queries, we'll need to know the paths of all assets of a given name.
    // TODO: Possibly move this functionality to the cache itself. It wouldn't hurt to have another dictionary inside the cache that handles
    // this relationship, and a series of helper methods that allow objects to ask for assets by name instead of a path.
    public Dictionary<string, string> assetPathsByName = new Dictionary<string, string>();

    // A set of reserved names that all newly created cutscene objects must be checked against. This is primarily intended
    // for keeping Yarn Spinner scripts from screwing with important pre-existing game objects in the scene. For preventing naming conflicts
    // with other cutscene objects a separate data structure should be used.
    public HashSet<string> reservedNames = new HashSet<string>();

    // How many effects have been created over the span of the cutscene. Useful for assigning unique names to effects.
    public int effectTotal = 0;

    // To enable smooth transitions between two background images, we need a reference to a background we can switch to.
    public static GameObject alternateBackground;

    public static CutsceneManager Instance;

    Coroutine colorChangeCoroutine;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Start a coroutine that watches for when it's okay to start the cutscene.
        StartCoroutine(AutomaticStartRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // This coroutine ensures that the entire scene has been loaded in and initialized before starting the cutscene. Object pools,
    // Yarn script compilation, the works. This prevents the Dialogue System object from starting before everything is ready.
    // TODO: For now all we have to wait for is the cutscene manager itself to be ready, so by the time this coroutine starts most of
    // the waiting is already done. Just make sure to wait a few frames before starting so everything is loaded in. Later, when we're getting
    // script information passed in and we have a bunch of other game objects to wait on, change it to watch for some 'isReady' flags or something.
    public IEnumerator AutomaticStartRoutine()
    {
        // Make sure the asset cache has been fully initialized before continuing.
        yield return new WaitUntil(() => WebAssetCache.Instance.status == WebAssetCache.WebCacheStatus.ReadyToUse);

        // Wait an additional frame to make sure all object Start() methods have been called.
        yield return null;

        // Get a list of all valid assets that are in the cache and can be used by cutscene objects.
        BuildAssetNameLists();

        // Once we're sure the scene has been setup and all objects have been created, compile a list of
        // gameobject names so we can "reserve" them and prevent the writers from making duplicate names.
        BuildReservedNameList();

        // Deal with any arguments passed in via the command line.
        ReadCommandLineArguments();

        // Spawn any essential cutscene objects.
        SpawnDefaultObjects();

        dialogueSystem.StartDialogue("Start");
    }

    public void BuildAssetNameLists()
    {
        List<WebAssetCache.LoadedImageAsset> assets = WebAssetCache.Instance.GetLoadedAssetsByCategory("characters", "npcs", "weapons", "accessories", "items", "backgrounds");

        foreach(WebAssetCache.LoadedImageAsset asset in assets)
        {
            if(!assetPathsByName.ContainsKey(asset.name))
            {
                assetPathsByName.Add(asset.name, asset.path);
                Debug.LogFormat("Cutscene Manager: Asset path added to list -> {0} : {1}", asset.name, asset.path);
            }
        }

        Debug.LogFormat("Cutscene Manager: Cutscene asset list built. {0} assets in the list.", assetPathsByName.Count);
    }

    public void BuildReservedNameList()
    {
        // Search through all gameobjects in the scene and add their names to the list.
        var objectsInScene = GameObject.FindObjectsOfType<GameObject>(true);

        foreach(GameObject objectInScene in objectsInScene)
        {
            reservedNames.Add(objectInScene.name);
        }

        // Add any manually reserved names here, if any.

        Debug.LogFormat("Cutscene Manager: List of reserved cutscene object names built. {0} names reserved.", objectsInScene.Length);
    }

    // Helper method for getting a desired command line argument.
    public string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();

        for(int i = 0; i < args.Length; i += 1)
        {
            if(args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }

        return null;
    }

    // Helper method for getting the existence of a command line argument, for when arguments can have no values.
    public bool HasArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();

        for(int i = 0; i < args.Length; i += 1)
        {
            if(args[i] == name)
            {
                return true;
            }
        }

        return false;
    }

    // Method for getting relevant arguments from the command line interface and configuring the scene before the cutscene starts.
    // These arguments should override whatever default settings are saved in the scene or by the user.
    public void ReadCommandLineArguments()
    {
        // Check for a help request and display the valid commands and their arguments (keep it cutscene specific, if possible).
        if(HasArg("-help"))
        {
            // Build the output text into a single string to avoid having a massive stacktrace attached to each line of output.
            string logOutput = "";
            logOutput += "Cutscene Manager: Valid command line arguments and their parameters are:\n";
            logOutput += "\tauto_advance -> on, off\n";
            logOutput += "\thold_time -> floating point number >= 0.0f\n";
            logOutput += "\ttypewriter -> on, off\n";
            logOutput += "\ttext_speed -> floating point number >= 1.0f\n";
            logOutput += "\ttext_speed_preset -> very_slow, slow, medium, fast, very_fast\n";
            logOutput += "\tcutscene_script -> path of Yarn Spinner script to be run by the cutscene\n";
            logOutput += "\tcutscene_directory -> path of the directory containing Yarn Spinner scripts to be run by the cutscene\n";
            Debug.LogFormat(logOutput);
        }

        // Get the default auto-advance setting.
        string autoAdvance = GetArg("-auto_advance");
        string[] autoAdvanceOptions = {"on", "off"};

        if(autoAdvance != null)
        {
            if(autoAdvance == "on")
            {
                // Autoadvance of the Yarn Spinner script should be turned on.
                uiController.SetAutoAdvanceEnabled(true);
            }
            else if(autoAdvance == "off")
            {
                // Autoadvance of the Yarn Spinner script should be turned off.
                uiController.SetAutoAdvanceEnabled(false);
            }
            else
            {
                // The value we got back isn't something we recognize. Let the user know.
                Debug.LogErrorFormat("Cutscene Manager: Unrecognized value '{0}' given for '-auto_advance' argument. Valid options are: {1}.", autoAdvance, autoAdvanceOptions);
            }
        }

        // Get the hold time for the auto-advance setting.
        string holdTime = GetArg("-hold_time");

        if(holdTime != null)
        {
            // Try and parse the value into a float and pass it to the UI controller.
            float parsedHoldTime = 0.0f;
            if(float.TryParse(holdTime, out parsedHoldTime))
            {
                uiController.SetAutoHoldTime(parsedHoldTime);
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: Unrecognized value '{0}' given for '-hold_time' argument. Value must be a floating point number.", holdTime);
            }
        }

        // Get the typewriter toggle setting.
        string typewriterSetting = GetArg("-typewriter");
        
        if(typewriterSetting != null)
        {
            if(typewriterSetting == "on")
            {
                uiController.SetTypewriterEffectEnabled(true);
            }
            else if(typewriterSetting == "off")
            {
                uiController.SetTypewriterEffectEnabled(false);
            }
            else
            {
                // The value we got back isn't something we recognize. Let the user know.
                Debug.LogErrorFormat("Cutscene Manager: Unrecognized value '{0}' given for '-auto_advance' argument. Valid options are: 'on', 'off'.", typewriterSetting);
            }
        }

        // Get the text display speed (preset version).
        string textSpeed = GetArg("-text_speed_preset");
        string[] textSpeedOptions = {"very_slow", "slow", "medium", "fast", "very_fast"};

        if(textSpeed != null)
        {
            // If the given value is valid pass it along to the UI controller.
            if(textSpeedOptions.Contains(textSpeed))
            {
                uiController.SetTextSpeed(textSpeed);
            }
            else
            {
                // The value we got back isn't something we recognize. Let the user know.
                Debug.LogErrorFormat("Cutscene Manager: Unrecognized value '{0}' given for '-text_speed_preset' argument. Valid options are: {1}.", textSpeed, textSpeedOptions);
            }
        }

        // Get the text display speed (characters per second version).
        textSpeed = GetArg("-text_speed");

        if(textSpeed != null)
        {
            // Try and parse the value into a float and pass it to the UI controller.
            float parsedTextSpeed = 0.0f;
            if(float.TryParse(textSpeed, out parsedTextSpeed))
            {
                uiController.SetTextSpeed(parsedTextSpeed);
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: Unrecognized value '{0}' given for '-text_speed' argument. Value must be a floating point number.", textSpeed);
            }
        }

        // Get the Yarn Spinner script we should load for this cutscene.
        string cutsceneScript = GetArg("-cutscene_script");

        if(cutsceneScript != null)
        {
            dialogueSystem.GetComponent<DynamicYarnLoader>().LoadScriptFile(cutsceneScript);
        }

        // Get the Yarn Spinner directory we should load for this cutscene.
        string cutsceneDirectory = GetArg("-cutscene_directory");

        if(cutsceneDirectory != null)
        {
            dialogueSystem.GetComponent<DynamicYarnLoader>().LoadScriptDirectory(cutsceneDirectory);
        }
    }

    public void SpawnDefaultObjects()
    {
        
    }

    public void StartCutscene()
    {

    }

    // Performs all necessary cleanup and shutdown procedures for ending the cutscene. For now, just hide all cutscene objects and fade to black.
    // Later we can worry about "garbage collection" after the object pools are created, and which part of the game comes after the cutscene
    // could (and probably should) be handled by whatever game manager started the cutscene in the first place.
    public void EndCutscene()
    {
        // Set the background color to black.
        //staticBackground.GetComponent<CutsceneBackground>().StartColorChangeAnimation(Color.black);

        // Hide all cutscene objects.
        foreach(KeyValuePair<string, GameObject> effect in effects.ToList())
        {
            effect.Value.GetComponent<CutsceneObject>().HideObject();
        }

        foreach(KeyValuePair<string, GameObject> cutsceneObject in cutsceneObjects.ToList())
        {
            cutsceneObject.Value.GetComponent<CutsceneObject>().HideObject();
        }

        // Hide the dialogue window.
        // TODO: Set up some standard way of hiding and showing the dialogue window. Will most likely be a part of the dialogue window
        // reverse engineering/customization that will come later.
        dialogueSystem.Stop();
        dialogueSystem.gameObject.SetActive(false);
    }

    // To make it easier for the writers, this method allows dimming multiple characters at once.
    // Note: Yarn Spinner doesn't support arrays as arguments, and you can't define the same command twice,
    // so I can't make this into a GameObject[] or use the same command name for each overloaded method.
    // As far as I know, it has to be this awful.
    [YarnCommand("dim_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, GameObject c6 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        List<CutsceneCharacter> charactersToDim = new List<CutsceneCharacter>();

        if(c1)
        {
            CutsceneCharacter character = c1.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be dimmed.", c1.name);
            }
        }

        if(c2)
        {
            CutsceneCharacter character = c2.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be dimmed.", c2.name);
            }
        }

        if(c3)
        {
            CutsceneCharacter character = c3.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be dimmed.", c3.name);
            }
        }

        if(c4)
        {
            CutsceneCharacter character = c4.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be dimmed.", c4.name);
            }
        }

        if(c5)
        {
            CutsceneCharacter character = c5.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be dimmed.", c5.name);
            }
        }

        if(c6)
        {
            CutsceneCharacter character = c6.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToDim.Add(character);
                character.StartCoroutine(character.DimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be dimmed.", c6.name);
            }
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => {
                foreach(CutsceneCharacter character in charactersToDim)
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
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, null, null, null, null, animationTime, waitForAnimation));
    }

    [YarnCommand("dim_three_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, c3, null, null, null, animationTime, waitForAnimation));
    }

    [YarnCommand("dim_four_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, c3, c4, null, null, animationTime, waitForAnimation));
    }

    [YarnCommand("dim_five_characters")]
    public static IEnumerator DimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(DimCharacters(c1, c2, c3, c4, c5, null, animationTime, waitForAnimation));
    }

    // Same for 'dim_characters' but in reverse.
    [YarnCommand("undim_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, GameObject c6 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        List<CutsceneCharacter> charactersToUndim = new List<CutsceneCharacter>();

        if(c1)
        {
            CutsceneCharacter character = c1.GetComponent<CutsceneCharacter>();

            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be undimmed.", c1.name);
            }
        }

        if(c2)
        {
            CutsceneCharacter character = c2.GetComponent<CutsceneCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be undimmed.", c2.name);
            }
        }

        if(c3)
        {
            CutsceneCharacter character = c3.GetComponent<CutsceneCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be undimmed.", c3.name);
            }
        }

        if(c4)
        {
            CutsceneCharacter character = c4.GetComponent<CutsceneCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be undimmed.", c4.name);
            }
        }

        if(c5)
        {
            CutsceneCharacter character = c5.GetComponent<CutsceneCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be undimmed.", c5.name);
            }
        }

        if(c6)
        {
            CutsceneCharacter character = c6.GetComponent<CutsceneCharacter>();
            
            if(character)
            {
                charactersToUndim.Add(character);
                character.StartCoroutine(character.UndimCharacter(animationTime));
            }
            else
            {
                Debug.LogErrorFormat("Cutscene Manager: '{0}' is not a character, npc, or item and cannot be undimmed.", c6.name);
            }
        }

        if(waitForAnimation)
        {
            yield return new WaitUntil(() => {
                foreach(CutsceneCharacter character in charactersToUndim)
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
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, null, null, null, null, animationTime, waitForAnimation));
    }

    [YarnCommand("undim_three_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, c3, null, null, null, animationTime, waitForAnimation));
    }

    [YarnCommand("undim_four_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, c3, c4, null, null, animationTime, waitForAnimation));
    }

    [YarnCommand("undim_five_characters")]
    public static IEnumerator UndimCharacters(GameObject c1 = null, GameObject c2 = null, GameObject c3 = null, GameObject c4 = null, GameObject c5 = null, float animationTime = 0.2f, bool waitForAnimation = false)
    {
        yield return Instance.StartCoroutine(UndimCharacters(c1, c2, c3, c4, c5, null, animationTime, waitForAnimation));
    }

    // Convenience command for writers to quickly switch which of two characters are dimmed or undimmed. Order of characters given
    // doesn't matter; the method handles that logic automatically.
    [YarnCommand("switch_dimmed_character")]
    public static IEnumerator SwitchDimmedCharacter(GameObject firstCharacter, GameObject secondCharacter, float animationTime = 0.2f, bool waitForAnimation = false, bool undimFirstCharacter = false, bool undimSecondCharacter = false)
    {
        // Don't accept null references. If the writer puts in the wrong name or for some reason a character
        // wasn't loaded or something else happens to make this not work, we want to know about it.
        if(!firstCharacter || !secondCharacter)
        {
            if(!firstCharacter)
            {
                Debug.LogErrorFormat("Cutscene Manager: Invalid first parameter for command 'switch_dimmed_character': Yarn Spinner was unable to find a game object by the given name.");
            }

            if(!secondCharacter)
            {
                Debug.LogErrorFormat("Cutscene Manager: Invalid second parameter for command 'switch_dimmed_character': Yarn Spinner was unable to find a game object by the given name.");
            }

            yield break;
        }
        
        // Only actual characters/npcs/whatever can be dimmed or undimmed. If the given object doesn't support the command, abort.
        CutsceneCharacter c1 = firstCharacter.GetComponent<CutsceneCharacter>();
        CutsceneCharacter c2 = secondCharacter.GetComponent<CutsceneCharacter>();

        if(!c1 || !c2)
        {
            if(!c1)
            {
                Debug.LogErrorFormat("Cutscene Manager: Invalid parameter for command 'switch_dimmed_character': '{0}' is not an existing character, npc, or item name.", c1.name);
            }

            if(!c2)
            {
                Debug.LogErrorFormat("Cutscene Manager: Invalid parameter for command 'switch_dimmed_character': '{0}' is not an existing character, npc, or item name.", c2.name);
            }
            
            yield break;
        }

        if (c1.isDimmed)
        {
            c1.StartCoroutine(c1.UndimCharacter(animationTime));
        }
        else
        {
            if(!undimFirstCharacter)
            {
                c1.StartCoroutine(c1.DimCharacter(animationTime));
            }
        }

        if (c2.isDimmed)
        {
            c2.StartCoroutine(c2.UndimCharacter(animationTime));
        }
        else
        {
            if(!undimSecondCharacter)
            {
                c2.StartCoroutine(c2.DimCharacter(animationTime));
            }
        }

        if (waitForAnimation)
        {
            yield return new WaitUntil(() => !c1.isDimming && !c1.isUndimming && !c2.isDimming && !c2.isUndimming);
        }
    }

    [YarnCommand("add_speedline_effect")]
    public static void SpawnSpeedLineEffect(GameObject position, string name = "", float radius = 100.0f, string color = "000000FF", float duration = -1.0f)
    {
        // If no name is given or there isn't already an effect by the given name, instantiate a new effect.
        if(Instance.effects.ContainsKey(name))
        {
            Debug.LogErrorFormat("Cutscene Manager: Cannot add a new effect named '{0}' because an effect by that name already exists.", name);
            return;
        }
        
        GameObject newEffect = Instantiate(Instance.speedlineEffectPrefab, position.transform);
        Instance.effectTotal += 1;

        if(name == "")
        {
            // Since no name was given, assign a unique default name to the effect.
            newEffect.name = "speedline_effect_" + Instance.effectTotal;
        }
        else
        {
            newEffect.name = name;
        }

        ParticleSystem particleSystem = newEffect.GetComponentInChildren<ParticleSystem>();

        // Set the desired radius of the effect.
        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.radius = radius;

        // Set the desired color of the effect.
        ParticleSystem.MainModule main = particleSystem.main;
        Color newColor = new Color(0.0f, 0.0f, 0.0f);

        if(ColorUtility.TryParseHtmlString(color, out newColor))
        {
            main.startColor = newColor;
        }
        else
        {
            Debug.LogWarningFormat("Cutscene Manager: Unable to parse given string '{0}' as a color value. Are you sure it's a valid hexadecimal color value?", color);
        }
        
        // Set the desired duration of the effect.
        // TODO: Make a component class specifically for effects where this can be handled.

        // Add the new effect to the dictionary so it can be found later.
        Instance.effects.Add(newEffect.name, newEffect);
    }

    [YarnCommand("remove_effect")]
    public static void RemoveSpeedLineEffect(string name)
    {
        if(Instance.effects.ContainsKey(name))
        {
            GameObject temp = Instance.effects[name];
            Instance.effects.Remove(name);
            Destroy(temp);
        }
        else
        {
            Debug.LogErrorFormat("Cutscene Manager: Cannot remove effect with name '{0}' because no effect by that name exists.", name);
        }
    }

    [YarnCommand("add_object")]
    public static void AddCutsceneObject(string objectName, string spriteName, GameObject position = null, bool visible = false)
    {
        // Check to make sure the object name isn't in the list of reserved names.
        if(Instance.reservedNames.Contains(objectName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Object cannot be created because the given name {0} is reserved by the client.", objectName);
            return;
        }

        // Check to make sure that a cutscene object by the given name doesn't already exist.
        if(Instance.cutsceneObjects.ContainsKey(objectName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Object cannot be created because another object with the name '{0}' already exists.", objectName);
            return;
        }

        // Next check to make sure that the sprite name given exists in the cache. If either of these things aren't true,
        // then there's nothing to do.
        if(!Instance.assetPathsByName.ContainsKey(spriteName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Object cannot be created because the sprite named '{0}' does not exist or is not allowed to be used as a cutscene object.", objectName);
            return;
        }

        // Now that we know the name values are legit, spawn the object and start giving it data.
        GameObject newObject = Instantiate(Instance.cutsceneObjectPrefab, Instance.objectContainer.transform);
        Instance.cutsceneObjects.Add(objectName, newObject);

        newObject.name = objectName;

        if(position)
        {
            newObject.transform.position = position.transform.position;
        }

        // Check if a sprite by the given name already exists in the pool and use it. If not, create a new one.
        Image image = newObject.GetComponent<Image>();
        image.sprite = Instance.GetSprite(spriteName);

        if(!visible)
        {
            newObject.GetComponent<CutsceneObject>().HideObject();
        }
        else
        {
            newObject.GetComponent<CutsceneObject>().ShowObject();
        }
    }

    [YarnCommand("remove_object")]
    public static void RemoveCutsceneObject(string objectName)
    {
        if(Instance.cutsceneObjects.ContainsKey(objectName))
        {
            GameObject temp = Instance.cutsceneObjects[objectName];
            Instance.cutsceneObjects.Remove(objectName);
            Destroy(temp);
        }
    }

    [YarnCommand("add_character")]
    public static void AddCharacter(string objectName, string characterName, float x = 0.0f, float y = 0.0f, float z = 0.0f, bool visible = false)
    {
        // NOTE: For now there isn't a huge difference between a base cutscene object and a character. The split in creation
        // methods needs to exist so that we'll be prepared for when more features are added in the future. The more content is built
        // upon the old method of doing things, the nastier the mess we'll be in when the methods need to be changed.

        // Check to make sure the object name isn't in the list of reserved names.
        if(Instance.reservedNames.Contains(objectName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Character cannot be created because the given name {0} is reserved by the client.", objectName);
            return;
        }

        // Check to make sure that a character by the given name doesn't already exist.
        if(Instance.cutsceneObjects.ContainsKey(objectName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Character cannot be created because another object with the name '{0}' already exists.", objectName);
            return;
        }

        // Next check to make sure that the sprite name given exists in the cache. If either of these things aren't true,
        // then there's nothing to do.
        if(!Instance.assetPathsByName.ContainsKey(characterName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Object cannot be created because the sprite named '{0}' does not exist or is not allowed to be used as a cutscene object.", objectName);
            return;
        }

        // Now that we know the name values are legit, spawn the object and start giving it data.
        GameObject newObject = Instantiate(Instance.characterPrefab, Instance.characterContainer.transform);
        newObject.name = objectName;

        // Check if a sprite by the given name already exists in the pool and use it. If not, create a new one.
        Image image = newObject.GetComponent<Image>();
        image.sprite = Instance.GetSprite(characterName);

        // If the object isn't meant to be seen after spawning, disable its rendering component.
        if(!visible)
        {
            newObject.GetComponent<CutsceneCharacter>().HideObject();
        }
        else
        {
            newObject.GetComponent<CutsceneCharacter>().ShowObject();
        }

        // Add the new object to the list(s) so it can be tracked.
        Instance.characters.Add(newObject);
        Instance.cutsceneObjects.Add(objectName, newObject);
    }

    [YarnCommand("remove_character")]
    public static void RemoveCharacter(string objectName)
    {
        if(Instance.cutsceneObjects.ContainsKey(objectName))
        {
            GameObject temp = Instance.cutsceneObjects[objectName];
            Instance.cutsceneObjects.Remove(objectName);
            Instance.characters.Remove(temp);
            Destroy(temp);
        }
    }

    [YarnCommand("add_background")]
    public static void AddBackground(string objectName, string spriteName, bool visible = false)
    {
        // Check to make sure the object name isn't in the list of reserved names.
        if(Instance.reservedNames.Contains(objectName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Background object cannot be created because the given name {0} is reserved by the client.", objectName);
            return;
        }

        // Check to make sure that a character by the given name doesn't already exist.
        if(Instance.cutsceneObjects.ContainsKey(objectName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Background object cannot be created because another object with the name '{0}' already exists.", objectName);
            return;
        }

        // Next check to make sure that the sprite name given exists in the cache. If either of these things aren't true,
        // then there's nothing to do.
        // NOTE: Probably redundant. Either handled by the existing GetSprite() method, or will be handled by a sprite pool.
        if(!Instance.assetPathsByName.ContainsKey(spriteName))
        {
            Debug.LogErrorFormat("Cutscene Manager: Background object cannot be created because the sprite named '{0}' does not exist or is not allowed to be used as a cutscene object.", spriteName);
            return;
        }

        // Now that we know the name values are legit, spawn the object and start giving it data.
        GameObject newObject = Instantiate(Instance.backgroundPrefab, Instance.backgroundsContainer.transform);
        newObject.name = objectName;

        // Check if a sprite by the given name already exists in the pool and use it. If not, create a new one.
        // NOTE: This should now be handled by the background object itself.
        newObject.GetComponent<CutsceneBackground>().SetImage(spriteName);

        // If the object isn't meant to be seen after spawning, disable its rendering component.
        if(!visible)
        {
            newObject.GetComponent<CutsceneBackground>().HideObject();
        }
        else
        {
            newObject.GetComponent<CutsceneBackground>().ShowObject();
        }

        // Add the new object to the list so it can be tracked.
        Instance.cutsceneObjects.Add(objectName, newObject);
        Instance.cutsceneBackgrounds.Add(objectName, newObject);
    }

    [YarnCommand("remove_background")]
    public static void RemoveBackground(string objectName)
    {
        if(Instance.cutsceneObjects.ContainsKey(objectName) && Instance.cutsceneBackgrounds.ContainsKey(objectName))
        {
            GameObject temp = Instance.cutsceneObjects[objectName];
            Instance.cutsceneObjects.Remove(objectName);
            Instance.cutsceneBackgrounds.Remove(objectName);
            Destroy(temp);
        }
    }

    // Until we have a pool of sprites and other assets, we need some methods that manage creating and passing around assets that other objects need.
    public Sprite GetSprite(string spriteName)
    {
        // If the sprite already exists in the pool, return it.
        if(sprites.ContainsKey(name))
        {
            return sprites[name];
        }
        else
        {
            // The sprite doesn't exist yet so it needs to be created, if possible.

            // Make sure the sprite name corresponds to a texture asset that exists in the cache, and that it is allowed to be used for cutscene objects.
            if(!assetPathsByName.ContainsKey(spriteName))
            {
                Debug.LogErrorFormat("Cutscene Manager: Sprite named '{0}' does not exist or is not allowed to be used as a cutscene object.", spriteName);
                return null;
            }

            // Create the new sprite, add it to the pool, and pass it on to whoever asked for it.
            WebAssetCache.LoadedImageAsset asset = WebAssetCache.Instance.GetLoadedImageAssetByPath(assetPathsByName[spriteName]);
            Sprite newSprite = Sprite.Create(asset.texture, new Rect(0.0f, 0.0f, asset.texture.width, asset.texture.height), new Vector2(0.0f, 0.0f), 100.0f, 0, SpriteMeshType.FullRect);

            Instance.sprites.Add(spriteName, newSprite);

            return newSprite;
        }
    }
}
