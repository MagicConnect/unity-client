using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using BestHTTP;

public class GameDataCache : MonoBehaviour
{
    // Singleton instance of this class to ensure we only download all of this once, and every object knows where to find it.
    public static GameDataCache Instance {get; private set;}

    #region JSON parsed data classes
    // This is the object containing meta information we parse from the JSON manifest.
    [System.Serializable]
    public class MetaInformation
    {
        [SerializeField]
        public string Hash {get; set;}
    }

    // Object containing asset information parsed from the JSON manifest.
    [System.Serializable]
    public class Asset
    {
        [SerializeField]
        public string Name {get; set;}
        [SerializeField]
        public string Path {get; set;}
        [SerializeField]
        public string Id {get; set;}
    }

    // This is the object representing the parsed JSON manifest.
    [System.Serializable]
    public class GameDataManifest
    {
        [SerializeField]
        public MetaInformation Meta {get; set;}
        [SerializeField]
        public Asset[] Assets {get; set;}
    }

    // This is the object representing the parsed JSON version file.
    [System.Serializable]
    public class VersionNumber
    {
        [SerializeField]
        public string Version {get; set;}
    }

    // Object representing an item parsed from JSON data.
    public class Item
    {
        public string name;
        public string art;
        public int sellValue;
        public string description;
        public string itemType;
        public string id;
    }

    // Object representing a condition parsed from JSON data.
    // NOTE: This is currently not anywhere in the game data that I can find. It exists as a class so
    // Abilities can be parsed from JSON without issues.
    public class Condition
    {}

    // Object representing properties parsed from JSON data.
    public class Props
    {
        public bool isPercent;
        public int baseValue;
        public string baseStat;
    }

    // Object representing an effect parsed from JSON data.
    public class Effect
    {
        public string value;
        public string target;
        public Props props;
    }

    public class LbChange
    {
        public bool shouldHide;
        public string name;
        public string description;
        public Condition[] conditions;
        public Effect[] effects;
        public string trigger;
        // TODO: Figure out what this is supposed to be, because there's some sort of ambiguity or redundancy, I don't know which yet.
        public LbChange lbChanges;
    }

    public class Ability
    {
        public string name;
        public string description;
        public bool isAbilityUsedAtLB0;
        public Condition[] conditions;
        public Effect[] effects;
        public string trigger;
        public LbChange[] lbChanges;
        public string id;
    }

    // A character id reference as it appears in a Banner object.
    public class BannerCharacterReference
    {
        public string name;
        public bool isBannerSpecial;
    }

    // An accessory id reference as it appears in a Banner object.
    public class BannerAccessoryReference
    {
        public string name;
        public bool isBannerSpecial;
    }

    // An item id reference as it appears in a Banner object.
    public class BannerItemReference
    {
        public string name;
        public bool isBannerSpecial;
    }

    // A weapon id reference as it appears in a Banner object.
    public class BannerWeaponReference
    {
        public string name;
        public bool isBannerSpecial;
    }

    public class Banner
    {
        public string id;
        public string name;
        public string art;
        public string description;
        public string type;
        public string activeStarts;
        public string activeEnds;
        public string rollItem;
        public BannerCharacterReference[] characters;
        public BannerAccessoryReference[] accessories;
        public BannerItemReference[] items;
        public BannerWeaponReference[] weapons;
    }

    // The base points object found in a character object.
    public class BasePoints
    {
        public int attacker;
        public int caster;
        public int defender;
        public int healer;
        public int ranger;
    }

    // The base stats object found in a character object.
    public class BaseStats
    {
        public int attack;
        public int defense;
        public int magic;
        public int special;
        public int accuracy;
        public int critical;
        public int hp;
        public int magicEvasion;
        public int meleeEvasion;
    }

    // The level points object found in a character object.
    public class LevelPoints
    {
        public int attacker;
        public int caster;
        public int defender;
        public int healer;
        public int ranger;
    }

    // The level stats object found in a character object.
    public class LevelStats
    {
        public int attack;
        public int defense;
        public int magic;
        public int special;
        public int accuracy;
        public int critical;
        public int hp;
        public int magicEvasion;
        public int meleeEvasion;
    }

    // The lb points object found in a character object.
    public class LbPoints
    {
        public int attacker;
        public int caster;
        public int defender;
        public int healer;
        public int ranger;
    }

    // The lb stats object found in a character object.
    public class LbStats
    {
        public int attack;
        public int defense;
        public int magic;
        public int special;
        public int accuracy;
        public int critical;
        public int hp;
        public int magicEvasion;
        public int meleeEvasion;
    }

    // A character's ability id references arranged by ability group.
    public class CharacterAbilityGroup
    {
        public string name;
        public string[] abilities;
    }

    // A skill id reference as it appears in a character object.
    public class CharacterSkillReference
    {
        public string name;
        public int lb;
    }

    // Spritesheet data that appears in a characters/enemies/etc.
    public class SpritesheetData
    {
        public int attackFrames;
        public int castFrames;
        public int deadFrames;
        public int idleFrames;
        public int moveFrames;
        public int onDeathFrames;
        public int onHitFrames;
    }

    public class Character
    {
        public string name;
        public string art;
        public string headArt;
        public string spritesheet;
        public int stars;
        public string primaryStat;
        public string archetype;
        public string weapon;
        public BasePoints basePoints;
        public BaseStats baseStats;
        public LevelPoints levelPoints;
        public LevelStats levelStats;
        public LbPoints lbPoints;
        public LbStats lbStats;
        public CharacterAbilityGroup[] abilities;
        public CharacterSkillReference[] skills;
        public string specialSkill;
        public string id;
        public SpritesheetData spritesheetData;
        public string reinforceItem;
        public int speed;
        public string title;
    }

    public class Accessory
    {
        public string name;
        public string art;
        public string itemType;
        public int sellValue;
        public string description;
        public int stars;
        public string primaryStat;
        public string id;
        public string[] abilities;
    }

    public class Enemy
    {
        public string id;
        public string name;
        public string art;
        public string spritesheet;
        public string primaryStat;
        public BasePoints basePoints;
        public BaseStats baseStats;
        public LevelPoints levelPoints;
        public LevelStats levelStats;
        public string[] abilities;
        public string[] skills;
        public SpritesheetData spritesheetData;
    }

    public class ElementHardCap
    {
        public int Dark;
        public int Earth;
        public int Fire;
        public int Ice;
        public int Light;
        public int Thunder;
        public int Neutral;
    }

    public class ElementSaturation
    {
        public int Dark;
        public int Earth;
        public int Fire;
        public int Ice;
        public int Light;
        public int Thunder;
        public int Neutral;
    }

    // TODO: Figure out the schema for this data.
    public class Grid
    {}

    public class NodeEnemyReference
    {
        public string name;
        public int level;
        public int width;
        public int height;
    }

    public class NodeCombat
    {
        public bool usesDefaultHardCap;
        public bool usesDefaultSaturation;
        public ElementHardCap elementHardCap;
        public ElementSaturation elementSaturation;
        public Grid grid;
    }

    public class MapNode
    {
        public string id;
        public string name;
        public float x;
        public float y;
        public string description;
        public int staminaCost;
        public string unlocksMap;
        public string[] abilities;
        public string[] drops;
        public bool isDefaultAvailable;
    }

    // TODO: Figure out the schema for this information.
    public class NodeConnection
    {}

    public class Map
    {
        public string id;
        public string name;
        public string art;
        public string activeStarts;
        public string activeEnds;
        public MapNode[] nodes;
        public NodeConnection[] nodeConnections;
    }

    // A character's shop information and id reference as listed in a shop.
    public class ShopCharacter
    {
        public string name;
        public int cost;
        public int quantity;
    }

    // An accessory's shop information and id reference as listed in a shop.
    public class ShopAccessory
    {
        public string name;
        public int cost;
        public int quantity;
    }

    // An item's shop information and id reference as listed in a shop.
    public class ShopItem
    {
        public string name;
        public int cost;
        public int quantity;
    }

    // A weapon's shop information and id reference as listed in a shop.
    public class ShopWeapon
    {
        public string name;
        public int cost;
        public int quantity;
    }

    public class Shop
    {
        public string id;
        public string name;
        public string shopReset;
        public string description;
        public string activeStarts;
        public string activeEnds;
        public string currencyItem;
        public ShopCharacter[] characters;
        public ShopAccessory[] accessories;
        public ShopItem[] items;
        public ShopWeapon[] weapons;
    }

    public class GeneratedElements
    {
        public int Neutral;
        public int Fire;
        public int Ice;
        public int Light;
        public int Dark;
        public int Earth;
        public int Thunder;
    }

    public class ConsumedElements
    {
        public int Neutral;
        public int Fire;
        public int Ice;
        public int Light;
        public int Dark;
        public int Earth;
        public int Thunder;
    }

    public class StatScaling
    {
        public int hp;
        public int attack;
        public int defense;
        public int magic;
        public int special;
        public int accuracy;
        public int critical;
        public int magicEvasion;
        public int meleeEvasion;
    }

    public class SkillAction
    {
        public int castTime;
        public bool dropsTrap;
        public bool canTargetDead;
        public string[] elements;
        public int hits;
        public string pattern;
        public int pull;
        public int push;
        public StatScaling statScaling;
        public string[] statusEffectChanges;
        public string validTargets;
    }

    public class Skill
    {
        public string name;
        public string art;
        public string description;
        public SkillAction[] actions;
        public int cooldown;
        public int hpCost;
        public int spcCost;
        public string id;
        public GeneratedElements generatedElements;
        public ConsumedElements consumedElements;
    }

    public class Weapon
    {
        public string name;
        public string art;
        public string itemType;
        public int sellValue;
        public string description;
        public int stars;
        public string primaryStat;
        public string[] abilities;
        public string weaponType;
        public string id;
    }

    public class AchievementRequirement
    {
        public string stat;
        public int statValue;
        public string mapName;
        public string mapNodeName;
    }

    public class AchievementAccessoryReward
    {
        public string name;
        public int quantity;
    }

    public class AchievementItemReward
    {
        public string name;
        public int quanity;
    }

    public class AchievementCharacterReward
    {
        public string name;
        public int quantity;
    }

    public class AchievementWeaponReward
    {
        public string name;
        public int quantity;
    }

    public class AchievementRewards
    {
        public AchievementAccessoryReward[] accessories;
        public AchievementCharacterReward[] characters;
        public AchievementItemReward[] items;
        public AchievementWeaponReward[] weapons;
    }

    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public string art;
        public AchievementRequirement requirements;
        public AchievementRewards rewards;
        public bool isRepeatable;
        public string category;
    }

    public class Store
    {}

    public class CalendarBonus
    {}

    public class GameDataContent
    {
        public Item[] items;
        public Ability[] abilities;
        public Banner[] banners;
        public Character[] characters;
        public Accessory[] accessories;
        public Enemy[] enemies;
        public Map[] maps;
        public Shop[] shops;
        public Skill[] skills;
        public Weapon[] weapons;
        public Achievement[] achievements;
        public Store[] stores;
        public CalendarBonus[] calendarBonuses;
    }
    #endregion

    // The dictionaries which store the game data objects by their id.
    public Dictionary<string, Item> itemsById = new Dictionary<string, Item>();
    public Dictionary<string, Ability> abilitiesById = new Dictionary<string, Ability>();
    public Dictionary<string, Banner> bannersById = new Dictionary<string, Banner>();
    public Dictionary<string, Character> charactersById = new Dictionary<string, Character>();
    public Dictionary<string, Accessory> accessoriesById = new Dictionary<string, Accessory>();
    public Dictionary<string, Enemy> enemiesById = new Dictionary<string, Enemy>();
    public Dictionary<string, Map> mapsById = new Dictionary<string, Map>();
    public Dictionary<string, Shop> shopsById = new Dictionary<string, Shop>();
    public Dictionary<string, Skill> skillsById = new Dictionary<string, Skill>();
    public Dictionary<string, Weapon> weaponsById = new Dictionary<string, Weapon>();
    public Dictionary<string, Achievement> achievementsById = new Dictionary<string, Achievement>();
    public Dictionary<string, Store> storesById = new Dictionary<string, Store>();
    public Dictionary<string, CalendarBonus> calendarBonusesById = new Dictionary<string, CalendarBonus>();

    // Just in case, these dictionaries will store the specific unparsed JTokens that the above gamedata was
    // parsed from. If the above object definitions aren't working for some reason, these will be a backup.
    public Dictionary<string, JToken> jsonItemsById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonAbilitiesById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonBannersById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonCharactersById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonAccessoriesById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonEnemiesById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonMapsById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonShopsById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonSkillsById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonWeaponsById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonAchievementsById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonStoresById = new Dictionary<string, JToken>();
    public Dictionary<string, JToken> jsonCalendarBonusesById = new Dictionary<string, JToken>();

    // The local version information, if it exists.
    private VersionNumber currentVersion;
    
    // The version information downloaded from the asset server.
    private VersionNumber serverVersion;

    // Reference to the startup coroutine so we can better track its progress.
    public Coroutine startupCoroutine;

    // The persistent directory where cache data should be saved to and loaded from.
    public string cacheDirectory;

    // Boolean flag indicating if the gamedata cache is ready to be used yet.
    public bool isReady = false;

    // The original JSON file of the server version.
    public string versionJSON;

    // The original JSON file of the gamedata content.
    public string sourceJSON;

    // Events which broadcast important state changes and information to listeners.
    public static event Action OnCacheReady;

    public static event Action OnGameDataCacheStartupBegin;

    public static event Action OnGameDataCacheStartupEnd;

    // The JSON game data parsed into a traversable tree/dictionary of JTokens.
    public JObject parsedGameData
    {
        get 
        {
            Debug.LogWarningFormat(this, "Game Data Cache: 'parsedGameData' has been deprecated. Use 'jsonData' instead, the name is better.");
            return _jsonData;
        } 
        private set
        {
            _jsonData = value;
        }
    }
    private JObject _jsonData;
    public JObject jsonData 
    {
        get 
        {
            return _jsonData;
        } 
        private set
        {
            _jsonData = value;
        }
    }

    // The fully parsed game data in C# object form.
    public GameDataContent data {get; private set;}

    void Awake()
    {
        // Set up our singleton instance of the class.
        if(Instance != null)
        {
            // If we already have an instance of this class, destroy this instance.
            Destroy(gameObject);
            return;
        }

        // If there is no instance of this class, set it and mark it so Unity doesn't destroy it between scene changes.
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // We're not allowed to call Application.persistentDataPath from a Monobehavior constructor (Unity's words, not mine) so we need to
        // set the path either here or in the Start() function
        cacheDirectory = Path.Combine(Application.persistentDataPath, "GameDataCache/");

        // Ensure that there is a cache directory we can save files to.
        if(!Directory.Exists(cacheDirectory))
        {
            Directory.CreateDirectory(cacheDirectory);
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

    // Begins the startup process once called. Startup runs in the background because it could take a while to complete.
    public void Startup()
    {
        if(startupCoroutine == null)
        {
            startupCoroutine = StartCoroutine(StartupRoutine());
        }
        else
        {
            Debug.LogErrorFormat("GameDataCache: Attempted to launch startup routine when startup routine is already in progress.");
        }
    }

    // This coroutine automates the startup sequence for the cache so the cache functionality can be made more modular and independent.
    // Any complex, macroscopic startup logic should be handled here.
    private IEnumerator StartupRoutine()
    {
        Debug.Log("GameDataCache: Startup coroutine launched.");

        // Fire the startup begin event.
        if(OnGameDataCacheStartupBegin != null)
        {
            OnGameDataCacheStartupBegin.Invoke();
        }

        Debug.Log("GameDataCache: Checking for cached version data...");

        // First we try and load any locally cached version data.
        string versionFilePath = Path.Combine(cacheDirectory, "gamedata_version.dat");
        if(File.Exists(versionFilePath))
        {
            currentVersion = JsonConvert.DeserializeObject<VersionNumber>(LoadDataFromFile("gamedata_version.dat"));
            Debug.Log("GameDataCache: Cached version data was found.");
        }
        else
        {
            Debug.Log("GameDataCache: No cached version data was found.");
        }

        Debug.Log("GameDataCache: Requesting version information from the server...");

        // Any HTTP request made during a Unity event function (such as Start or Update) needs to be setup first. Since we can't
        // guarantee the caller won't be a Unity event function, we might as well put this here just to make sure.
        HTTPManager.Setup();
        HTTPManager.MaxConnectionPerServer = 1;
        HTTPRequest versionRequest = new HTTPRequest(new Uri("https://gamedata.magic-connect.com/version.json"), OnVersionRequestFinished);
        versionRequest.ConnectTimeout = TimeSpan.FromSeconds(30);
        versionRequest.Timeout = TimeSpan.FromSeconds(1000);
        versionRequest.MaxRetries = 10;
        versionRequest.Send();

        // Wait until we get the response back.
        // TODO: convert this into some kind of switch statement. There are other possible states aside from Finished that need to be handled.
        // Either that or just let the version request callback handle it, since it's already setup to handle the different possible responses.
        yield return new WaitUntil(() => versionRequest.State == HTTPRequestStates.Finished && serverVersion != null);

        // If any new data was downloaded from the server, then we need to cache it later.
        bool newDataToCache = false;

        // If the local version and the server version don't match then we'll need to download new content from the server.
        bool versionMismatch = (currentVersion == null) || (currentVersion.Version != serverVersion.Version);

        // We also need to make sure that a local cache of the content actually exists. If one doesn't, then we'll need to download a new one from the server
        // anyway.
        bool cachedContentExists = File.Exists(Path.Combine(cacheDirectory, "content.dat"));

        // If there is a version mismatch or the cache doesn't exist, go ahead and download content from the server. Otherwise, load the cached copy.
        if(versionMismatch || !cachedContentExists)
        {
            Debug.LogFormat("GameDataCache: Cached game data doesn't match server data. New data will be downloaded.");
            HTTPRequest contentRequest = new HTTPRequest(new Uri("https://gamedata.magic-connect.com/content.json"), OnContentRequestFinished);
            contentRequest.Send();

            yield return new WaitUntil(() => contentRequest.State == HTTPRequestStates.Finished && parsedGameData != null);
            newDataToCache = true;
            Debug.LogFormat("GameDataCache: New game data has been downloaded from the server.");
        }
        else
        {
            Debug.LogFormat("GameDataCache: Local gamedata version matches server version. Loading gamedata from cache.");

            // At some point the content.dat file failed to be created, and it caused an exception here. Make sure that the content file can actually
            // be parsed into usable data before continuing.
            try
            {
                //parsedGameData = JObject.Parse(LoadDataFromFile("content.dat"));
                LoadGameDataFromJson(LoadDataFromFile("content.dat"));
            }
            catch(Exception e)
            {
                Debug.LogErrorFormat("GameDataCache: Exception occurred while loading 'content.dat': {0}", e);
            }
        }

        // Cache the new version and asset manifest locally. We do the version file last because that's how we know the cache was successfully created.
        // A missing or out of date version file means the cache may be corrupt/incomplete and needs to be rebuilt.
        Debug.Log("GameDataCache: Caching version and content data.");
        if(newDataToCache)
        {
            SaveDataToFile("content.dat", sourceJSON);
            SaveDataToFile("gamedata_version.dat", versionJSON);
            currentVersion = serverVersion;
        }

        // The startup coroutine is complete and the cache should be ready.
        startupCoroutine = null;
        isReady = true;
        Debug.Log("GameDataCache: Startup routine complete. Cache is ready for use. Closing down startup coroutine.");

        // Fire off the events for startup finishing, and the gamedata cache being okay to use.
        if(OnGameDataCacheStartupEnd != null)
        {
            OnGameDataCacheStartupEnd.Invoke();
        }

        if(OnCacheReady != null)
        {
            OnCacheReady();
        }
    }

    // Parses a json string of game data into usable objects.
    public void LoadGameDataFromJson(string json)
    {
        // Store the original json string of the game data.
        sourceJSON = json;

        // Automatically parse the JSON into JTokens.
        parsedGameData = JObject.Parse(json);

        // Parse the JSON and store it in dedicated C# objects.
        data = JsonConvert.DeserializeObject<GameDataContent>(json);

        // Store the parsed data in dictionaries for ease of access.
        foreach(Item item in data.items)
        {
            itemsById.Add(item.id, item);
        }

        foreach(JToken item in jsonData["items"].Children())
        {
            jsonItemsById.Add(item["id"].Value<string>(), item);
        }

        foreach(Banner banner in data.banners)
        {
            bannersById.Add(banner.id, banner);
        }

        foreach(JToken banner in jsonData["banners"].Children())
        {
            jsonBannersById.Add(banner["id"].Value<string>(), banner);
        }

        foreach(Character character in data.characters)
        {
            charactersById.Add(character.id, character);
        }

        foreach(JToken character in jsonData["characters"].Children())
        {
            jsonCharactersById.Add(character["id"].Value<string>(), character);
        }

        foreach(Ability ability in data.abilities)
        {
            abilitiesById.Add(ability.id, ability);
        }

        foreach(JToken ability in jsonData["abilities"].Children())
        {
            jsonAbilitiesById.Add(ability["id"].Value<string>(), ability);
        }

        foreach(Accessory accessory in data.accessories)
        {
            accessoriesById.Add(accessory.id, accessory);
        }

        foreach(JToken accessory in jsonData["accessories"].Children())
        {
            jsonAccessoriesById.Add(accessory["id"].Value<string>(), accessory);
        }

        foreach(Achievement achievement in data.achievements)
        {
            achievementsById.Add(achievement.id, achievement);
        }

        foreach(JToken achievement in jsonData["achievements"].Children())
        {
            jsonAchievementsById.Add(achievement["id"].Value<string>(), achievement);
        }

        // TODO: Whenever calendar bonuses are defined and have ids, add them here.
        foreach(CalendarBonus bonus in data.calendarBonuses)
        {
            //
        }

        foreach(JToken bonus in jsonData["calendarBonuses"].Children())
        {
            //jsonItemsById.Add(ability["id"].Value<string>(), ability);
        }

        foreach(Enemy enemy in data.enemies)
        {
            enemiesById.Add(enemy.id, enemy);
        }

        foreach(JToken enemy in jsonData["enemies"].Children())
        {
            jsonEnemiesById.Add(enemy["id"].Value<string>(), enemy);
        }

        foreach(Map map in data.maps)
        {
            mapsById.Add(map.id, map);
        }

        foreach(JToken map in jsonData["maps"].Children())
        {
            jsonMapsById.Add(map["id"].Value<string>(), map);
        }

        foreach(Shop shop in data.shops)
        {
            shopsById.Add(shop.id, shop);
        }

        foreach(JToken shop in jsonData["shops"].Children())
        {
            jsonShopsById.Add(shop["id"].Value<string>(), shop);
        }

        foreach(Skill skill in data.skills)
        {
            skillsById.Add(skill.id, skill);
        }

        foreach(JToken skill in jsonData["skills"].Children())
        {
            jsonSkillsById.Add(skill["id"].Value<string>(), skill);
        }
        
        // TODO: Whenever stores are defined and have an id, add them here.
        foreach(Store store in data.stores)
        {
            
        }

        foreach(JToken store in jsonData["stores"].Children())
        {
            //jsonItemsById.Add(ability["id"].Value<string>(), ability);
        }

        foreach(Weapon weapon in data.weapons)
        {
            weaponsById.Add(weapon.id, weapon);
        }

        foreach(JToken weapon in jsonData["weapons"].Children())
        {
            jsonWeaponsById.Add(weapon["id"].Value<string>(), weapon);
        }
    }

    // Callback for our http version request.
    private void OnVersionRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // Http requests can return with a response object if successful, or a null response as a result of some error that occurred. 
        // We need to handle each possible result state of the request so we can know about and fix any issues.
        switch(req.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(resp.IsSuccess)
                {
                    // Now that we got our response from the server, parse the results into a usable object.
                    serverVersion = JsonConvert.DeserializeObject<VersionNumber>(resp.DataAsText);
                    versionJSON = resp.DataAsText;
                    Debug.LogFormat("GameDataCache: Server manifest version downloaded. Version: {0}", serverVersion.Version);
                }
                else
                {
                    Debug.LogWarningFormat("GameDataCache: Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("GameDataCache: Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("GameDataCache: Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("GameDataCache: Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("GameDataCache: Processing the request timed out.");
                break;
        }// end switch block
    }

    // Callback for our http content request.
    private void OnContentRequestFinished(HTTPRequest req, HTTPResponse resp)
    {
        // Http requests can return with a response object if successful, or a null response as a result of some error that occurred. 
        // We need to handle each possible result state of the request so we can know about and fix any issues.
        switch(req.State)
        {
            // The request finished without any problem.
            case HTTPRequestStates.Finished:
                if(resp.IsSuccess)
                {
                    // Now that we got our response from the server, parse the results into a usable object.
                    //parsedGameData = JObject.Parse(resp.DataAsText);
                    //contentJSON = resp.DataAsText;
                    LoadGameDataFromJson(resp.DataAsText);
                    Debug.LogFormat("GameDataCache: Server game data downloaded.");
                }
                else
                {
                    Debug.LogWarningFormat("GameDataCache: Request finished successfully, but the server sent an error. Status Code: {0}--{1} Message: {2}", resp.StatusCode, resp.Message, resp.DataAsText);
                }
                break;
            // The request finished with an unexpected error. The request's Exception property may contain more info about the error.
            case HTTPRequestStates.Error:
                Debug.LogError("GameDataCache: Request finished with an error: " + (req.Exception != null ? (req.Exception.Message + "\n" + req.Exception.StackTrace) : "No Exception"));
                break;
            // The request aborted, initiated by the user.
            case HTTPRequestStates.Aborted:
                Debug.LogWarning("GameDataCache: Request aborted.");
                break;
            // Connecting to the server timed out.
            case HTTPRequestStates.ConnectionTimedOut:
                Debug.LogError("GameDataCache: Connection timed out.");
                break;
            // The request didn't finish in the given time.
            case HTTPRequestStates.TimedOut:
                Debug.LogError("GameDataCache: Processing the request timed out.");
                break;
        }// end switch block
    }

    // Method which handles writing serialized classes to the persistent data folder.
    // TODO: This method is a lot shorter now than I expected it to be and it's only used for caching the version and manifest data,
    // so consider just deleting this.
    private void SaveDataToFile(string path, string json)
    {
        var filePath = Path.Combine(cacheDirectory, path);
        File.WriteAllText(filePath, json);
    }

    // Method which handles loading serialized classes from the persistent data folder.
    private string LoadDataFromFile(string path)
    {
        var filePath = Path.Combine(cacheDirectory, path);

        if(File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        else
        {
            Debug.LogErrorFormat("There is no file under the path {0}", filePath);
            return "";
        }
    }
}
