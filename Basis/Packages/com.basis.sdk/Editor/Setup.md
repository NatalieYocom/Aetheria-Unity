#if UNITY_EDITOR
using System;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

[InitializeOnLoad]
public static class Setup
{
    private const string POIYOMI_URP_URL = "https://github.com/poiyomi/PoiyomiToonShader/releases/download/V8.1.161/PoiyomiToonURP_8.1.161.unitypackage";
    private const string URP_PACKAGE_ID = "com.unity.render-pipelines.universal";
    private const string FIRST_INSTALL_KEY = "AetheriaSDK_FirstInstallCompleted";

    private static AddRequest addPackageRequest;

    static Setup()
    {
        // Subscribe to Unity editor startup / package load event
        EditorApplication.delayCall += OnEditorBoot;
    }

    private static void OnEditorBoot()
    {
        // 1. Check if this is the very first install of the Aetheria SDK
        if (!EditorPrefs.GetBool(FIRST_INSTALL_KEY, false))
        {
            Debug.Log("[Aetheria SDK] First installation detected. Running initial environment setup...");
            EditorPrefs.SetBool(FIRST_INSTALL_KEY, true);
            RunPoiyomiURPCheck(isManualTrigger: false);
            return;
        }

        // 2. Standard Unity boot check: ensure Poiyomi URP is present
        if (!IsPoiyomiURPInstalled())
        {
            Debug.Log("[Aetheria SDK] Poiyomi URP missing on startup. Prompting user for download...");
            RunPoiyomiURPCheck(isManualTrigger: false);
        }
    }

    /// <summary>
    /// Manual trigger accessible via the topbar menu item.
    /// </summary>
    [MenuItem("Aetheria/SDK/Check & Download Poiyomi URP", false, 10)]
    public static void ManualCheckAndDownload()
    {
        RunPoiyomiURPCheck(isManualTrigger: true);
    }

    public static void RunPoiyomiURPCheck(bool isManualTrigger)
    {
        if (IsPoiyomiURPInstalled())
        {
            if (isManualTrigger)
            {
                EditorUtility.DisplayDialog(
                    "Aetheria SDK Setup",
                    "Poiyomi Toon URP is already installed and ready to use in this project!",
                    "OK"
                );
            }
            return;
        }

        bool proceed = EditorUtility.DisplayDialog(
            "Poiyomi URP Required",
            "Poiyomi URP shaders were not found in your project.\n\nWould you like to download and install Poiyomi Toon URP automatically now?",
            "Download & Install",
            "Cancel"
        );

        if (proceed)
        {
            EnsureURPAndDownloadPoiyomi();
        }
    }

    private static bool IsPoiyomiURPInstalled()
    {
        return Shader.Find("Poiyomi/Poiyomi Toon URP") != null ||
               Shader.Find(".poiyomi/Poiyomi Toon URP") != null ||
               Shader.Find("Poiyomi/Poiyomi Pro URP") != null;
    }

    private static void EnsureURPAndDownloadPoiyomi()
    {
        // First ensure Universal Render Pipeline package is added to Package Manager
        if (Shader.Find("Universal Render Pipeline/Lit") == null)
        {
            Debug.Log("[Aetheria SDK] Installing URP via Package Manager...");
            addPackageRequest = Client.Add(URP_PACKAGE_ID);
            EditorApplication.update += MonitorURPInstallation;
        }
        else
        {
            DownloadPoiyomiPackage();
        }
    }

    private static void MonitorURPInstallation()
    {
        if (addPackageRequest == null) return;

        if (addPackageRequest.IsCompleted)
        {
            if (addPackageRequest.Status == StatusCode.Success)
            {
                Debug.Log($"[Aetheria SDK] Successfully installed {addPackageRequest.Result.packageId}");
                AssetDatabase.Refresh();
                DownloadPoiyomiPackage();
            }
            else if (addPackageRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError($"[Aetheria SDK] URP Installation failed: {addPackageRequest.Error.message}");
                EditorUtility.DisplayDialog("Error", $"Failed to install URP package: {addPackageRequest.Error.message}", "OK");
            }

            EditorApplication.update -= MonitorURPInstallation;
            addPackageRequest = null;
        }
    }

    private static void DownloadPoiyomiPackage()
    {
        string tempPath = Path.Combine(Application.temporaryCachePath, "PoiyomiToonURP.unitypackage");

        try
        {
            EditorUtility.DisplayProgressBar("Aetheria SDK Setup", "Downloading latest Poiyomi Toon URP...", 0.4f);
            Debug.Log($"[Aetheria SDK] Downloading package from {POIYOMI_URP_URL}...");

            using (WebClient client = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                client.DownloadFile(POIYOMI_URP_URL, tempPath);
            }

            EditorUtility.DisplayProgressBar("Aetheria SDK Setup", "Importing Poiyomi Toon URP...", 0.8f);
            Debug.Log("[Aetheria SDK] Importing unitypackage...");

            AssetDatabase.ImportPackage(tempPath, false);

            EditorUtility.ClearProgressBar();
            Debug.Log("[Aetheria SDK] Poiyomi Toon URP imported successfully.");

            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Aetheria SDK] Setup error: {ex.Message}");
            EditorUtility.DisplayDialog("Setup Error", $"Failed to download/import Poiyomi URP:\n{ex.Message}", "OK");
        }
    }
}
#endif
