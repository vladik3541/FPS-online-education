using UnityEngine;
using Photon.Pun;

public class BombAudioManager : MonoBehaviourPunCallbacks
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bombAudioSource;
    [SerializeField] private AudioSource uiAudioSource;
    
    [Header("Bomb Sounds")]
    [SerializeField] private AudioClip plantSound;
    [SerializeField] private AudioClip plantingSound; // Loop під час закладання
    [SerializeField] private AudioClip defuseSound;
    [SerializeField] private AudioClip defusingSound; // Loop під час знешкодження
    [SerializeField] private AudioClip bombTickSound;
    [SerializeField] private AudioClip bombTickFastSound; // Коли < 10 секунд
    [SerializeField] private AudioClip explosionSound;
    
    [Header("UI Sounds")]
    [SerializeField] private AudioClip bombPlantedAnnouncement;
    [SerializeField] private AudioClip bombDefusedAnnouncement;
    [SerializeField] private AudioClip tenSecondsWarning;
    
    private BombManager bombManager;
    private BombState lastBombState;
    private bool playedTenSecondWarning = false;
    private float tickTimer = 0f;
    private float tickInterval = 1f;

    void Start()
    {
        bombManager = BombManager.Instance;
        
        if (bombAudioSource == null)
        {
            bombAudioSource = gameObject.AddComponent<AudioSource>();
            bombAudioSource.spatialBlend = 1f; // 3D звук
            bombAudioSource.maxDistance = 50f;
        }
        
        if (uiAudioSource == null)
        {
            uiAudioSource = gameObject.AddComponent<AudioSource>();
            uiAudioSource.spatialBlend = 0f; // 2D звук
        }
    }

    void Update()
    {
        if (bombManager == null) return;

        BombState currentState = bombManager.GetBombState();
        
        // Перевіряємо зміну стану
        if (currentState != lastBombState)
        {
            OnBombStateChanged(lastBombState, currentState);
            lastBombState = currentState;
        }

        // Логіка звуків в залежності від стану
        switch (currentState)
        {
            case BombState.Planting:
                PlayPlantingLoop();
                break;
                
            case BombState.Planted:
                PlayBombTick();
                CheckTenSecondWarning();
                break;
                
            case BombState.Defusing:
                PlayDefusingLoop();
                break;
        }
    }

    private void OnBombStateChanged(BombState oldState, BombState newState)
    {
        // Зупиняємо всі loop звуки
        if (bombAudioSource.isPlaying)
        {
            bombAudioSource.Stop();
        }

        switch (newState)
        {
            case BombState.Planting:
                Debug.Log("🎵 Починається закладання бомби");
                break;
                
            case BombState.Planted:
                PlaySound(bombAudioSource, plantSound);
                PlaySound(uiAudioSource, bombPlantedAnnouncement);
                tickTimer = 0f;
                playedTenSecondWarning = false;
                Debug.Log("🎵 Бомбу закладено");
                
                // Позиціонуємо AudioSource на місці бомби
                if (bombManager != null)
                {
                    transform.position = bombManager.GetBombPosition();
                }
                break;
                
            case BombState.Defusing:
                Debug.Log("🎵 Починається знешкодження");
                break;
                
            case BombState.Defused:
                PlaySound(bombAudioSource, defuseSound);
                PlaySound(uiAudioSource, bombDefusedAnnouncement);
                Debug.Log("🎵 Бомбу знешкоджено");
                break;
                
            case BombState.Exploded:
                PlaySound(bombAudioSource, explosionSound);
                Debug.Log("🎵 Вибух!");
                break;
                
            case BombState.NotPlanted:
                playedTenSecondWarning = false;
                tickTimer = 0f;
                break;
        }
    }

    private void PlayPlantingLoop()
    {
        if (plantingSound != null && !bombAudioSource.isPlaying)
        {
            bombAudioSource.clip = plantingSound;
            bombAudioSource.loop = true;
            bombAudioSource.Play();
        }
    }

    private void PlayDefusingLoop()
    {
        if (defusingSound != null && !bombAudioSource.isPlaying)
        {
            bombAudioSource.clip = defusingSound;
            bombAudioSource.loop = true;
            bombAudioSource.Play();
        }
    }

    private void PlayBombTick()
    {
        if (bombTickSound == null && bombTickFastSound == null) return;

        double timeRemaining = bombManager.GetBombTimeRemaining();
        
        // Швидше тиканя коли < 10 секунд
        if (timeRemaining <= 10)
        {
            tickInterval = 0.5f;
        }
        else
        {
            tickInterval = 1f;
        }

        tickTimer += Time.deltaTime;
        
        if (tickTimer >= tickInterval)
        {
            AudioClip tickClip = timeRemaining <= 10 ? bombTickFastSound : bombTickSound;
            PlaySound(bombAudioSource, tickClip);
            tickTimer = 0f;
        }
    }

    private void CheckTenSecondWarning()
    {
        if (playedTenSecondWarning) return;
        
        double timeRemaining = bombManager.GetBombTimeRemaining();
        
        if (timeRemaining <= 10)
        {
            PlaySound(uiAudioSource, tenSecondsWarning);
            playedTenSecondWarning = true;
            Debug.Log("⚠️ 10 СЕКУНД ДО ВИБУХУ!");
        }
    }

    private void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source == null || clip == null) return;
        
        source.PlayOneShot(clip);
    }

    // RPC методи для синхронізації звуків між гравцями
    [PunRPC]
    public void PlaySoundRPC(string soundName)
    {
        AudioClip clip = soundName switch
        {
            "plant" => plantSound,
            "defuse" => defuseSound,
            "explosion" => explosionSound,
            "bombPlanted" => bombPlantedAnnouncement,
            "bombDefused" => bombDefusedAnnouncement,
            _ => null
        };

        if (clip != null)
        {
            PlaySound(bombAudioSource, clip);
        }
    }

    // Публічні методи для виклику з BombManager
    public void OnBombPlantedSound()
    {
        photonView.RPC(nameof(PlaySoundRPC), RpcTarget.All, "plant");
    }

    public void OnBombDefusedSound()
    {
        photonView.RPC(nameof(PlaySoundRPC), RpcTarget.All, "defuse");
    }

    public void OnBombExplodedSound()
    {
        photonView.RPC(nameof(PlaySoundRPC), RpcTarget.All, "explosion");
    }
}