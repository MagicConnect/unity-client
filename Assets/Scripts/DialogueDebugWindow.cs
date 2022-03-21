using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Yarn.Unity;

public class DialogueDebugWindow : MonoBehaviour
{
    // The actual visible portion of the debug window.
    public GameObject displayPanel;

    // The text input field which holds the address for the desired yarn script folder.
    public GameObject workspaceInput;

    // The checkbox 
    public GameObject loadSubfoldersToggle;

    // The Yarn dialogue runner that we're going to load scripts into.
    public GameObject dialogueRunner;

    // Start is called before the first frame update
    void Start()
    {
        displayPanel.SetActive(false);
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
        //string workspace = workspaceInput.GetComponent<TMP_InputField>().text;
        
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
            //LoadYarnScripts(yarnScripts);
            dialogueRunner.GetComponent<DialogueRunner>().Stop();
            dialogueRunner.GetComponent<DynamicYarnLoader>().LoadScript(contents);
            dialogueRunner.GetComponent<DialogueRunner>().StartDialogue("Start");
        }
    }

    public void OnReloadAllScriptsClicked()
    {

    }

    public void ToggleDebugWindow()
    {
        displayPanel.SetActive(!displayPanel.activeInHierarchy);
    }

    private void LoadYarnScripts(IEnumerable<string> scripts)
    {
        foreach(string scriptPath in scripts)
        {
            Debug.Log(scriptPath);
        }
    }
}
