using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    public AudioClip bgmClip;
    [Range(0f, 1f)] public float normalVolume = 0.5f;
    [Range(0f, 1f)] public float duckedVolume = 0.2f;
    public float fadeSpeed = 2f;

    AudioSource source;
    float targetVolume;

    void Awake()
    {
        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.clip = bgmClip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = normalVolume;
        targetVolume = normalVolume;
        source.Play();
    }

    void Update()
    {
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, fadeSpeed * Time.deltaTime);
    }

    public void Duck(bool on)
    {
        targetVolume = on ? duckedVolume : normalVolume;
    }
}