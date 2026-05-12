using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NightfallAnimationRiggingSetupMenu
{
    private const string MenuRoot = "Tools/TPS/Nightfall/";
    private const string RigRootName = "Nightfall_RuntimeRig";

    [MenuItem(MenuRoot + "Setup Animation Rigging Helpers")]
    public static void SetupAnimationRiggingHelpers()
    {
        Type rigBuilderType = FindType("UnityEngine.Animations.Rigging.RigBuilder");
        Type rigType = FindType("UnityEngine.Animations.Rigging.Rig");
        Type rigLayerType = FindType("UnityEngine.Animations.Rigging.RigLayer");
        Type twoBoneIkType = FindType("UnityEngine.Animations.Rigging.TwoBoneIKConstraint");

        if (rigBuilderType == null || rigType == null || rigLayerType == null || twoBoneIkType == null)
        {
            EditorUtility.DisplayDialog(
                "Animation Rigging Not Ready",
                "The com.unity.animation.rigging package is in the manifest, but Unity has not resolved/imported it yet. Let Package Manager finish, then run this menu item again.",
                "OK");
            return;
        }

        GameObject player = FindPlayerObject();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Player Missing", "Select the Player object or open SampleScene so the Player can be found.", "OK");
            return;
        }

        Animator animator = player.GetComponentInChildren<Animator>(true);
        if (animator == null || !animator.isHuman)
        {
            EditorUtility.DisplayDialog("Animator Missing", "The selected Player needs a Humanoid Animator under CharacterVisual.", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Setup Nightfall Animation Rigging Helpers");

        Component rigBuilder = EnsureComponent(animator.gameObject, rigBuilderType);
        GameObject rigRoot = EnsureChild(animator.transform, RigRootName);
        Component rig = EnsureComponent(rigRoot, rigType);
        SetPublicPropertyOrField(rig, "weight", 1f);
        EnsureRigLayer(rigBuilder, rigLayerType, rig);

        GameObject footTargets = EnsureChild(rigRoot.transform, "FootIK_Targets");
        GameObject footConstraints = EnsureChild(rigRoot.transform, "FootIK_Constraints");
        GameObject handTargets = EnsureChild(rigRoot.transform, "WeaponIK_Targets");
        GameObject handConstraints = EnsureChild(rigRoot.transform, "WeaponIK_Constraints");

        Transform leftFootTarget = BuildLegIK(animator, twoBoneIkType, footTargets.transform, footConstraints.transform, true);
        Transform rightFootTarget = BuildLegIK(animator, twoBoneIkType, footTargets.transform, footConstraints.transform, false);
        Transform leftHandTarget = BuildArmIK(animator, twoBoneIkType, handTargets.transform, handConstraints.transform, true);
        Transform rightHandTarget = BuildArmIK(animator, twoBoneIkType, handTargets.transform, handConstraints.transform, false);

        NightfallFootIKTargets footDriver = rigRoot.GetComponent<NightfallFootIKTargets>();
        if (footDriver == null)
        {
            footDriver = Undo.AddComponent<NightfallFootIKTargets>(rigRoot);
        }

        SerializedObject driver = new SerializedObject(footDriver);
        SetObjectReference(driver, "leftFoot", animator.GetBoneTransform(HumanBodyBones.LeftFoot));
        SetObjectReference(driver, "rightFoot", animator.GetBoneTransform(HumanBodyBones.RightFoot));
        SetObjectReference(driver, "leftTarget", leftFootTarget);
        SetObjectReference(driver, "rightTarget", rightFootTarget);
        SetObjectReference(driver, "leftHint", FindDeepChild(footTargets.transform, "LeftFoot_Hint"));
        SetObjectReference(driver, "rightHint", FindDeepChild(footTargets.transform, "RightFoot_Hint"));
        driver.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = rigRoot;
        EditorUtility.SetDirty(animator.gameObject);
        EditorUtility.SetDirty(rigRoot);
        EditorSceneManager.MarkSceneDirty(animator.gameObject.scene);
        Debug.Log($"Nightfall Animation Rigging helpers ready. Foot targets: {leftFootTarget != null && rightFootTarget != null}. Hand targets: {leftHandTarget != null && rightHandTarget != null}.");
    }

    private static Transform BuildLegIK(Animator animator, Type twoBoneIkType, Transform targetsRoot, Transform constraintsRoot, bool left)
    {
        HumanBodyBones upperBone = left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg;
        HumanBodyBones lowerBone = left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg;
        HumanBodyBones footBone = left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot;
        string side = left ? "Left" : "Right";

        Transform upper = animator.GetBoneTransform(upperBone);
        Transform lower = animator.GetBoneTransform(lowerBone);
        Transform foot = animator.GetBoneTransform(footBone);
        if (upper == null || lower == null || foot == null)
        {
            Debug.LogWarning($"Skipping {side} foot IK because one or more leg bones are missing.");
            return null;
        }

        Transform target = EnsureChild(targetsRoot, $"{side}Foot_Target").transform;
        Transform hint = EnsureChild(targetsRoot, $"{side}Foot_Hint").transform;
        ResetTransform(target, foot.position, foot.rotation);
        ResetTransform(hint, lower.position + animator.transform.forward * 0.35f + Vector3.up * 0.15f, Quaternion.identity);

        GameObject constraintObject = EnsureChild(constraintsRoot, $"{side}Foot_TwoBoneIK");
        Component constraint = EnsureComponent(constraintObject, twoBoneIkType);
        ConfigureTwoBoneIK(constraint, upper, lower, foot, target, hint, 0.85f, 1f, 0.35f, 1f);
        return target;
    }

    private static Transform BuildArmIK(Animator animator, Type twoBoneIkType, Transform targetsRoot, Transform constraintsRoot, bool left)
    {
        HumanBodyBones upperBone = left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm;
        HumanBodyBones lowerBone = left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm;
        HumanBodyBones handBone = left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand;
        string side = left ? "Left" : "Right";

        Transform upper = animator.GetBoneTransform(upperBone);
        Transform lower = animator.GetBoneTransform(lowerBone);
        Transform hand = animator.GetBoneTransform(handBone);
        if (upper == null || lower == null || hand == null)
        {
            Debug.LogWarning($"Skipping {side} hand IK because one or more arm bones are missing.");
            return null;
        }

        Transform target = EnsureChild(targetsRoot, $"{side}Hand_Target").transform;
        Transform hint = EnsureChild(targetsRoot, $"{side}Hand_Hint").transform;
        ResetTransform(target, hand.position, hand.rotation);
        ResetTransform(hint, lower.position + animator.transform.right * (left ? -0.35f : 0.35f), Quaternion.identity);

        GameObject constraintObject = EnsureChild(constraintsRoot, $"{side}Hand_TwoBoneIK");
        Component constraint = EnsureComponent(constraintObject, twoBoneIkType);
        ConfigureTwoBoneIK(constraint, upper, lower, hand, target, hint, 0f, 1f, 1f, 1f);
        return target;
    }

    private static void ConfigureTwoBoneIK(Component constraint, Transform root, Transform mid, Transform tip, Transform target, Transform hint, float weight, float targetPositionWeight, float targetRotationWeight, float hintWeight)
    {
        SetPublicPropertyOrField(constraint, "weight", weight);

        SerializedObject serializedConstraint = new SerializedObject(constraint);
        SerializedProperty data = serializedConstraint.FindProperty("m_Data");
        if (data == null)
        {
            Debug.LogWarning($"Could not configure {constraint.name}; no serialized m_Data property found.");
            return;
        }

        SetObjectReference(data, "m_Root", root);
        SetObjectReference(data, "m_Mid", mid);
        SetObjectReference(data, "m_Tip", tip);
        SetObjectReference(data, "m_Target", target);
        SetObjectReference(data, "m_Hint", hint);
        SetBool(data, "m_MaintainTargetPositionOffset", false);
        SetBool(data, "m_MaintainTargetRotationOffset", false);
        SetFloat(data, "m_TargetPositionWeight", targetPositionWeight);
        SetFloat(data, "m_TargetRotationWeight", targetRotationWeight);
        SetFloat(data, "m_HintWeight", hintWeight);
        serializedConstraint.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(constraint);
    }

    private static void EnsureRigLayer(Component rigBuilder, Type rigLayerType, Component rig)
    {
        PropertyInfo layersProperty = rigBuilder.GetType().GetProperty("layers", BindingFlags.Instance | BindingFlags.Public);
        if (layersProperty == null)
        {
            Debug.LogWarning("RigBuilder exists, but its layers property was not found.");
            return;
        }

        IList layers = layersProperty.GetValue(rigBuilder, null) as IList;
        if (layers == null)
        {
            return;
        }

        foreach (object layer in layers)
        {
            object existingRig = GetPublicPropertyOrField(layer, "rig");
            if (ReferenceEquals(existingRig, rig))
            {
                SetPublicPropertyOrField(layer, "active", true);
                EditorUtility.SetDirty(rigBuilder);
                return;
            }
        }

        object rigLayer = Activator.CreateInstance(rigLayerType, rig, true);
        layers.Add(rigLayer);
        layersProperty.SetValue(rigBuilder, layers, null);
        EditorUtility.SetDirty(rigBuilder);
    }

    private static GameObject FindPlayerObject()
    {
        if (Selection.activeGameObject != null)
        {
            Transform selected = Selection.activeGameObject.transform;
            while (selected != null)
            {
                if (selected.CompareTag("Player") || selected.name == "Player")
                {
                    return selected.gameObject;
                }

                selected = selected.parent;
            }
        }

        GameObject taggedPlayer = GameObject.FindWithTag("Player");
        if (taggedPlayer != null)
        {
            return taggedPlayer;
        }

        return GameObject.Find("Player");
    }

    private static Type FindType(string fullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName))
            .FirstOrDefault(type => type != null);
    }

    private static Component EnsureComponent(GameObject gameObject, Type componentType)
    {
        Component component = gameObject.GetComponent(componentType);
        return component != null ? component : Undo.AddComponent(gameObject, componentType);
    }

    private static GameObject EnsureChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child != null)
        {
            return child.gameObject;
        }

        GameObject gameObject = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(gameObject, $"Create {childName}");
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void ResetTransform(Transform transform, Vector3 position, Quaternion rotation)
    {
        Undo.RecordObject(transform, $"Move {transform.name}");
        transform.position = position;
        transform.rotation = rotation;
    }

    private static Transform FindDeepChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetObjectReference(SerializedProperty parent, string relativePropertyName, UnityEngine.Object value)
    {
        SerializedProperty property = parent.FindPropertyRelative(relativePropertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetBool(SerializedProperty parent, string relativePropertyName, bool value)
    {
        SerializedProperty property = parent.FindPropertyRelative(relativePropertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }

    private static void SetFloat(SerializedProperty parent, string relativePropertyName, float value)
    {
        SerializedProperty property = parent.FindPropertyRelative(relativePropertyName);
        if (property != null)
        {
            property.floatValue = value;
        }
    }

    private static object GetPublicPropertyOrField(object target, string name)
    {
        if (target == null)
        {
            return null;
        }

        Type type = target.GetType();
        PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property != null)
        {
            return property.GetValue(target, null);
        }

        FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        return field != null ? field.GetValue(target) : null;
    }

    private static void SetPublicPropertyOrField(object target, string name, object value)
    {
        if (target == null)
        {
            return;
        }

        Type type = target.GetType();
        PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
        if (property != null && property.CanWrite)
        {
            property.SetValue(target, value, null);
            return;
        }

        FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
