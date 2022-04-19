using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yarn.Unity;
using Newtonsoft.Json;

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

    private class DebugSettings
    {
        public string Workspace {get; set;} = "";

        public string LoadSubfolders {get; set;} = "true";
    }

    private DebugSettings settings;

    // Start is called before the first frame update
    void Start()
    {
        displayPanel.SetActive(false);

        // Create a blank settings object to be populated later.
        settings = new DebugSettings();

        // Load previously saved settings, if they exist.
        LoadSettings();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleDebugWindow();
        }
    }

    // When the 'load all scripts' button is clicked, all scripts in the designated folder are loaded, compiled into a program,
    // and added to the Dialogue Runner.
    public void OnLoadAllScriptsClicked()
    {
        Debug.Log("Load All Scripts button clicked.");
        Debug.Log(workspaceInput.GetComponent<TMP_InputField>().text);
        string path = workspaceInput.GetComponent<TMP_InputField>().text;
        
        // Check if the given path isn't a valid directory.
        if(!Directory.Exists(path))
        {
            // If not, check if it is a valid yarn script file.
            if(File.Exists(path) && Path.GetExtension(path) == ".yarn")
            {
                // If so, load the file contents and sent it to the dynamic yarn loader.
                //path = Path.GetDirectoryName(path);
                //Debug.LogWarning("Single yarn file detected and changed to a directory.");
                dialogueRunner.GetComponent<DialogueRunner>().Stop();
                HideOptionsList();
                dialogueRunner.GetComponent<DynamicYarnLoader>().LoadScript(File.ReadAllText(path));
                dialogueRunner.GetComponent<DialogueRunner>().StartDialogue("Start");
            }
            else
            {
                // If the path isn't a valid directory or file, then log an error and don't bother trying to load any scripts.
                Debug.LogError("Path given is not a valid directory or yarn script file.");
                return;
            }
        }
        else
        {
            // If the load subfolders checkbox is checked then we search all subdirectories for yarn scripts.
            IEnumerable<string> yarnScripts;
            if(loadSubfoldersToggle.GetComponent<Toggle>().isOn)
            {
                yarnScripts = Directory.EnumerateFiles(path, "*.yarn", SearchOption.AllDirectories);
            }
            // If not, just search the top directory only.
            else
            {
                yarnScripts = Directory.EnumerateFiles(path, "*.yarn", SearchOption.TopDirectoryOnly);
            }

            // Load the contents of each script file and combine them into a single Yarn script.
            string contents = "";
            foreach(string file in yarnScripts)
            {
                contents += File.ReadAllText(file);
                contents += "\n";
            }
            
            // Pass the contents of yarn script(s) to the Dialogue Runner's dynamic loader so it can handle compilation.
            dialogueRunner.GetComponent<DialogueRunner>().Stop();
            HideOptionsList();
            dialogueRunner.GetComponent<DynamicYarnLoader>().LoadScript(contents);
            dialogueRunner.GetComponent<DialogueRunner>().StartDialogue("Start");
        }

        RefreshNodeDropdown();
        SaveSettings();
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
}
