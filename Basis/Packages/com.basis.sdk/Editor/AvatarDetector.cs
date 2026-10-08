#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Monitors selected GameObjects in the Inspector to detect unconverted avatars (VRChat, VRM, etc.)
/// and prompts the user to initiate conversion via ConvertAvatar.cs.
/// </summary>
[CustomEditor(typeof(Transform))]
public class UnconvertedAvatarDetector : Editor
{
    // Types to identify as legacy/unconverted avatars
    private static readonly string[] LEGACY_DESCRIPTOR_TYPES = new string[]
    {
        "VRC.SDK3.Avatars.Components.VRCAvatarDescriptor",
        "VRCAvatarDescriptor",
        "VRM.VRMMeta",
        "VRM10.VRM10Object"
    };

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        Transform targetTransform = (Transform)target;
        if (targetTransform == null) return;

        GameObject selectedObj = targetTransform.gameObject;

        // Check if the current GameObject is an unconverted avatar
        if (IsUnconvertedAvatar(selectedObj, out string detectedType))
        {
            DrawConversionPrompt(selectedObj, detectedType);
        }
    }

    /// <summary>
    /// Checks if a GameObject has a legacy avatar descriptor and lacks an Aetheria descriptor.
    /// </summary>
    private static bool IsUnconvertedAvatar(GameObject obj, out string descriptorName)
    {
        descriptorName = string.Empty;

        // 1. If already converted (has Aetheria descriptor), skip prompt
        if (obj.GetComponent("AetheriaAvatarDescriptor") != null || 
            obj.GetComponent("BasisAvatarDescriptor") != null)
        {
            return false;
        }

        // 2. Search for any known legacy descriptor on the root GameObject
        Component[] components = obj.GetComponents<Component>();
        foreach (Component comp in components)
        {
            if (comp == null) continue;

            string fullTypeName = comp.GetType().FullName;
            string shortTypeName = comp.GetType().Name;

            foreach (string legacyType in LEGACY_DESCRIPTOR_TYPES)
            {
                if (fullTypeName == legacyType || shortTypeName == legacyType)
                {
                    descriptorName = shortTypeName;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Draws the Inspector conversion card with Cyber-Etheric Sovereign styling.
    /// </summary>
    private static void DrawConversionPrompt(GameObject targetObj, string descriptorType)
    {
        EditorGUILayout.Space(12);

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(12, 12, 10, 10),
            margin = new RectOffset(0, 0, 8, 8)
        };

        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.HelpBox(
            $"Unconverted avatar detected on '{targetObj.name}' ({descriptorType}).\n\nWould you like to convert this model to the native Aetheria SDK format?",
            MessageType.Info
        );

        EditorGUILayout.Space(6);

        // Cyber-Etheric Cyan Button
        GUI.backgroundColor = new Color(0.0f, 0.95f, 0.99f, 1.0f);
        if (GUILayout.Button("✨ Convert Avatar to Aetheria", GUILayout.Height(36)))
        {
            PromptAndExecute(targetObj);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    private static void PromptAndExecute(GameObject avatarObj)
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Convert Avatar to Aetheria",
            $"Are you sure you want to convert '{avatarObj.name}' to an Aetheria Avatar?\n\nThis will trigger the conversion pipeline (shaders, physics, expression menus, and scripts).",
            "Start Conversion",
            "Cancel"
        );

        if (confirm)
        {
            // First run environment setup checks (URP & Watari dependencies)
            Setup.CheckDependenciesMenu();

            // Invoke ConvertAvatar.ExecuteConversion (Script to be created next)
            InvokeConvertAvatarScript(avatarObj);
        }
    }

    /// <summary>
    /// Invokes ConvertAvatar.ExecuteConversion via static reference or reflection if class is pending.
    /// </summary>
    private static void InvokeConvertAvatarScript(GameObject avatarObj)
    {
        Type convertAvatarType = Type.GetType("ConvertAvatar") ?? FindTypeInAssemblies("ConvertAvatar");

        if (convertAvatarType != null)
        {
            var method = convertAvatarType.GetMethod("ExecuteConversion", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, new object[] { avatarObj });
                return;
            }
        }

        Debug.LogWarning("[Aetheria SDK] 'ConvertAvatar.cs' with method 'ExecuteConversion(GameObject)' was not found yet. Ready to implement ConvertAvatar.cs!");
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
