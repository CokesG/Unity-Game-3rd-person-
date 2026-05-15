using UnityEngine;

public enum ReticleStyle
{
    Crosshair = 0,
    Dot = 1,
    Circle = 2,
    CircleDot = 3,
}

public static class TpsPlayerSettings
{
    private const string MouseDpiKey = "TPS.MouseDpi";
    private const string Cm360Key = "TPS.Cm360";
    private const string LookSensitivityKey = "TPS.LookSensitivity";
    private const string VerticalRatioKey = "TPS.VerticalRatio";
    private const string AdsMultiplierKey = "TPS.AdsMultiplier";
    private const string ReticleStyleKey = "TPS.ReticleStyle";
    private const string ReticleColorKey = "TPS.ReticleColor";
    private const string ReticleSizeKey = "TPS.ReticleSize";
    private const string ReticleGapKey = "TPS.ReticleGap";
    private const string ReticleThicknessKey = "TPS.ReticleThickness";
    private const string ReticleOutlineKey = "TPS.ReticleOutline";

    public static bool SettingsOpen { get; set; }
    public static float MouseDpi { get; private set; } = 800f;
    public static float CmPer360 { get; private set; } = 35f;
    public static float LookSensitivity { get; private set; } = 1.5f;
    public static float VerticalRatio { get; private set; } = 1f;
    public static float AdsMultiplier { get; private set; } = 1f;
    public static ReticleStyle ReticleMode { get; private set; } = ReticleStyle.Crosshair;
    public static Color ReticleColor { get; private set; } = new Color(0.15f, 1f, 0.55f, 0.95f);
    public static float ReticleSize { get; private set; } = 6f;
    public static float ReticleGap { get; private set; } = 5f;
    public static float ReticleThickness { get; private set; } = 1.5f;
    public static bool ReticleOutline { get; private set; } = true;

    public static float HipDegreesPerPixel => LookSensitivity * 0.022f;
    public static float AimDegreesPerPixel => HipDegreesPerPixel * AdsMultiplier;

    static TpsPlayerSettings()
    {
        Load();
    }

    public static void Load()
    {
        MouseDpi = Mathf.Clamp(PlayerPrefs.GetFloat(MouseDpiKey, MouseDpi), 200f, 6400f);
        CmPer360 = Mathf.Clamp(PlayerPrefs.GetFloat(Cm360Key, CmPer360), 5f, 120f);
        LookSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(LookSensitivityKey, ConvertCm360ToSensitivity(CmPer360, MouseDpi)), 0.1f, 10f);
        VerticalRatio = Mathf.Clamp(PlayerPrefs.GetFloat(VerticalRatioKey, VerticalRatio), 0.1f, 2f);
        AdsMultiplier = Mathf.Clamp(PlayerPrefs.GetFloat(AdsMultiplierKey, AdsMultiplier), 0.1f, 2f);
        ReticleMode = (ReticleStyle)Mathf.Clamp(PlayerPrefs.GetInt(ReticleStyleKey, (int)ReticleMode), 0, 3);
        ReticleColor = HtmlToColor(PlayerPrefs.GetString(ReticleColorKey, ColorUtility.ToHtmlStringRGB(ReticleColor)), ReticleColor);
        ReticleSize = Mathf.Clamp(PlayerPrefs.GetFloat(ReticleSizeKey, ReticleSize), 2f, 30f);
        ReticleGap = Mathf.Clamp(PlayerPrefs.GetFloat(ReticleGapKey, ReticleGap), 0f, 30f);
        ReticleThickness = Mathf.Clamp(PlayerPrefs.GetFloat(ReticleThicknessKey, ReticleThickness), 1f, 6f);
        ReticleOutline = PlayerPrefs.GetInt(ReticleOutlineKey, ReticleOutline ? 1 : 0) != 0;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MouseDpiKey, MouseDpi);
        PlayerPrefs.SetFloat(Cm360Key, CmPer360);
        PlayerPrefs.SetFloat(LookSensitivityKey, LookSensitivity);
        PlayerPrefs.SetFloat(VerticalRatioKey, VerticalRatio);
        PlayerPrefs.SetFloat(AdsMultiplierKey, AdsMultiplier);
        PlayerPrefs.SetInt(ReticleStyleKey, (int)ReticleMode);
        PlayerPrefs.SetString(ReticleColorKey, ColorUtility.ToHtmlStringRGB(ReticleColor));
        PlayerPrefs.SetFloat(ReticleSizeKey, ReticleSize);
        PlayerPrefs.SetFloat(ReticleGapKey, ReticleGap);
        PlayerPrefs.SetFloat(ReticleThicknessKey, ReticleThickness);
        PlayerPrefs.SetInt(ReticleOutlineKey, ReticleOutline ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ResetDefaults()
    {
        MouseDpi = 800f;
        CmPer360 = 35f;
        LookSensitivity = 1.5f;
        VerticalRatio = 1f;
        AdsMultiplier = 1f;
        ReticleMode = ReticleStyle.Crosshair;
        ReticleColor = new Color(0.15f, 1f, 0.55f, 0.95f);
        ReticleSize = 6f;
        ReticleGap = 5f;
        ReticleThickness = 1.5f;
        ReticleOutline = true;
        Save();
    }

    public static void SetMouseDpi(float value)
    {
        MouseDpi = Mathf.Clamp(value, 200f, 6400f);
        Save();
    }

    public static void SetCmPer360(float value)
    {
        CmPer360 = Mathf.Clamp(value, 5f, 120f);
        Save();
    }

    public static void SetLookSensitivity(float value)
    {
        LookSensitivity = Mathf.Clamp(value, 0.1f, 10f);
        Save();
    }

    public static void SetVerticalRatio(float value)
    {
        VerticalRatio = Mathf.Clamp(value, 0.1f, 2f);
        Save();
    }

    public static void SetAdsMultiplier(float value)
    {
        AdsMultiplier = Mathf.Clamp(value, 0.1f, 2f);
        Save();
    }

    public static void SetReticleStyle(ReticleStyle value)
    {
        ReticleMode = value;
        Save();
    }

    public static void SetReticleColor(Color value)
    {
        ReticleColor = new Color(value.r, value.g, value.b, 0.95f);
        Save();
    }

    public static void SetReticleSize(float value)
    {
        ReticleSize = Mathf.Clamp(value, 2f, 30f);
        Save();
    }

    public static void SetReticleGap(float value)
    {
        ReticleGap = Mathf.Clamp(value, 0f, 30f);
        Save();
    }

    public static void SetReticleThickness(float value)
    {
        ReticleThickness = Mathf.Clamp(value, 1f, 6f);
        Save();
    }

    public static void SetReticleOutline(bool value)
    {
        ReticleOutline = value;
        Save();
    }

    private static Color HtmlToColor(string html, Color fallback)
    {
        if (!html.StartsWith("#"))
        {
            html = "#" + html;
        }

        return ColorUtility.TryParseHtmlString(html, out Color color) ? new Color(color.r, color.g, color.b, 0.95f) : fallback;
    }

    private static float ConvertCm360ToSensitivity(float cm360, float dpi)
    {
        float degreesPerPixel = 360f / Mathf.Max(1f, dpi * (cm360 / 2.54f));
        return Mathf.Clamp(degreesPerPixel / 0.022f, 0.1f, 10f);
    }
}
