using System.Collections;
using UnityEngine;
using TMPro;

public class CaveTeleportTrigger : MonoBehaviour
{
    public Transform caveSpawnPoint;
    public float waitTime = 5f;

    [Header("UI")]
    public TMP_Text countdownText;

    [Header("Cave Volume")]
    public GameObject caveVolume;

    private CaveFadeTeleport fadeTeleport;

    private void Awake()
    {
        fadeTeleport = GetComponent<CaveFadeTeleport>();
        if (fadeTeleport == null)
        {
            fadeTeleport = gameObject.AddComponent<CaveFadeTeleport>();
        }

        fadeTeleport.isEnteringCave = true;
        fadeTeleport.countdownTime = waitTime;
        if (caveSpawnPoint != null)
            fadeTeleport.targetSpawnPoint = caveSpawnPoint;
        if (countdownText != null)
            fadeTeleport.countdownText = countdownText;
    }

    private void Start()
    {
        if (caveSpawnPoint == null)
        {
            GameObject spawnObj = GameObject.Find("ChanTinhCaveSpawnPoint");
            if (spawnObj != null)
            {
                caveSpawnPoint = spawnObj.transform;
                fadeTeleport.targetSpawnPoint = caveSpawnPoint;
            }
        }
    }
}
