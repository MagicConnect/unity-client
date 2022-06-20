using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityBuilderAction
{
    public static class CutsceneStandaloneBuildScript
    {
        private static readonly string Eol = Environment.NewLine;

        private static readonly string[] Secrets =
            {"androidKeystorePass", "androidKeyaliasName", "androidKeyaliasPass"};

        public static void Build()
        {
            // Gather values from args
            Dictionary<string, string> options = GetValidatedOptions();

            // Set version for this build
            PlayerSettings.bundleVersion = options["buildVersion"];
            PlayerSettings.macOS.buildNumber = options["buildVersion"];
            //PlayerSettings.Android.bundleVersionCode = int.Parse(options["androidVersionCode"]);

            // Apply build target
            var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                    if (options.TryGetValue("androidKeystoreName", out string keystoreName) &&
                        !string.IsNullOrEmpty(keystoreName))
                        PlayerSettings.Android.keystoreName = keystoreName;
                    if (options.TryGetValue("androidKeystorePass", out string keystorePass) &&
                        !string.IsNullOrEmpty(keystorePass))
                        PlayerSettings.Android.keystorePass = keystorePass;
                    if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) &&
                        !string.IsNullOrEmpty(keyaliasName))
                        PlayerSettings.Android.keyaliasName = keyaliasName;
                    if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) &&
                        !string.IsNullOrEmpty(keyaliasPass))
                        PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    break;
                }
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }

            bool developmentMode = options.TryGetValue("Development", out string _) || options.TryGetValue("development", out string _);

            // Custom build
            Build(buildTarget, options["customBuildPath"], developmentMode);
        }

        private static Dictionary<string, string> GetValidatedOptions()
        {
            ParseCommandLineArguments(out Dictionary<string, string> validatedOptions);

            if (!validatedOptions.TryGetValue("projectPath", out string _))
            {
                Console.WriteLine("Missing argument -projectPath");
                EditorApplication.Exit(110);
            }

            if (!validatedOptions.TryGetValue("buildTarget", out string buildTarget))
            {
                Console.WriteLine("Missing argument -buildTarget");
                EditorApplication.Exit(120);
            }

            if (!Enum.IsDefined(typeof(BuildTarget), buildTarget ?? string.Empty))
            {
                EditorApplication.Exit(121);
            }

            if (!validatedOptions.TryGetValue("customBuildPath", out string _))
            {
                Console.WriteLine("Missing argument -customBuildPath");
                EditorApplication.Exit(130);
            }

            const string defaultCustomBuildName = "TestBuild";
            if (!validatedOptions.TryGetValue("customBuildName", out string customBuildName))
            {
                Console.WriteLine($"Missing argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }
            else if (customBuildName == "")
            {
                Console.WriteLine($"Invalid argument -customBuildName, defaulting to {defaultCustomBuildName}.");
                validatedOptions.Add("customBuildName", defaultCustomBuildName);
            }

            return validatedOptions;
        }

        private static void ParseCommandLineArguments(out Dictionary<string, string> providedArguments)
        {
            providedArguments = new Dictionary<string, string>();
            string[] args = Environment.GetCommandLineArgs();

            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#    Parsing settings     #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}"
            );

            // Extract flags with optional values
            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                // Parse flag
                bool isFlag = args[current].StartsWith("-");
                if (!isFlag) continue;
                string flag = args[current].TrimStart('-');

                // Parse optional value
                bool flagHasValue = next < args.Length && !args[next].StartsWith("-");
                string value = flagHasValue ? args[next].TrimStart('-') : "";
                bool secret = Secrets.Contains(flag);
                string displayValue = secret ? "*HIDDEN*" : "\"" + value + "\"";

                // Assign
                Console.WriteLine($"Found flag \"{flag}\" with value {displayValue}.");
                providedArguments.Add(flag, value);
            }
        }

        private static void Build(BuildTarget buildTarget, string filePath, bool developmentMode)
        {
            //string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            string[] scenes = new string[]{"Assets/Scenes/Loading.unity", "Assets/Scenes/Cutscene.unity"};

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                target = buildTarget,
//                targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget),
                locationPathName = filePath,
//                options = UnityEditor.BuildOptions.Development
            };

            // Build the list of custom scripting definitions before passing them into the build player options.
            List<string> scriptingDefines = new List<string>();
            scriptingDefines.Add("CUTSCENE_ONLY_BUILD");

            // We can't use development mode in the custom build for some reason, but we can set a definition to enable
            // the ingame debug console.
            if(developmentMode)
            {
                scriptingDefines.Add("USE_CUSTOM_INGAME_DEBUG_CONSOLE");
            }

            buildPlayerOptions.extraScriptingDefines = scriptingDefines.ToArray();
            //buildPlayerOptions.options = BuildOptions.Development;

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }

        private static void ReportSummary(BuildSummary summary)
        {
            Console.WriteLine(
                $"{Eol}" +
                $"###########################{Eol}" +
                $"#      Build results      #{Eol}" +
                $"###########################{Eol}" +
                $"{Eol}" +
                $"Duration: {summary.totalTime.ToString()}{Eol}" +
                $"Warnings: {summary.totalWarnings.ToString()}{Eol}" +
                $"Errors: {summary.totalErrors.ToString()}{Eol}" +
                $"Size: {summary.totalSize.ToString()} bytes{Eol}" +
                $"{Eol}"
            );
        }

        private static void ExitWithResult(BuildResult result)
        {
            switch (result)
            {
                case BuildResult.Succeeded:
                    Console.WriteLine("Build succeeded!");
                    EditorApplication.Exit(0);
                    break;
                case BuildResult.Failed:
                    Console.WriteLine("Build failed!");
                    EditorApplication.Exit(101);
                    break;
                case BuildResult.Cancelled:
                    Console.WriteLine("Build cancelled!");
                    EditorApplication.Exit(102);
                    break;
                case BuildResult.Unknown:
                default:
                    Console.WriteLine("Build result is unknown!");
                    EditorApplication.Exit(103);
                    break;
            }
        }

        [MenuItem("MyTools/Build Cutscene Only - Current Target")]
        public static void BuildCutsceneLocalCurrentTarget()
        {
            BuildCutsceneLocal(EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem("MyTools/Build Cutscene Only - Windows64")]
        public static void BuildCutsceneLocalWindows64()
        {
            BuildCutsceneLocal(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("MyTools/Build Cutscene Only - Linux64")]
        public static void BuildCutsceneLocalLinux64()
        {
            BuildCutsceneLocal(BuildTarget.StandaloneLinux64);
        }

        [MenuItem("MyTools/Build Cutscene Only - OSX")]
        public static void BuildCutsceneLocalOSX()
        {
            BuildCutsceneLocal(BuildTarget.StandaloneOSX);
        }

        public static void BuildCutsceneLocal(BuildTarget buildTarget)
        {
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new string[]{"Assets/Scenes/Loading.unity", "Assets/Scenes/Cutscene.unity"};
            buildPlayerOptions.target = buildTarget;
            buildPlayerOptions.options = BuildOptions.None;
            buildPlayerOptions.extraScriptingDefines = new string[]{"CUTSCENE_ONLY_BUILD"};

            switch(buildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                    buildPlayerOptions.locationPathName = Path.Combine(Application.dataPath, "../", "Build/StandaloneWindows64/MagicConnect_CutsceneOnly.exe");
                    break;
                case BuildTarget.StandaloneLinux64:
                    buildPlayerOptions.locationPathName = Path.Combine(Application.dataPath, "../", "Build/StandaloneLinux64/MagicConnect_CutsceneOnly");
                    break;
                case BuildTarget.StandaloneOSX:
                    buildPlayerOptions.locationPathName = Path.Combine(Application.dataPath, "../", "Build/StandaloneOSX/MagicConnect_CutsceneOnly");
                    break;
                default:
                    Debug.LogErrorFormat("Cutscene Only Build for build target '{0}' not yet supported. Using default locationPathName.");
                    buildPlayerOptions.locationPathName = Path.Combine(Application.dataPath, "../", "Build/MagicConnect_CutsceneOnly");
                    break;
            }

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if(summary.result == BuildResult.Succeeded)
            {
                Debug.LogFormat("Cutscene Only Build succeeded: {0} bytes", summary.totalSize);
            }

            if(summary.result == BuildResult.Failed)
            {
                Debug.LogFormat("Cutscene Only Build failed");
            }
        }

        public static void BuildCutsceneRemote()
        {
            // Gather values from args
            Dictionary<string, string> options = GetValidatedOptions();

            // Apply build target
            var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            /*
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                    if (options.TryGetValue("androidKeystoreName", out string keystoreName) &&
                        !string.IsNullOrEmpty(keystoreName))
                        PlayerSettings.Android.keystoreName = keystoreName;
                    if (options.TryGetValue("androidKeystorePass", out string keystorePass) &&
                        !string.IsNullOrEmpty(keystorePass))
                        PlayerSettings.Android.keystorePass = keystorePass;
                    if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) &&
                        !string.IsNullOrEmpty(keyaliasName))
                        PlayerSettings.Android.keyaliasName = keyaliasName;
                    if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) &&
                        !string.IsNullOrEmpty(keyaliasPass))
                        PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    break;
                }
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }
            */

            // Custom build
            //Build(buildTarget, options["customBuildPath"]);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new string[]{"Assets/Scenes/Loading.unity", "Assets/Scenes/Cutscene.unity"};
            buildPlayerOptions.target = buildTarget;
            buildPlayerOptions.locationPathName = options["customBuildPath"];
            buildPlayerOptions.options = BuildOptions.Development;
            buildPlayerOptions.extraScriptingDefines = new string[]{"CUTSCENE_ONLY_BUILD"};

            BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            ReportSummary(buildSummary);
            ExitWithResult(buildSummary.result);
        }

        [MenuItem("MyTools/Build Cutscene Only - Remote Windows")]
        public static void BuildCutsceneRemoteWindows64()
        {
            // Gather values from args
            //Dictionary<string, string> options = GetValidatedOptions();

            // Apply build target
            //var buildTarget = (BuildTarget) Enum.Parse(typeof(BuildTarget), options["buildTarget"]);
            /*
            switch (buildTarget)
            {
                case BuildTarget.Android:
                {
                    EditorUserBuildSettings.buildAppBundle = options["customBuildPath"].EndsWith(".aab");
                    if (options.TryGetValue("androidKeystoreName", out string keystoreName) &&
                        !string.IsNullOrEmpty(keystoreName))
                        PlayerSettings.Android.keystoreName = keystoreName;
                    if (options.TryGetValue("androidKeystorePass", out string keystorePass) &&
                        !string.IsNullOrEmpty(keystorePass))
                        PlayerSettings.Android.keystorePass = keystorePass;
                    if (options.TryGetValue("androidKeyaliasName", out string keyaliasName) &&
                        !string.IsNullOrEmpty(keyaliasName))
                        PlayerSettings.Android.keyaliasName = keyaliasName;
                    if (options.TryGetValue("androidKeyaliasPass", out string keyaliasPass) &&
                        !string.IsNullOrEmpty(keyaliasPass))
                        PlayerSettings.Android.keyaliasPass = keyaliasPass;
                    break;
                }
                case BuildTarget.StandaloneOSX:
                    PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                    break;
            }
            */

            // Custom build
            //Build(buildTarget, options["customBuildPath"]);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new string[]{"Assets/Scenes/Loading.unity", "Assets/Scenes/Cutscene.unity"};
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.locationPathName = "Build/StandaloneWindows64/MagicConnect_CutsceneOnly.exe";
            buildPlayerOptions.options = BuildOptions.Development;
            buildPlayerOptions.extraScriptingDefines = new string[]{"CUTSCENE_ONLY_BUILD"};

            //BuildSummary buildSummary = BuildPipeline.BuildPlayer(buildPlayerOptions).summary;
            //ReportSummary(buildSummary);
            //ExitWithResult(buildSummary.result);

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            ReportSummary(summary);
        }
    }
}