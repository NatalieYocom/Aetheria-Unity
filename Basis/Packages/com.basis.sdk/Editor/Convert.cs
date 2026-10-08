#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Master avatar conversion pipeline. Invokes Watari to process avatar components,
/// then iterates through all materials to upgrade Poiyomi shaders to Poiyomi URP.
/// </summary>
public static class Convert
{
    public static void ExecuteConversion(GameObject avatarObj)
    {
        if (avatarObj == null)
        {
            Debug.LogError("[Aetheria SDK] Conversion failed: Target GameObject is null.");
            return;
        }

        EditorUtility.DisplayProgressBar("Aetheria Avatar Conversion", "Step 1/2: Running Watari Converter...", 0.3f);

        try
        {
            // 1. Execute Watari Conversion
            bool watariSuccess = RunWatariConversion(avatarObj);
            if (!watariSuccess)
            {
                Debug.LogWarning("[Aetheria SDK] Watari conversion ended with warnings or was skipped. Proceeding to material pass...");
            }

            EditorUtility.DisplayProgressBar("Aetheria Avatar Conversion", "Step 2/2: Converting Materials to Poiyomi URP...", 0.7f);

            // 2. Convert all materials from Standard Poiyomi to Poiyomi URP
            ConvertAvatarMaterialsToPoiyomiURP(avatarObj);

            // 3. Attach Aetheria Avatar Descriptor marker if missing
            EnsureAetheriaDescriptorAttached(avatarObj);

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Conversion Complete", $"Successfully converted '{avatarObj.name}' to an Aetheria Avatar!", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[Aetheria SDK] Avatar conversion encountered an error: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    /// Invokes Watari's conversion API using reflection so it compiles cleanly even if Watari is dynamically loaded.
    /// </summary>
    private static bool RunWatariConversion(GameObject avatarObj)
    {
        Type watariType = FindTypeInAssemblies("WatariConverter") ?? 
                          FindTypeInAssemblies("Watari.WatariConverter");

        if (watariType == null)
        {
            Debug.LogError("[Aetheria SDK] Watari Converter package was not found. Please install Watari via Aetheria > SDK > Check & Install Dependencies.");
            return false;
        }

        MethodInfo convertMethod = watariType.GetMethod("Convert", BindingFlags.Public | BindingFlags.Static) ??
                                  watariType.GetMethod("ConvertAvatar", BindingFlags.Public | BindingFlags.Static) ??
                                  watariType.GetMethod("Process", BindingFlags.Public | BindingFlags.Static);

        if (convertMethod != null)
        {
            Debug.Log($"[Aetheria SDK] Executing Watari conversion on '{avatarObj.name}'...");
            convertMethod.Invoke(null, new object[] { avatarObj });
            return true;
        }

        // Alternative: Look for instance-based Watari component execution
        Component watariComp = avatarObj.GetComponent(watariType);
        if (watariComp == null) watariComp = avatarObj.AddComponent(watariType);

        MethodInfo instanceConvert = watariType.GetMethod("Convert", BindingFlags.Public | BindingFlags.Instance) ??
                                     watariType.GetMethod("Process", BindingFlags.Public | BindingFlags.Instance);

        if (instanceConvert != null)
        {
            instanceConvert.Invoke(watariComp, null);
            UnityEngine.Object.DestroyImmediate(watariComp);
            return true;
        }

        Debug.LogWarning("[Aetheria SDK] Could not locate static conversion method on WatariConverter. Running fallback material conversion.");
        return false;
    }

    /// <summary>
    /// Finds all renderers on the avatar and updates Poiyomi materials to their URP equivalent.
    /// </summary>
    private static void ConvertAvatarMaterialsToPoiyomiURP(GameObject avatarObj)
    {
        Shader poiyomiURP = Shader.Find("Poiyomi/Poiyomi Toon URP") ??
                           Shader.Find(".poiyomi/Poiyomi Toon URP") ??
                           Shader.Find("Poiyomi/Poiyomi Pro URP");

        if (poiyomiURP == null)
        {
            Debug.LogError("[Aetheria SDK] Poiyomi URP Shader not found in project! Ensure Poiyomi Toon URP unitypackage is imported.");
            return;
        }

        Renderer[] renderers = avatarObj.GetComponentsInChildren<Renderer>(true);
        HashSet<Material> processedMaterials = new HashSet<Material>();
        int convertedCount = 0;

        foreach (Renderer renderer in renderers)
        {
            Material[] sharedMats = renderer.sharedMaterials;
            for (int i = 0; i < sharedMats.Length; i++)
            {
                Material mat = sharedMats[i];
                if (mat == null || mat.shader == null || processedMaterials.Contains(mat)) continue;

                processedMaterials.Add(mat);
                string shaderName = mat.shader.name;

                // Target any Poiyomi shader variant that isn't already set to URP
                if (shaderName.Contains("Poiyomi") && !shaderName.Contains("URP"))
                {
                    Undo.RecordObject(mat, "Convert Material to Poiyomi URP");

                    // Assign Poiyomi URP shader target (preserves property names)
                    mat.shader = poiyomiURP;

                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Aetheria SDK] Replaced {convertedCount} Poiyomi material(s) with Poiyomi URP on '{avatarObj.name}'.");
    }

    private static void EnsureAetheriaDescriptorAttached(GameObject avatarObj)
    {
        Type aetheriaDescType = FindTypeInAssemblies("AetheriaAvatarDescriptor") ??
                               FindTypeInAssemblies("BasisAvatarDescriptor");

        if (aetheriaDescType != null && avatarObj.GetComponent(aetheriaDescType) == null)
        {
            avatarObj.AddComponent(aetheriaDescType);
            Debug.Log($"[Aetheria SDK] Attached {aetheriaDescType.Name} to '{avatarObj.name}'.");
        }
    }

    private static Type FindTypeInAssemblies(string typeName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = asm.GetType(typeName);
            if (t != null) return t;
            foreach (var type in asm.GetTypes())
            {
                if (type.Name == typeName) return type;
            }
        }
        return null;
    }
}
#endif
