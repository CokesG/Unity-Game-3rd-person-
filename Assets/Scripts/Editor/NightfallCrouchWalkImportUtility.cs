using System;
using UnityEditor;
using UnityEngine;

public static class NightfallCrouchWalkImportUtility
{
    private const string MenuPath = "Tools/TPS/Nightfall/Reimport Procedural Crouch Walk Candidates";
    private const string UserProceduralFolder = "Assets/Animations/NightfallVanguard/UserCrouchWalkProcedural";
    private const string NightfallProceduralFolder = "Assets/Art/Characters/NightfallVanguard/Exports/ProceduralCrouchWalk";

    [MenuItem(MenuPath)]
    public static void ReimportProceduralCrouchWalkCandidates()
    {
        int configuredCount = 0;
        configuredCount += ConfigureFolder(UserProceduralFolder, "User_Crouch_Walk");
        configuredCount += ConfigureFolder(NightfallProceduralFolder, "Nightfall_FullQuality_CrouchWalk");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Configured {configuredCount} procedural crouch-walk candidate importers.");
    }

    private static int ConfigureFolder(string folder, string clipPrefix)
    {
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { folder });
        int configuredCount = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null || !path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            ConfigureImporter(importer, path, clipPrefix);
            importer.SaveAndReimport();
            configuredCount++;
        }

        return configuredCount;
    }

    private static void ConfigureImporter(ModelImporter importer, string path, string clipPrefix)
    {
        importer.importAnimation = true;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        importer.resampleCurves = true;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.clipAnimations;
        }

        if (clips == null || clips.Length == 0)
        {
            return;
        }

        string direction = ResolveDirection(path);
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].name = $"{clipPrefix}_{direction}_Procedural";
            clips[i].loopTime = true;
            clips[i].loopPose = true;
            clips[i].lockRootHeightY = true;
            clips[i].keepOriginalPositionY = true;
            clips[i].heightFromFeet = false;
            clips[i].lockRootPositionXZ = true;
            clips[i].keepOriginalPositionXZ = true;
            clips[i].lockRootRotation = true;
            clips[i].keepOriginalOrientation = true;
            clips[i].wrapMode = WrapMode.Loop;
        }

        importer.clipAnimations = clips;
    }

    private static string ResolveDirection(string path)
    {
        string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
        if (fileName.IndexOf("_Back_", StringComparison.Ordinal) >= 0) return "Back";
        if (fileName.IndexOf("_Left_", StringComparison.Ordinal) >= 0) return "Left";
        if (fileName.IndexOf("_Right_", StringComparison.Ordinal) >= 0) return "Right";
        return "Forward";
    }
}
