using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.PostProcessing;

public class CensorEffectSetup
{
    [MenuItem("Tools/Censor Effect/Auto-Setup Censor Camera", false, 10)]
    private static void AutoSetup()
    {
        // Try to find the main camera, which we assume has the PostProcessLayer component.
        PostProcessLayer ppLayer = Object.FindObjectOfType<PostProcessLayer>();
        if (ppLayer == null)
        {
            EditorUtility.DisplayDialog("Censor Effect Setup", "Could not find a camera with a Post-process Layer component in the scene. Please add one to your main camera first.", "OK");
            return;
        }

        Camera mainCamera = ppLayer.GetComponent<Camera>();
        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog("Censor Effect Setup", "The GameObject with the Post-process Layer does not have a Camera component.", "OK");
            return;
        }

        // Check if a mask camera already exists as a child.
        Transform existingMaskCamera = mainCamera.transform.Find("Censor Mask Camera");
        if (existingMaskCamera != null)
        {
            if (EditorUtility.DisplayDialog("Censor Effect Setup", "A 'Censor Mask Camera' already exists as a child of the main camera. Do you want to remove it and create a new one?", "Yes", "No"))
            {
                Object.DestroyImmediate(existingMaskCamera.gameObject);
            }
            else
            {
                return;
            }
        }

        // Create and configure the new mask camera object.
        GameObject maskCamGo = new GameObject("Censor Mask Camera");
        maskCamGo.transform.SetParent(mainCamera.transform, false);
        maskCamGo.transform.localPosition = Vector3.zero;
        maskCamGo.transform.localRotation = Quaternion.identity;
        maskCamGo.transform.localScale = Vector3.one;

        Camera maskCamera = maskCamGo.AddComponent<Camera>();

        // Configure the camera's properties.
        maskCamera.CopyFrom(mainCamera);
        maskCamera.depth = mainCamera.depth - 1; // Ensure it renders before the main camera.
        maskCamera.clearFlags = CameraClearFlags.SolidColor;
        maskCamera.backgroundColor = Color.black;
        maskCamera.cullingMask = 0; // Start with nothing, user will configure via Post-process Profile.

        // Remove unnecessary components.
        if (maskCamGo.TryGetComponent(out AudioListener listener))
        {
            Object.DestroyImmediate(listener);
        }

        // Add and configure the mask generator.
        maskCamGo.AddComponent<CensorMaskGenerator>();

        EditorUtility.DisplayDialog("Censor Effect Setup", "Successfully created and configured the 'Censor Mask Camera' as a child of your main camera.", "OK");
    }
}
