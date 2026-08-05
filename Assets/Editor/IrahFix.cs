using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public static class IrahFix
{
    [InitializeOnLoadMethod]
    [MenuItem("Luminang/Apply Irah Fix")]
    static void RunFix()
    {
        if (EditorPrefs.GetBool("IrahCoolerSetup_FixV2", false)) return;
        EditorPrefs.SetBool("IrahCoolerSetup_FixV2", true);

        // 1. Fix Animation Import Settings (Bake Root Transform Y so she doesn't sink)
        string animPath = "Assets/Animations/NPC_Animations/Mixamo/Carrying.fbx";
        ModelImporter importer = AssetImporter.GetAtPath(animPath) as ModelImporter;
        if (importer != null)
        {
            ModelImporterClipAnimation[] clipAnimations = importer.clipAnimations;
            if (clipAnimations != null && clipAnimations.Length > 0)
            {
                for (int i = 0; i < clipAnimations.Length; i++)
                {
                    clipAnimations[i].lockRootHeightY = true;
                    clipAnimations[i].keepOriginalPositionY = true;
                }
                importer.clipAnimations = clipAnimations;
                importer.SaveAndReimport();
                Debug.Log("[IrahFix] Successfully baked Root Transform Y for Carrying animation to prevent sinking.");
            }
        }

        // 2. Fix Animator Controller
        string controllerPath = "Assets/Animations/NPC_Animations/Controllers/Irah_Controller.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller != null)
        {
            controller.AddParameter("StartCarrying", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState idleState = null;
            AnimatorState carryingState = null;

            foreach (var state in rootStateMachine.states)
            {
                if (state.state.name == "Breathing Idle") idleState = state.state;
                if (state.state.name == "Carrying") carryingState = state.state;
            }

            if (idleState != null && carryingState != null)
            {
                // Set Breathing Idle as default
                rootStateMachine.defaultState = idleState;

                // Add transition from Idle to Carrying
                AnimatorStateTransition startTransition = idleState.AddTransition(carryingState);
                startTransition.AddCondition(AnimatorConditionMode.If, 0, "StartCarrying");
                startTransition.hasExitTime = false;
                startTransition.duration = 0.25f;

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();
                Debug.Log("[IrahFix] Successfully updated Irah_Controller flow.");
            }
        }

        // 3. Delete the old setup script
        AssetDatabase.DeleteAsset("Assets/Editor/IrahSetupGenerator.cs");
        Debug.Log("[IrahFix] Setup completed and old generator deleted.");
    }
}
