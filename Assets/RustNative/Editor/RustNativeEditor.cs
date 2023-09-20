using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

public class RustNativeEditor : EditorWindow
{
    private static RustNativeConfig _config;
    public static RustNativeConfig Config
    {
        get
        {
            if (_config != null) return _config;
            _config = new RustNativeConfig();
            _config.SyncDisk();
            return _config;
        }
    }

    [SerializeField] private VisualTreeAsset mainUXML;
    [SerializeField] private VisualTreeAsset projectUXML;

    public static readonly string UnityFolder = Path.GetDirectoryName(Application.dataPath);
    public static readonly string RustNativeFolder = Path.Combine(UnityFolder, "RustNative");

    [MenuItem("Window/Rust Native Manager")]
    public static void ShowWindow()
    {
        GetWindow<RustNativeEditor>(title: "Rust Native");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Add(mainUXML.Instantiate());

        root.Q<Button>("refresh").RegisterCallback<ClickEvent>(e => ReloadProjectList());
        root.Q<Button>("new-project").RegisterCallback<ClickEvent>(e =>
            GetWindow<RustNativeNewProjectEditor>(title: "New Rust Native Project", utility: true)
        );

        Label cargoError = root.Q<Label>("error-text");
        TextField cargoLocation = root.Q<TextField>("cargo-location");
        cargoLocation.RegisterValueChangedCallback(e => Config.CargoLocation = e.newValue);
        root.Q<Button>("cargo-find").RegisterCallback<ClickEvent>(e =>
            cargoLocation.value = EditorUtility.OpenFilePanel("Select Cargo Location", "", "")
        );

        ReloadProjectList();
        Config.SyncDisk();
    }

    public static void ReloadProjectList()
    {
        if (!HasOpenInstances<RustNativeEditor>()) return;
        RustNativeEditor window = GetWindow<RustNativeEditor>("", focus: false);
        VisualElement root = window.rootVisualElement;

        ListView projectList = root.Q<ListView>("project-list");
        projectList.hierarchy.Clear();
        try
        {
            string[] projects = Directory.GetDirectories(RustNativeFolder);
            foreach (string project in projects)
            {
                string projectName = Path.GetFileName(project);
                VisualElement projectContainer = window.projectUXML.Instantiate();
                projectContainer.Q<Label>("project-label").text = projectName;
                projectContainer.Q<Button>("rebuild").RegisterCallback<ClickEvent>(e => RebuildBindings(projectName));
                projectList.hierarchy.Add(projectContainer);
            }
        }
        catch (DirectoryNotFoundException)
        {
            // do nothing
        }
    }

    public static void RebuildBindings(string projectName)
    {
        string cargoLocation = Config.CargoLocation;
        if (!ValidateCargoLocation(cargoLocation)) return;

        try
        {
            string projectDir = Path.Combine(RustNativeFolder, projectName);
            string projectUnityDir = Path.Combine(Application.dataPath, "RustNative", projectName);

            EditorUtility.DisplayProgressBar($"Rebuilding '{projectName}' Bindings", "Building library...", 0.4f);
            Process cargo = new Process();
            cargo.StartInfo.WorkingDirectory = projectDir;
            cargo.StartInfo.FileName = cargoLocation;
            cargo.StartInfo.Arguments = "build --release";
            cargo.StartInfo.RedirectStandardOutput = true;
            cargo.StartInfo.RedirectStandardError = true;
            cargo.StartInfo.UseShellExecute = false;
            cargo.Start();
            cargo.WaitForExit();
            if (cargo.ExitCode != 0) throw new Exception(cargo.StandardError.ReadToEnd());

            EditorUtility.DisplayProgressBar($"Rebuilding '{projectName}' Bindings", "Building bindings...", 0.8f);
            cargo = new Process();
            cargo.StartInfo.WorkingDirectory = projectDir;
            cargo.StartInfo.FileName = cargoLocation;
            cargo.StartInfo.Arguments = "test --release";
            cargo.StartInfo.RedirectStandardOutput = true;
            cargo.StartInfo.RedirectStandardError = true;
            cargo.StartInfo.UseShellExecute = false;
            cargo.Start();
            cargo.WaitForExit();
            if (cargo.ExitCode != 0) throw new Exception(cargo.StandardError.ReadToEnd());

            EditorUtility.DisplayProgressBar($"Rebuilding '{projectName}' Bindings", "Copying library and bind files...", 0.9f);
            if (Directory.Exists(projectUnityDir)) Directory.Delete(projectUnityDir, true);
            Directory.CreateDirectory(projectUnityDir);

            int randomId = UnityEngine.Random.Range(100000, 999999);
#if UNITY_EDITOR_LINUX
            string libraryFilename = $"lib{projectName}.so";
            string libraryUnityFilename = $"{projectName}-{randomId}.so";
#elif UNITY_EDITOR_WIN
            string libraryFilename = $"{projectName}.dll";
            string libraryUnityFilename = $"{projectName}-{randomId}.dll";
#elif UNITY_EDITOR_OSX
            string libraryFilename = $"lib{projectName}.dylib";
            string libraryUnityFilename = $"{projectName}-{randomId}.dylib";
#endif

            string libraryUnityName = $"{projectName}-{randomId}";
            string targetDir = Path.Combine(projectDir, "target", "release");
            string libraryPath = Path.Combine(targetDir, libraryFilename);
            File.Copy(libraryPath, Path.Combine(projectUnityDir, libraryUnityFilename));

            string bindingFilename = $"{projectName}.cs";
            string bindingPath = Path.Combine(Path.Combine(projectDir, "bindings", "csharp"), bindingFilename);
            string bindingUnityPath = Path.Combine(projectUnityDir, bindingFilename);
            string bindingText = File.ReadAllText(bindingPath);
            bindingText = bindingText.Replace($"public const string NativeLib = \"{projectName}\";", $"public const string NativeLib = \"{libraryUnityName}\";");
            File.WriteAllText(bindingUnityPath, bindingText);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"Rust Native Rebuild Binding Error: {e.Message}");
            return;
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
    }

    private void OnEnable() => Config.configChanged += UpdateWithConfig;
    private void OnDisable() => Config.configChanged -= UpdateWithConfig;
    private void UpdateWithConfig(RustNativeConfig config)
    {
        if (!EditorWindow.HasOpenInstances<RustNativeEditor>()) return;
        RustNativeEditor window = EditorWindow.GetWindow<RustNativeEditor>("", focus: false);
        VisualElement root = window.rootVisualElement;
        Label cargoError = root.Q<Label>("error-text");
        TextField cargoLocation = root.Q<TextField>("cargo-location");
        cargoLocation.value = config.CargoLocation;
        if (!ValidateCargoLocation(config.CargoLocation))
            cargoError.text = "Invalid cargo location.";
        else cargoError.text = "";
    }

    public static bool ValidateCargoLocation(string cargoLocation)
    {
        if (cargoLocation == null || cargoLocation.Equals("")) return false;

        Process cargo = new Process();
        cargo.StartInfo.FileName = cargoLocation;
        cargo.StartInfo.Arguments = "-V";
        cargo.StartInfo.RedirectStandardOutput = true;
        cargo.StartInfo.RedirectStandardError = true;
        cargo.StartInfo.UseShellExecute = false;

        try
        {
            cargo.Start();
        }
        catch (Exception)
        {
            return false;
        }

        cargo.WaitForExit();
        string output = cargo.StandardOutput.ReadToEnd();
        string regex = "^cargo \\d+\\.\\d+\\.\\d+ \\([\\w\\d]+ \\d{4}-\\d{2}-\\d{2}\\)$";
        if (!Regex.IsMatch(output, regex)) return false;
        return true;
    }
}

public class RustNativeConfig
{
    public static readonly string ConfigLocation = Path.Combine(RustNativeEditor.RustNativeFolder, "config.json");

    [SerializeField] private string cargoLocation;

    public event ConfigChanged configChanged;
    public delegate void ConfigChanged(RustNativeConfig config);

    public string CargoLocation
    {
        get => cargoLocation;
        set
        {
            cargoLocation = value;
            configChanged?.Invoke(this);
            Save();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(RustNativeEditor.RustNativeFolder);
        string jsonText = JsonUtility.ToJson(this, true);
        using (StreamWriter writer = new StreamWriter(ConfigLocation, false)) writer.Write(jsonText);
    }

    public void SyncDisk()
    {
        Directory.CreateDirectory(RustNativeEditor.RustNativeFolder);
        if (!File.Exists(ConfigLocation)) Save();
        string jsonText = File.ReadAllText(ConfigLocation);
        RustNativeConfig newData = JsonUtility.FromJson<RustNativeConfig>(jsonText);
        cargoLocation = newData.cargoLocation;
        configChanged?.Invoke(this);
    }
}
