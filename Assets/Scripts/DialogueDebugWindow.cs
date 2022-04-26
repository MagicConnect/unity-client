using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yarn.Unity;
using Newtonsoft.Json;
using SimpleFileBrowser;

public class DialogueDebugWindow : MonoBehaviour
{
    // TODO: Use references to the actual components we'll be interacting with instead of the game objects.
    // It's annoying having to use GetComponent() every time and it makes the code harder to read.

    // The actual visible portion of the debug window.
    public GameObject displayPanel;

    // The text input field which holds the address for the desired yarn script folder.
    public GameObject workspaceInput;

    // The checkbox 
    public GameObject loadSubfoldersToggle;

    // The Yarn dialogue runner that we're going to load scripts into.
    public GameObject dialogueRunner;

    // The dropdown component where our loaded yarn script nodes should be displayed.
    public GameObject loadedNodesDropdown;

    // The coroutine handling file browser input.
    Coroutine fileBrowserCoroutine;

    private class DebugSettings
    {
        public string Workspace {get; set;} = "";

        public string LoadSubfolders {get; set;} = "true";
    }

    private DebugSettings settings;

    public UISkin lightModeSkin;

    public UISkin darkModeSkin;

    public bool useDarkMode = true;

    // Start is called before the first frame update
    void Start()
    {
        displayPanel.SetActive(false);

        // Create a blank settings object to be populated later.
        settings = new DebugSettings();

        // Load previously saved settings, if they exist.
        //LoadSettings();

        // Set up the SimpleFileBrowser.
        FileBrowser.SetFilters(false, new FileBrowser.Filter("Yarn Scripts", ".yarn"));
        FileBrowser.Skin = darkModeSkin;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleDebugWindow();
        }
    }

    void OnDestroy()
    {
        
    }

    // When the 'load all scripts' button is clicked, all scripts in the designated folder are loaded, compiled into a program,
    // and added to the Dialogue Runner.
    public void OnLoadAllScriptsClicked()
    {
        // Get the files and folders to load from the file browser.
        fileBrowserCoroutine = StartCoroutine(ShowLoadDialogueCoroutine());
    }

    public void OnStartAtNodeClicked()
    {
        string selectedOption = loadedNodesDropdown.GetComponent<TMP_Dropdown>().captionText.text;
        if(dialogueRunner.GetComponent<DialogueRunner>().NodeExists(selectedOption))
        {
            dialogueRunner.GetComponent<DialogueRunner>().Stop();
            HideOptionsList();
            dialogueRunner.GetComponent<DialogueRunner>().StartDialogue(selectedOption);
        }
        else
        {
            Debug.LogError("Cannot jump to a node that does not exist.");
        }
    }

    public void RefreshNodeDropdown()
    {
        loadedNodesDropdown.GetComponent<TMP_Dropdown>().ClearOptions();
        List<string> nodeNames = new List<string>(dialogueRunner.GetComponent<DialogueRunner>().Dialogue.NodeNames);
        loadedNodesDropdown.GetComponent<TMP_Dropdown>().AddOptions(nodeNames);
    }

    // For some reason Yarn Spinner doesn't hide the options list when you stop the dialogue and switch to a new project.
    // We need to manually hide it to keep it from getting in the way.
    public void HideOptionsList()
    {
        CanvasGroup options = dialogueRunner.GetComponent<DialogueRunner>().GetComponentInChildren<OptionsListView>().gameObject.GetComponent<CanvasGroup>();
        options.alpha = 0;
        options.interactable = false;
        options.blocksRaycasts = false;
    }

    public void ToggleDebugWindow()
    {
        displayPanel.SetActive(!displayPanel.activeInHierarchy);
    }

    // Save the current debug window data and settings so it's easier to pick up where the user left off.
    public void SaveSettings()
    {
        // Populate/update the settings object with the current debug window configuration.
        settings.Workspace = workspaceInput.GetComponent<TMP_InputField>().text;

        if(loadSubfoldersToggle.GetComponent<Toggle>().isOn)
        {
            settings.LoadSubfolders = "true";
        }
        else
        {
            settings.LoadSubfolders = "false";
        }

        // If there isn't already a directory to save the settings to, create one.
        string settingsDirectory = Path.Combine(Application.persistentDataPath, "settings/");

        if(!Directory.Exists(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }

        // Serialize the settings to json and write them to a file.
        string jsonString = JsonConvert.SerializeObject(settings);
        File.WriteAllText(Path.Combine(settingsDirectory, "dialogue_debug_settings.json"), jsonString);

        Debug.LogFormat("Saved dialogue debug window settings to '{0}'.", Path.Combine(settingsDirectory, "dialogue_debug_settings.json"));
    }

    // Load the saved settings and set up the debug window to use them.
    public void LoadSettings()
    {
        string settingsDirectory = Path.Combine(Application.persistentDataPath, "settings/");

        if(Directory.Exists(settingsDirectory))
        {
            string filePath = Path.Combine(settingsDirectory, "dialogue_debug_settings.json");

            if(File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                settings = JsonConvert.DeserializeObject<DebugSettings>(jsonString);
                
                workspaceInput.GetComponent<TMP_InputField>().text = settings.Workspace;

                if(settings.LoadSubfolders == "true")
                {
                    loadSubfoldersToggle.GetComponent<Toggle>().isOn = true;
                }
                else
                {
                    loadSubfoldersToggle.GetComponent<Toggle>().isOn = false;
                }
            }
        }
    }

    // Coroutine that handles opening the file browser and loading the files the user has chosen.
    private IEnumerator ShowLoadDialogueCoroutine()
    {
        // Wait for the user to interact with the file browser.
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Yarn Spinner Scripts", "Load");

        Debug.Log(FileBrowser.Success);

        // If the file browser was successful in getting some files and/or directories, continue.
        if(FileBrowser.Success && FileBrowser.Result.Length > 0)
        {
            // The combined script to be passed to the dynamic yarn loader.
            string yarnScriptContents = "";

            // The filebrowser returns an array of all files and folders selected by the user.
            foreach(string path in FileBrowser.Result)
            {
                // Determine whether the given path is a file or a directory.
                if(Directory.Exists(path))
                {
                    // Enumerate all yarn files in the given directory, then add them to the script.
                    Debug.LogFormat("Loading all Yarnspinner scripts in '{0}'", path);
                    var yarnScripts = Directory.EnumerateFiles(path, "*.yarn", SearchOption.AllDirectories);

                    foreach(string script in yarnScripts)
                    {
                        Debug.LogFormat("Loading Yarnspinner script '{0}'.", script);
                        yarnScriptContents += File.ReadAllText(script);
                        yarnScriptContents += "\n";
                    }
                }
                else if(File.Exists(path))
                {
                    // Load the yarn file and add its contents to the script.
                    Debug.LogFormat("Loading Yarnspinner script '{0}'.", path);
                    yarnScriptContents += File.ReadAllText(path);
                    yarnScriptContents += "\n";
                }
                else
                {
                    // We shouldn't get here, assuming the file browser works properly, but it doesn't hurt to make sure.
                    Debug.LogErrorFormat("'{0}' is not a valid file or directory.");
                }
            }

            // Pass the contents of yarn script(s) to the Dialogue Runner's dynamic loader so it can handle compilation.
            dialogueRunner.GetComponent<DialogueRunner>().Stop();
            HideOptionsList();
            dialogueRunner.GetComponent<DynamicYarnLoader>().LoadScript(yarnScriptContents);
            dialogueRunner.GetComponent<DialogueRunner>().StartDialogue("Start");
            RefreshNodeDropdown();

            // TODO: Repurpose the debug settings code or disable it entirely for now. It looks like the simple file browser automatically keeps track of
            // the last directory it was used in, and having a 'load subfolder' toggle doesn't seem useful now that the filebrowser is much simpler to use.
            //SaveSettings();
        }

        // This coroutine is done, so nullify the reference so everyone else knows.
        fileBrowserCoroutine = null;
    }
}
