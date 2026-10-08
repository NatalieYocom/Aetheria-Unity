#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Transform))]
public class VRChatAvatarDetector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GameObject selectedObj = ((Transform)target).gameObject;

        // Check if GameObject has a VRChat Avatar Descriptor attached
        Component vrcDescriptor = selectedObj.GetComponent("VRC.SDK3.Avatars.Components.VRCAvatarDescriptor");
        
        if (vrcDescriptor != null && !selectedObj.GetComponent<AetheriaAvatarDescriptor>())
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("VRChat Avatar Descriptor detected! Would you like to convert this model to an Aetheria Avatar?", MessageType.Info);
            
            GUI.backgroundColor = new Color(0.0f, 0.95f, 0.99f); // Cyber-Etheric Cyan
            if (GUILayout.Button("✨ Convert VRChat Avatar to Aetheria", GUILayout.Height(35)))
            {
                VRChatToAetheriaConverter.ConvertAvatar(selectedObj, vrcDescriptor);
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif
