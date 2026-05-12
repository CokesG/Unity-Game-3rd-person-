using UnityEngine;

public static class TpsRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            return;
        }

        if (player.GetComponent<PlayerWeaponController>() == null)
        {
            player.AddComponent<PlayerWeaponController>();
        }

        if (Object.FindAnyObjectByType<TPSReticleHUD>() == null)
        {
            GameObject hud = new GameObject("TPS_ReticleHUD");
            Object.DontDestroyOnLoad(hud);
            hud.AddComponent<TPSReticleHUD>();
        }

        if (Object.FindAnyObjectByType<TargetDummy>() == null)
        {
            CreateRuntimeDummy("Runtime_TargetDummy_Close", new Vector3(0f, 1f, 12f));
            CreateRuntimeDummy("Runtime_TargetDummy_Mid", new Vector3(-4f, 1f, 22f));
            CreateRuntimeDummy("Runtime_TargetDummy_Far", new Vector3(4f, 1f, 34f));
        }
    }

    private static void CreateRuntimeDummy(string name, Vector3 position)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name;
        body.transform.position = position;
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head_Critical";
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        head.transform.localScale = Vector3.one * 0.34f;

        ConfigurePrototypeTargetCollider(body);
        body.AddComponent<TargetDummy>();
    }

    private static void ConfigurePrototypeTargetCollider(GameObject body)
    {
        CapsuleCollider bodyCollider = body.GetComponent<CapsuleCollider>();
        if (bodyCollider == null)
        {
            return;
        }

        bodyCollider.height = 1.35f;
        bodyCollider.center = new Vector3(0f, -0.22f, 0f);
    }
}
