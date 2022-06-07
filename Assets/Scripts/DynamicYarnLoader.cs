using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Yarn.Compiler;
using Yarn.Unity;

public class DynamicYarnLoader : MonoBehaviour
{
    private DialogueRunner dr;

    public List<string> parseErrorMessages = new List<string>();

    public bool isSuccessfullyParsed = false;

    public void Awake()
    {
        dr = GetComponent<DialogueRunner>();
    }

    // Loads the text from a single Yarn script and then passes it into the LoadScript() method.
    public void LoadScriptFile(string path)
    {
        if(File.Exists(path) && Path.HasExtension(".yarn"))
        {
            // Load the yarn file and add its contents to the script.
            Debug.LogFormat("Loading Yarnspinner script '{0}'.", path);
            string yarnScriptContents = "";
            yarnScriptContents += File.ReadAllText(path);
            yarnScriptContents += "\n";

            LoadScript(yarnScriptContents);
        }
        else
        {
            Debug.LogErrorFormat("'{0}' is not a valid path to a Yarn Spinner script file.", path);
        }
    }

    // Loads the text from all Yarn scripts in a given directory and passes them to the LoadScript() method.
    public void LoadScriptDirectory(string path)
    {
        if(Directory.Exists(path))
        {
            // Enumerate all yarn files in the given directory, then add them to the script.
            Debug.LogFormat("Loading all Yarnspinner scripts in '{0}'", path);
            string yarnScriptContents = "";
            var yarnScripts = Directory.EnumerateFiles(path, "*.yarn", SearchOption.TopDirectoryOnly);

            foreach (string script in yarnScripts)
            {
                Debug.LogFormat("Loading Yarnspinner script '{0}'.", script);
                yarnScriptContents += File.ReadAllText(script);
                yarnScriptContents += "\n";
            }

            LoadScript(yarnScriptContents);
        }
        else
        {
            Debug.LogErrorFormat("'{0}' is not a valid path to a directory containing Yarn Spinner script files.", path);
        }
    }

    // Compiles a given yarn script, creates a new yarn project from it, and gives it to the dialogue runner.
    public void LoadScript(string sourceText, string assetPath = "default.yarn")
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        
        Yarn.Program compiledProgram = null;
        IDictionary<string, Yarn.Compiler.StringInfo> stringTable = null;
        
        // Compile the source code into a compiled Yarn program (or
        // generate a parse error)
        var compilationJob = CompilationJob.CreateFromString(fileName, sourceText, null);
        compilationJob.CompilationType = CompilationJob.Type.FullCompilation;

        var result = Yarn.Compiler.Compiler.Compile(compilationJob);

        IEnumerable<Diagnostic> errors = result.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

        if (errors.Count() > 0)
        {
            Debug.LogError("Bad");
            /*parseErrorMessages.AddRange(errors.Select(e => {
                string message = $"{ctx.assetPath}: {e}";
                ctx.LogImportError($"Error importing {message}");
                return message;
            }));*/
        }
        else
        {
            stringTable = result.StringTable;
            compiledProgram = result.Program;

            YarnProject newProject = YarnProject.CreateInstance<YarnProject>();
            byte[] compiledBytes = null;

            using (var memoryStream = new MemoryStream())
            using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
            {
                // Serialize the compiled program to memory
                compiledProgram.WriteTo(outputStream);
                outputStream.Flush();

                compiledBytes = memoryStream.ToArray();
            }

            newProject.compiledYarnProgram = compiledBytes;
            newProject.name = "NAME";
            // Dont know what these do. They were in the proper projects
            /*yp.searchAssembliesForActions.Add("YarnSpinner.Unity");
            yp.searchAssembliesForActions.Add("YarnSpinner.Unity.Editor");
            yp.searchAssembliesForActions.Add("YarnSpinner.Editor.Tests");
            yp.searchAssembliesForActions.Add("CommandsInAnAssemblyDefinition");
            yp.searchAssembliesForActions.Add("YarnSpinnerTests");
            yp.searchAssembliesForActions.Add("PsdPlugin");
            yp.searchAssembliesForActions.Add("DocCodeExamples");*/

            var newLocalization = ScriptableObject.CreateInstance<Localization>();
            string defaultLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
            newLocalization.LocaleCode = defaultLanguage;

            var stringTableEntries = stringTable.Select(x => new StringTableEntry
            {
                ID = x.Key,
                Language = defaultLanguage,
                Text = x.Value.text,
                File = x.Value.fileName,
                Node = x.Value.nodeName,
                LineNumber = x.Value.lineNumber.ToString(),
                Lock = GetHashString(x.Value.text, 8),
            });
            // Add these new lines to the localisation's asset
            foreach (var entry in stringTableEntries)
            {
                newLocalization.AddLocalizedString(entry.ID, entry.Text);
            }
            
            newProject.baseLocalization = newLocalization;
            newProject.localizations.Add(newLocalization);
            newLocalization.name = defaultLanguage;

            dr.SetProject(newProject);
        }
    }

    public void LoadScript_New(string sourceText, string assetPath = "default.yarn")
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);

        Yarn.Program compiledProgram = null;
        IDictionary<string, Yarn.Compiler.StringInfo> stringTable = null;

        parseErrorMessages.Clear();

        // Compile the source code into a compiled Yarn program (or
        // generate a parse error)
        var compilationJob = CompilationJob.CreateFromString(fileName, sourceText, null);
        compilationJob.CompilationType = CompilationJob.Type.FullCompilation;

        var result = Yarn.Compiler.Compiler.Compile(compilationJob);

        IEnumerable<Diagnostic> errors = result.Diagnostics.Where(d => d.Severity == Diagnostic.DiagnosticSeverity.Error);

        if (errors.Count() > 0)
        {
            isSuccessfullyParsed = false;

            parseErrorMessages.AddRange(errors.Select(e =>
            {
                string message = $"{assetPath}: {e}";
                //ctx.LogImportError($"Error importing {message}");
                return message;
            }));
        }
        else
        {
            isSuccessfullyParsed = true;
            //LastImportHadImplicitStringIDs = result.ContainsImplicitStringTags;
            //LastImportHadAnyStrings = result.StringTable.Count > 0;

            stringTable = result.StringTable;
            compiledProgram = result.Program;

            YarnProject newProject = YarnProject.CreateInstance<YarnProject>();
            byte[] compiledBytes = null;

            using (var memoryStream = new MemoryStream())
            using (var outputStream = new Google.Protobuf.CodedOutputStream(memoryStream))
            {
                // Serialize the compiled program to memory
                compiledProgram.WriteTo(outputStream);
                outputStream.Flush();

                compiledBytes = memoryStream.ToArray();
            }

            newProject.compiledYarnProgram = compiledBytes;
            newProject.name = "NAME";
            // Dont know what these do. They were in the proper projects
            /*yp.searchAssembliesForActions.Add("YarnSpinner.Unity");
            yp.searchAssembliesForActions.Add("YarnSpinner.Unity.Editor");
            yp.searchAssembliesForActions.Add("YarnSpinner.Editor.Tests");
            yp.searchAssembliesForActions.Add("CommandsInAnAssemblyDefinition");
            yp.searchAssembliesForActions.Add("YarnSpinnerTests");
            yp.searchAssembliesForActions.Add("PsdPlugin");
            yp.searchAssembliesForActions.Add("DocCodeExamples");*/

            dr.SetProject(newProject);
        }
    }

    // from YarnImporter.cs, copied due to access levels
    public static string GetHashString(string inputString, int limitCharacters = -1)
    {
        var sb = new StringBuilder();
        foreach (byte b in GetHash(inputString))
        {
            sb.Append(b.ToString("x2"));
        }

        if (limitCharacters == -1)
        {
            // Return the entire string
            return sb.ToString();
        }
        else
        {
            // Return a substring (or the entire string, if
            // limitCharacters is longer than the string)
            return sb.ToString(0, Mathf.Min(sb.Length, limitCharacters));
        }
    }

    // from YarnImporter.cs, copied due to access levels
    public static byte[] GetHash(string inputString)
    {
        using (HashAlgorithm algorithm = SHA256.Create())
        {
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }
    }

    public void Update()
    {
        /*if(Input.GetKeyUp(KeyCode.Q))
        {
            LoadScript();
            if (dr.Dialogue != null && dr.Dialogue.IsActive)
                dr.Stop();
            dr.StartDialogue("Start");
        }*/
    }
}