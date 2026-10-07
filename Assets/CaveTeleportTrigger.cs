using UnityEngine;
using TMPro;

public class CaveTeleportTrigger : MonoBehaviour
{
    public Transform caveSpawnPoint;
    public float waitTime = 5f;

    [Header("UI")]
    public TMP_Text countdownText;

    private float timer;
    private GameObject player;
    private bool playerInside;

    private void Start()
    {
        if (caveSpawnPoint == null)
        {
            GameObject spawnObj = GameObject.Find("ChanTinhCaveSpawnPoint");
            if (spawnObj != null)
            {
                caveSpawnPoint = spawnObj.transform;
            }
        }

        if (countdownText == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                Transform textTr = canvas.transform.Find("CaveCountdownText");
                if (textTr != null)
                {
                    countdownText = textTr.GetComponent<TMP_Text>();
                }
            }
        }

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    private bool IsPlayer(Collider other, out GameObject playerObj)
    {
        playerObj = null;

        if (other == null) return false;

        string colName = other.gameObject.name.ToLower();
        string rootName = other.transform.root != null ? other.transform.root.name.ToLower() : "";

        // Ignore mountain / mesh objects completely
        if (colName.Contains("dragonspire") || colName.Contains("mountain") || colName.Contains("meshy") ||
            rootName.Contains("dragonspire") || rootName.Contains("mountain") || rootName.Contains("meshy"))
        {
            return false;
        }

        // Check if other or root is thachsanh or tagged Player
        if (other.CompareTag("Player") || rootName.Contains("thachsanh") || colName.Contains("thachsanh"))
        {
            if (other.transform.root != null && (rootName.Contains("thachsanh") || other.transform.root.CompareTag("Player")))
            {
                playerObj = other.transform.root.gameObject;
            }
            else
            {
                playerObj = other.gameObject;
            }
            return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other, out GameObject targetPlayer))
            return;

        player = targetPlayer;

        if (!player.CompareTag("Player"))
        {
            player.tag = "Player";
        }

        Debug.Log($"Thach Sanh ({player.name}) entered cave trigger on mountain area");
        Debug.Log("Player entered cave trigger");
        Debug.Log("Cave timer started");

        playerInside = true;
        timer = 0f;

        if (countdownText != null)
        {
            countdownText.text = "Entering cave in " + Mathf.CeilToInt(waitTime) + "...";
            countdownText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsPlayer(other, out GameObject targetPlayer) || !playerInside)
            return;

        timer += Time.deltaTime;

        float remaining = waitTime - timer;

        if (countdownText != null)
        {
            int displayCount = Mathf.Max(0, Mathf.CeilToInt(remaining));
            countdownText.text = "Entering cave in " + displayCount + "...";
        }

        if (timer >= waitTime)
        {
            Debug.Log("Teleporting player to cave");
            TeleportPlayer();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other, out GameObject targetPlayer))
            return;

        Debug.Log($"Thach Sanh ({targetPlayer.name}) left cave trigger");
        timer = 0f;
        playerInside = false;
        player = null;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    [Header("Cave Volume")]
    public GameObject caveVolume;

    private void TeleportPlayer()
    {
        if (caveSpawnPoint == null)
        {
            GameObject spawnObj = GameObject.Find("ChanTinhCaveSpawnPoint");
            if (spawnObj != null)
            {
                caveSpawnPoint = spawnObj.transform;
            }
        }

        if (player == null || caveSpawnPoint == null)
        {
            Debug.LogError("Player or ChanTinhCaveSpawnPoint is missing!");
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller != null)
            controller.enabled = false;

        player.transform.position = caveSpawnPoint.position;
        player.transform.rotation = caveSpawnPoint.rotation;

        if (controller != null)
            controller.enabled = true;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (caveVolume == null)
        {
            caveVolume = GameObject.Find("CaveVolume");
        }

        if (caveVolume != null)
        {
            caveVolume.SetActive(true);
            Debug.Log("CaveVolume ENABLED after teleporting to cave");
        }

        timer = 0f;
        playerInside = false;
    }
}
