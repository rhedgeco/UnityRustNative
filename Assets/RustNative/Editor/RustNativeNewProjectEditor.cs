using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

public class RustNativeNewProjectEditor : EditorWindow
{
    [SerializeField] private VisualTreeAsset mainUXML;

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;
        root.Add(mainUXML.Instantiate());

        TextField projectName = root.Q<TextField>("project-name");
        root.Q<Button>("create").RegisterCallback<ClickEvent>(e =>
        {
            string errorText = CreateProject(projectName.value);
            root.Q<Label>("error-text").text = errorText;
            if (!errorText.Equals("")) Debug.LogError($"Rust Native New Project Error:\n{errorText}");
            else Close();
        });
    }

    public static string CreateProject(string projectName)
    {
        string cargoLocation = RustNativeEditor.Config.CargoLocation;
        if (!RustNativeEditor.ValidateCargoLocation(cargoLocation)) return "Invalid cargo location";

        try
        {
            EditorUtility.DisplayProgressBar("Creating New Rust Native Project", "Extracting template...", 0.2f);
            string projectTemplateZip = Path.Combine(Path.Combine(Application.dataPath, "RustNative", "Editor"), "template_rust_library.zip");
            ZipFile.ExtractToDirectory(projectTemplateZip, RustNativeEditor.RustNativeFolder);

            EditorUtility.DisplayProgressBar("Creating New Rust Native Project", "Renaming template...", 0.5f);
            string templateDir = Path.Combine(RustNativeEditor.RustNativeFolder, "template_rust_library");
            string projectDir = Path.Combine(RustNativeEditor.RustNativeFolder, projectName);
            Directory.Move(templateDir, projectDir);

            EditorUtility.DisplayProgressBar("Creating New Rust Native Project", "Updating Cargo TOML...", 0.6f);
            string tomlFile = Path.Combine(projectDir, "Cargo.toml");
            string tomlText = File.ReadAllText(tomlFile);
            tomlText = tomlText.Replace("template_rust_library", projectName);
            File.WriteAllText(tomlFile, tomlText);

            EditorUtility.DisplayProgressBar("Creating New Rust Native Project", "Updating binding generator...", 0.7f);
            string testFile = Path.Combine(projectDir, "tests", "bindings.rs");
            string testText = File.ReadAllText(testFile);
            testText = testText.Replace("template_rust_library", projectName);
            File.WriteAllText(testFile, testText);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Directory.Delete(Path.Combine(RustNativeEditor.RustNativeFolder, "template_rust_library"));
            return e.Message;
        }

        EditorUtility.ClearProgressBar();
        RustNativeEditor.ReloadProjectList();
        return "";
    }
}
