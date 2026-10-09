using UnityEngine;
using UnityEngine.Rendering;

public class CaveLightingController : MonoBehaviour
{
    [Header("Directional Light (Sun)")]
    public Light sunLight;
    public float outdoorSunIntensity = 0.65f;
    public Color outdoorSunColor = new Color(1.0f, 0.75f, 0.45f);

    [Header("Cave Lighting Settings")]
    public float caveSunIntensity = 0.0f; // Completely turn off sun inside cave
    public Color caveSunColor = new Color(0.05f, 0.08f, 0.15f);
    public Color caveAmbientColor = new Color(0.008f, 0.012f, 0.025f); // Pitch dark ambient

    [Header("Camera Background")]
    public Color caveCameraBackgroundColor = new Color(0.005f, 0.01f, 0.02f); // Dark blue-black background

    [Header("Fog Settings")]
    public bool enableCaveFog = true;
    public Color caveFogColor = new Color(0.01f, 0.03f, 0.06f);
    public float caveFogDensity = 0.04f;

    // Saved outdoor state
    private float savedSunIntensity;
    private Color savedSunColor;
    private Color savedAmbientColor;
    private AmbientMode savedAmbientMode;
    private bool savedFogEnabled;
    private Color savedFogColor;
    private float savedFogDensity;
    private FogMode savedFogMode;

    private Camera mainCam;
    private CameraClearFlags savedClearFlags = CameraClearFlags.Skybox;
    private Color savedCamBgColor = Color.black;
    private bool isInsideCave = false;

    private void Awake()
    {
        FindSunLightIfNeeded();
        FindCameraIfNeeded();
        SaveOutdoorSettings();
    }

    private void FindSunLightIfNeeded()
    {
        if (sunLight == null)
        {
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    sunLight = l;
                    break;
                }
            }
        }
    }

    private void FindCameraIfNeeded()
    {
        if (mainCam == null)
        {
            mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = Object.FindAnyObjectByType<Camera>();
            }
        }
    }

    public void SaveOutdoorSettings()
    {
        FindSunLightIfNeeded();
        FindCameraIfNeeded();

        if (sunLight != null)
        {
            savedSunIntensity = sunLight.intensity > 0.05f ? sunLight.intensity : outdoorSunIntensity;
            savedSunColor = sunLight.color;
        }
        else
        {
            savedSunIntensity = outdoorSunIntensity;
            savedSunColor = outdoorSunColor;
        }

        savedAmbientColor = RenderSettings.ambientLight;
        savedAmbientMode = RenderSettings.ambientMode;
        savedFogEnabled = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogDensity = RenderSettings.fogDensity;
        savedFogMode = RenderSettings.fogMode;

        if (mainCam != null)
        {
            savedClearFlags = mainCam.clearFlags;
            savedCamBgColor = mainCam.backgroundColor;
        }
    }

    public void ApplyCaveLighting()
    {
        isInsideCave = true;
        FindSunLightIfNeeded();
        FindCameraIfNeeded();

        // 1. Turn off direct sunlight completely
        if (sunLight != null)
        {
            sunLight.intensity = caveSunIntensity;
            sunLight.color = caveSunColor;
        }

        // 2. Set dark flat ambient light
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = caveAmbientColor;

        // 3. Set camera background to dark solid color (hides skybox bleed)
        if (mainCam != null)
        {
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = caveCameraBackgroundColor;
        }

        // 4. Cool blue cave fog
        if (enableCaveFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = caveFogColor;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = caveFogDensity;
        }

        Debug.Log("[CaveLightingController] Applied DARK CAVE Lighting (Sun OFF, Dark Ambient, Solid Dark Camera Background)");
    }

    public void RestoreOutdoorLighting()
    {
        isInsideCave = false;
        FindSunLightIfNeeded();
        FindCameraIfNeeded();

        // 1. Restore sunlight
        if (sunLight != null)
        {
            sunLight.intensity = savedSunIntensity;
            sunLight.color = savedSunColor;
        }

        // 2. Restore ambient light & mode
        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientLight = savedAmbientColor;

        // 3. Restore camera background & skybox
        if (mainCam != null)
        {
            mainCam.clearFlags = savedClearFlags;
            mainCam.backgroundColor = savedCamBgColor;
        }

        // 4. Restore outdoor fog
        RenderSettings.fog = savedFogEnabled;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogDensity = savedFogDensity;
        RenderSettings.fogMode = savedFogMode;

        Debug.Log("[CaveLightingController] Restored OUTDOOR Lighting & Skybox");
    }

    public bool IsInsideCave => isInsideCave;
}
