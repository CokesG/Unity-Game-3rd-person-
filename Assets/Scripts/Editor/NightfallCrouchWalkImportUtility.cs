using System;
using UnityEditor;
using UnityEngine;

public static class NightfallCrouchWalkImportUtility
{
    private const string MenuPath = "Tools/TPS/Nightfall/Reimport Crouch Walk Candidates";
    private const string LegacyMenuPath = "Tools/TPS/Nightfall/Reimport Procedural Crouch Walk Candidates";
    private const string UserAuthoredFolder = "Assets/Animations/NightfallVanguard/UserCrouchWalk";
    private const string UserProceduralFolder = "Assets/Animations/NightfallVanguard/UserCrouchWalkProcedural";
    private const string NightfallProceduralFolder = "Assets/Art/Characters/NightfallVanguard/Exports/ProceduralCrouchWalk";

    [MenuItem(MenuPath)]
    public static void ReimportProceduralCrouchWalkCandidates()
    {
        int configuredCount = 0;
        configuredCount += ConfigureFolder(UserAuthoredFolder, "User_Crouch_Walk", string.Empty);
        configuredCount += ConfigureFolder(UserProceduralFolder, "User_Crouch_Walk", "_Procedural");
        configuredCount += ConfigureFolder(NightfallProceduralFolder, "Nightfall_FullQuality_CrouchWalk", "_Procedural");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Configured {configuredCount} crouch-walk candidate importers.");
    }

    [MenuItem(LegacyMenuPath)]
    public static void ReimportProceduralCrouchWalkCandidatesLegacy()
    {
        ReimportProceduralCrouchWalkCandidates();
    }

    private static int ConfigureFolder(string folder, string clipPrefix, string clipSuffix)
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

            ConfigureImporter(importer, path, clipPrefix, clipSuffix);
            importer.SaveAndReimport();
            configuredCount++;
        }

        return configuredCount;
    }

    private static void ConfigureImporter(ModelImporter importer, string path, string clipPrefix, string clipSuffix)
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
            clips[i].name = $"{clipPrefix}_{direction}{clipSuffix}";
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
        if (HasDirectionToken(fileName, "Back")) return "Back";
        if (HasDirectionToken(fileName, "Left")) return "Left";
        if (HasDirectionToken(fileName, "Right")) return "Right";
        return "Forward";
    }

    private static bool HasDirectionToken(string fileName, string direction)
    {
        return fileName.EndsWith("_" + direction, StringComparison.Ordinal)
            || fileName.IndexOf("_" + direction + "_", StringComparison.Ordinal) >= 0;
    }
}
