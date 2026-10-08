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
    private const string WATARI_GIT_URL = "https://github.com/yuna0x0/watari-basis.git";
    private const string POIYOMI_URP_URL = "https://github.com/poiyomi/PoiyomiToonShader/releases/download/V8.1.161/PoiyomiToonURP_8.1.161.unitypackage";
    private const string URP_PACKAGE_ID = "com.unity.render-pipelines.universal";

    private static AddRequest addPackageRequest;

    static Setup()
    {
        EditorApplication.delayCall += OnEditorBoot;
    }

    private static void OnEditorBoot()
    {
        // Ensure Watari Converter and Poiyomi URP are installed on project boot
        if (!IsWatariInstalled())
        {
            Debug.Log("[Aetheria SDK] Watari Basis Converter not detected. Prompting installation...");
            PromptAndInstallWatari();
        }
    }

    [MenuItem("Aetheria/SDK/Check & Install Dependencies", false, 10)]
    public static void CheckDependenciesMenu()
    {
        if (IsWatariInstalled() && IsPoiyomiURPInstalled())
        {
            EditorUtility.DisplayDialog("Aetheria SDK Setup", "All dependencies (Watari Converter & Poiyomi URP) are installed!", "OK");
            return;
        }

        if (!IsWatariInstalled()) PromptAndInstallWatari();
        if (!IsPoiyomiURPInstalled()) EnsureURPAndDownloadPoiyomi();
    }

    public static bool IsWatariInstalled()
    {
        return Type.GetType("Watari.WatariConverter, com.yuna0x0.basis.convert") != null ||
               Shader.Find("Basis/JigglePhysics") != null ||
               System.AppDomain.CurrentDomain.ToString().Contains("watari");
    }

    private static bool IsPoiyomiURPInstalled()
    {
        return Shader.Find("Poiyomi/Poiyomi Toon URP") != null ||
               Shader.Find(".poiyomi/Poiyomi Toon URP") != null;
    }

    public static void PromptAndInstallWatari()
    {
        bool install = EditorUtility.DisplayDialog(
            "Aetheria SDK - Install Watari Converter",
            "Aetheria uses 'Watari' to convert VRChat PhysBones, Expressions, and Constraints into BasisVR format.\n\nWould you like to install Watari via Unity Package Manager now?",
            "Install Watari",
            "Cancel"
        );

        if (install)
        {
            Debug.Log($"[Aetheria SDK] Installing Watari from {WATARI_GIT_URL}...");
            addPackageRequest = Client.Add(WATARI_GIT_URL);
            EditorApplication.update += MonitorWatariInstallation;
        }
    }

    private static void MonitorWatariInstallation()
    {
        if (addPackageRequest == null) return;

        if (addPackageRequest.IsCompleted)
        {
            if (addPackageRequest.Status == StatusCode.Success)
            {
                Debug.Log("[Aetheria SDK] Watari installed successfully!");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogError($"[Aetheria SDK] Failed to install Watari: {addPackageRequest.Error.message}");
            }

            EditorApplication.update -= MonitorWatariInstallation;
            addPackageRequest = null;
        }
    }

    public static void EnsureURPAndDownloadPoiyomi()
    {
        if (Shader.Find("Universal Render Pipeline/Lit") == null)
        {
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
                AssetDatabase.Refresh();
                DownloadPoiyomiPackage();
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
            EditorUtility.DisplayProgressBar("Aetheria SDK", "Downloading Poiyomi Toon URP...", 0.5f);
            using (WebClient client = new WebClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                client.DownloadFile(POIYOMI_URP_URL, tempPath);
            }
            EditorUtility.DisplayProgressBar("Aetheria SDK", "Importing Poiyomi Toon URP...", 0.9f);
            AssetDatabase.ImportPackage(tempPath, false);
            EditorUtility.ClearProgressBar();
            if (File.Exists(tempPath)) File.Delete(tempPath);
            AssetDatabase.Refresh();
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Aetheria SDK] Poiyomi download error: {ex.Message}");
        }
    }
}
#endif
