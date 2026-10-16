using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaveFadeTeleport : MonoBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetSpawnPoint;
    public float countdownTime = 5.0f;
    public float fadeDuration = 2.0f;
    public bool isEnteringCave = true;

    [Header("UI References")]
    public TMP_Text countdownText;
    public CanvasGroup fadeCanvasGroup;

    [Header("Lighting Controller")]
    public CaveLightingController lightingController;

    private float currentTimer = 0f;
    private bool playerInside = false;
    private bool isTeleporting = false;
    private GameObject playerObj;

    private void Start()
    {
        EnsureUIOverlay();
        EnsureLightingController();
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private void EnsureLightingController()
    {
        if (lightingController == null)
        {
            lightingController = Object.FindAnyObjectByType<CaveLightingController>();
            if (lightingController == null)
            {
                GameObject lightCtrlObj = new GameObject("CaveLightingController");
                lightingController = lightCtrlObj.AddComponent<CaveLightingController>();
            }
        }
    }

    private void EnsureUIOverlay()
    {
        if (fadeCanvasGroup == null)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("CaveTeleportCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            Transform fadeObjTr = canvas.transform.Find("CaveFadeOverlay");
            GameObject fadeObj;
            if (fadeObjTr != null)
            {
                fadeObj = fadeObjTr.gameObject;
            }
            else
            {
                fadeObj = new GameObject("CaveFadeOverlay");
                fadeObj.transform.SetParent(canvas.transform, false);
                RectTransform rect = fadeObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.one;

                Image img = fadeObj.AddComponent<Image>();
                img.color = Color.black;
                img.raycastTarget = false;
            }

            fadeCanvasGroup = fadeObj.GetComponent<CanvasGroup>();
            if (fadeCanvasGroup == null)
            {
                fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
            }
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (countdownText == null)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform textTr = canvas.transform.Find("CaveCountdownText");
                if (textTr != null)
                {
                    countdownText = textTr.GetComponent<TMP_Text>();
                }
            }
        }
    }

    private bool IsPlayer(Collider other, out GameObject targetPlayer)
    {
        targetPlayer = null;
        if (other == null) return false;

        string colName = other.gameObject.name.ToLower();
        string rootName = other.transform.root != null ? other.transform.root.name.ToLower() : "";

        if (colName.Contains("dragonspire") || colName.Contains("mountain") || colName.Contains("meshy") ||
            rootName.Contains("dragonspire") || rootName.Contains("mountain") || rootName.Contains("meshy"))
        {
            return false;
        }

        if (other.CompareTag("Player") || rootName.Contains("thachsanh") || colName.Contains("thachsanh") || rootName.Contains("thachsan") || colName.Contains("thachsan"))
        {
            if (other.transform.root != null && (rootName.Contains("thachsanh") || rootName.Contains("thachsan") || other.transform.root.CompareTag("Player")))
            {
                targetPlayer = other.transform.root.gameObject;
            }
            else
            {
                targetPlayer = other.gameObject;
            }
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTeleporting) return;
        if (!IsPlayer(other, out GameObject targetPlayer)) return;

        playerObj = targetPlayer;
        playerInside = true;
        currentTimer = countdownTime;

        if (countdownText != null)
        {
            countdownText.text = isEnteringCave
                ? $"Vào hang trong {Mathf.CeilToInt(currentTimer)}s..."
                : $"Thoát khỏi hang trong {Mathf.CeilToInt(currentTimer)}s...";
            countdownText.gameObject.SetActive(true);
        }

        Debug.Log($"[CaveFadeTeleport] Player entered {(isEnteringCave ? "Cave Entrance" : "Cave Exit")} trigger");
    }

    private void OnTriggerStay(Collider other)
    {
        if (isTeleporting || !playerInside || playerObj == null) return;
        if (!IsPlayer(other, out _)) return;

        currentTimer -= Time.deltaTime;

        if (countdownText != null)
        {
            int secondsLeft = Mathf.Max(0, Mathf.CeilToInt(currentTimer));
            countdownText.text = isEnteringCave
                ? $"Vào hang trong {secondsLeft}s..."
                : $"Thoát khỏi hang trong {secondsLeft}s...";
        }

        if (currentTimer <= 0f)
        {
            StartCoroutine(ExecuteTeleportSequence());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isTeleporting) return;
        if (!IsPlayer(other, out _)) return;

        playerInside = false;
        playerObj = null;
        currentTimer = countdownTime;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        Debug.Log("[CaveFadeTeleport] Player exited trigger - Countdown cancelled & reset");
    }

    private IEnumerator ExecuteTeleportSequence()
    {
        isTeleporting = true;
        playerInside = false;

        PlayerMovement pMovement = playerObj != null ? playerObj.GetComponent<PlayerMovement>() : null;
        if (pMovement != null) pMovement.enabled = false;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        // 1. Fade out to black (2 seconds)
        EnsureUIOverlay();
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                fadeCanvasGroup.blocksRaycasts = true;
            }
            yield return null;
        }
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1.0f;
            fadeCanvasGroup.blocksRaycasts = true;
        }

        // 2. Perform Teleport while fully black
        if (playerObj != null && targetSpawnPoint != null)
        {
            CharacterController controller = playerObj.GetComponent<CharacterController>();
            Rigidbody rb = playerObj.GetComponent<Rigidbody>();

            if (controller != null) controller.enabled = false;

            playerObj.transform.position = targetSpawnPoint.position;
            playerObj.transform.rotation = targetSpawnPoint.rotation;
            Physics.SyncTransforms();

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }

            if (controller != null) controller.enabled = true;

            // Update lighting mode
            EnsureLightingController();
            if (lightingController != null)
            {
                if (isEnteringCave)
                    lightingController.ApplyCaveLighting();
                else
                    lightingController.RestoreOutdoorLighting();
            }

            // Snap main camera if CameraFollow is present
            CameraFollow camFollow = Object.FindAnyObjectByType<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.target = playerObj.transform;
                camFollow.transform.position = playerObj.transform.position - (playerObj.transform.forward * camFollow.distance) + (Vector3.up * camFollow.heightOffset);
                camFollow.transform.LookAt(playerObj.transform.position + Vector3.up * camFollow.heightOffset);
            }
        }
        else
        {
            Debug.LogError("[CaveFadeTeleport] Player object or Target Spawn Point is missing!");
        }

        yield return new WaitForSeconds(0.2f);

        // 3. Fade back in to clear (2 seconds)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(1.0f - (elapsed / fadeDuration));
            }
            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (pMovement != null) pMovement.enabled = true;

        isTeleporting = false;
        currentTimer = countdownTime;
        Debug.Log($"[CaveFadeTeleport] Teleport sequence completed successfully! ({(isEnteringCave ? "Inside Cave" : "Outside")})");
    }
}
