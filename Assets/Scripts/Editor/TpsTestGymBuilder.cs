using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class TpsTestGymBuilder
{
    private const string ScenePath = "Assets/Scenes/TPS_TestGym.unity";
    private const string InputPath = "Assets/Settings/PlayerInputActions.inputactions";
    private const string DataFolder = "Assets/Combat";
    private const string WeaponPath = "Assets/Combat/PrototypeRifle.asset";
    private const string MaterialFolder = "Assets/Combat/Materials";

    [MenuItem("Tools/TPS/Create Test Gym Scene")]
    public static void CreateTestGymScene()
    {
        EnsureFolders();
        WeaponDefinition rifle = GetOrCreatePrototypeRifle();
        InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject player = CreatePlayer(inputActions, rifle);
        ThirdPersonCameraController cameraController = CreateCamera(player);
        PlayerWeaponController weaponController = player.GetComponent<PlayerWeaponController>();
        AssignObject(weaponController, "cameraController", cameraController);

        CreateLighting();
        CreateGrayboxArena();
        CreateTargets();
        CreateHud(cameraController, weaponController, player.GetComponent<ThirdPersonMotor>());

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("TPS Test Gym", $"Created {ScenePath}. Open it, press Play, and use WASD/Shift/Ctrl/Right Click/Left Click/R/V.", "Good");
    }

    [MenuItem("Tools/TPS/Build Test Gym In Current Scene")]
    public static void BuildTestGymInCurrentScene()
    {
        EnsureFolders();
        CreateGrayboxArena();
        CreateTargets();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        ThirdPersonCameraController cameraController = Object.FindAnyObjectByType<ThirdPersonCameraController>();
        if (player != null)
        {
            WeaponDefinition rifle = GetOrCreatePrototypeRifle();
            PlayerWeaponController weaponController = player.GetComponent<PlayerWeaponController>();
            if (weaponController == null)
            {
                weaponController = player.AddComponent<PlayerWeaponController>();
            }

            Transform muzzle = player.transform.Find("WeaponMuzzle");
            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject("WeaponMuzzle");
                muzzleObject.transform.SetParent(player.transform);
                muzzleObject.transform.localPosition = new Vector3(0.36f, 1.25f, 0.72f);
                muzzle = muzzleObject.transform;
            }

            AssignObject(weaponController, "weapon", rifle);
            AssignObject(weaponController, "muzzle", muzzle);
            AssignObject(weaponController, "cameraController", cameraController);

            if (Object.FindAnyObjectByType<TPSReticleHUD>() == null)
            {
                CreateHud(cameraController, weaponController, player.GetComponent<ThirdPersonMotor>());
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreatePlayer(InputActionAsset inputActions, WeaponDefinition rifle)
    {
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        SetLayerRecursively(player, LayerMask.NameToLayer("Player"));
        player.transform.position = new Vector3(0f, 1f, 0f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = Vector3.zero;
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;

        GameObject cameraTarget = new GameObject("CameraTarget");
        cameraTarget.transform.SetParent(player.transform);
        cameraTarget.transform.localPosition = new Vector3(0f, 0.65f, 0f);

        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0f, -0.95f, 0f);

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "CharacterVisual_Proxy";
        visual.transform.SetParent(player.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("MAT_PlayerProxy", new Color(0.15f, 0.45f, 0.95f));
        SetLayerRecursively(visual, LayerMask.NameToLayer("Player"));

        GameObject muzzle = new GameObject("WeaponMuzzle");
        muzzle.transform.SetParent(player.transform);
        muzzle.transform.localPosition = new Vector3(0.36f, 1.25f, 0.72f);

        PlayerInputHandler input = player.AddComponent<PlayerInputHandler>();
        AssignObject(input, "playerControls", inputActions);

        ThirdPersonMotor motor = player.AddComponent<ThirdPersonMotor>();
        AssignObject(motor, "cameraTarget", cameraTarget.transform);

        player.AddComponent<PlayerAnimationController>();
        PlayerWeaponController weaponController = player.AddComponent<PlayerWeaponController>();
        AssignObject(weaponController, "weapon", rifle);
        AssignObject(weaponController, "muzzle", muzzle.transform);

        return player;
    }

    private static ThirdPersonCameraController CreateCamera(GameObject player)
    {
        GameObject cameraObject = new GameObject("ThirdPersonCamera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.45f, 2.1f, -3.75f);
        cameraObject.transform.rotation = Quaternion.Euler(5f, 0f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 85f;
        camera.nearClipPlane = 0.08f;
        cameraObject.AddComponent<AudioListener>();

        ThirdPersonCameraController controller = cameraObject.AddComponent<ThirdPersonCameraController>();
        AssignObject(controller, "target", player.transform.Find("CameraTarget"));
        AssignObject(controller, "controlledCamera", camera);
        int playerLayer = LayerMask.NameToLayer("Player");
        int nonPlayerMask = playerLayer >= 0 ? ~(1 << playerLayer) : ~0;
        AssignLayerMask(controller, "collisionLayers", nonPlayerMask);
        AssignLayerMask(controller, "aimRayLayers", nonPlayerMask);
        return controller;
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private static void CreateGrayboxArena()
    {
        Material floor = GetOrCreateMaterial("MAT_GymFloor", new Color(0.34f, 0.36f, 0.38f));
        Material wall = GetOrCreateMaterial("MAT_GymWall", new Color(0.55f, 0.58f, 0.6f));
        Material cover = GetOrCreateMaterial("MAT_GymCover", new Color(0.2f, 0.24f, 0.28f));

        CreateBox("TPS_Gym_Ground", new Vector3(0f, -0.05f, 25f), new Vector3(34f, 0.1f, 70f), floor);
        CreateBox("Sprint_Lane_Left_Wall", new Vector3(-4.2f, 1f, 18f), new Vector3(0.25f, 2f, 24f), wall);
        CreateBox("Sprint_Lane_Right_Wall", new Vector3(4.2f, 1f, 18f), new Vector3(0.25f, 2f, 24f), wall);

        GameObject rampUp = CreateBox("Slide_Ramp_Down_Test", new Vector3(-9f, 0.55f, 20f), new Vector3(5f, 0.25f, 10f), floor);
        rampUp.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

        GameObject rampDown = CreateBox("Slide_Ramp_Up_Test", new Vector3(9f, 0.55f, 20f), new Vector3(5f, 0.25f, 10f), floor);
        rampDown.transform.rotation = Quaternion.Euler(-12f, 0f, 0f);

        CreateBox("Low_Ceiling_Crouch_Test", new Vector3(-8f, 1.75f, 6f), new Vector3(5f, 0.25f, 5f), cover);
        CreateBox("Low_Ceiling_Left_Post", new Vector3(-10.35f, 0.85f, 6f), new Vector3(0.25f, 1.7f, 5f), cover);
        CreateBox("Low_Ceiling_Right_Post", new Vector3(-5.65f, 0.85f, 6f), new Vector3(0.25f, 1.7f, 5f), cover);

        CreateBox("Waist_Cover_Aim_Blocker", new Vector3(0f, 0.6f, 9f), new Vector3(3f, 1.2f, 0.45f), cover);
        CreateBox("Corner_Cover_Left", new Vector3(7f, 1f, 9f), new Vector3(0.45f, 2f, 5f), cover);
        CreateBox("Corner_Cover_Back", new Vector3(9.5f, 1f, 11.25f), new Vector3(5f, 2f, 0.45f), cover);
        CreateBox("Mantle_Placeholder_Ledge", new Vector3(0f, 0.75f, 34f), new Vector3(5f, 1.5f, 1f), wall);
    }

    private static void CreateTargets()
    {
        CreateTarget("TargetDummy_Close_8m", new Vector3(0f, 1f, 13f));
        CreateTarget("TargetDummy_Mid_22m", new Vector3(-6f, 1f, 26f));
        CreateTarget("TargetDummy_Far_42m", new Vector3(6f, 1f, 46f));
        CreateTarget("TargetDummy_Corner", new Vector3(10f, 1f, 15f));
    }

    private static void CreateTarget(string name, Vector3 position)
    {
        if (GameObject.Find(name) != null)
        {
            return;
        }

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name;
        body.transform.position = position;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        body.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("MAT_Target", new Color(0.1f, 0.75f, 0.95f));

        TargetDummy dummy = body.AddComponent<TargetDummy>();
        AssignObject(dummy, "visualRenderer", body.GetComponent<Renderer>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head_Critical";
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        head.transform.localScale = Vector3.one * 0.34f;
        head.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial("MAT_TargetHead", new Color(1f, 0.85f, 0.2f));
    }

    private static void CreateHud(ThirdPersonCameraController cameraController, PlayerWeaponController weaponController, ThirdPersonMotor motor)
    {
        GameObject hud = new GameObject("TPS_ReticleHUD");
        TPSReticleHUD reticle = hud.AddComponent<TPSReticleHUD>();
        AssignObject(reticle, "cameraController", cameraController);
        AssignObject(reticle, "weaponController", weaponController);
        AssignObject(reticle, "motor", motor);
    }

    private static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private static WeaponDefinition GetOrCreatePrototypeRifle()
    {
        WeaponDefinition rifle = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(WeaponPath);
        if (rifle != null)
        {
            return rifle;
        }

        rifle = ScriptableObject.CreateInstance<WeaponDefinition>();
        rifle.weaponId = "prototype_rifle";
        rifle.displayName = "Prototype Rifle";
        rifle.fireMode = WeaponFireMode.Automatic;
        rifle.shotModel = WeaponShotModel.Hitscan;
        rifle.bodyDamage = 24f;
        rifle.headshotMultiplier = 1.8f;
        rifle.maxRange = 180f;
        rifle.fireRate = 540f;
        rifle.magazineSize = 30;
        rifle.reloadTime = 1.9f;
        rifle.emptyReloadTime = 2.25f;
        rifle.hipSpreadDegrees = 2.4f;
        rifle.adsSpreadDegrees = 0.18f;
        rifle.movingSpreadAddDegrees = 0.65f;
        rifle.airborneSpreadAddDegrees = 1.1f;
        rifle.slideSpreadAddDegrees = 1.4f;
        rifle.cameraRecoilPitch = 0.42f;
        rifle.cameraRecoilYaw = 0.18f;

        AssetDatabase.CreateAsset(rifle, WeaponPath);
        return rifle;
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        string path = $"{MaterialFolder}/{materialName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        material = new Material(shader);
        material.color = color;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Combat");
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            AssetDatabase.CreateFolder(DataFolder, "Materials");
        }
    }

    private static void AssignObject(Object target, string propertyName, Object value)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignLayerMask(Object target, string propertyName, int mask)
    {
        if (target == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(target);
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            return;
        }

        property.intValue = mask;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerRecursively(GameObject gameObject, int layer)
    {
        if (gameObject == null || layer < 0)
        {
            return;
        }

        gameObject.layer = layer;
        foreach (Transform child in gameObject.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
