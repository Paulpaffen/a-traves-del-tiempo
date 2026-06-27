using System.Collections;
using UnityEngine;

/// <summary>
/// Hace que el teléfono fijo de la sala 2010 suene de vez en cuando con audio
/// espacial. Suena por unos segundos, espera un intervalo aleatorio y repite.
///
/// Uso: agregar al GameObject "Telefono fijo" y asignar un AudioClip de timbre.
/// Ligero en Quest: un solo AudioSource 3D, sin nada por frame.
/// </summary>
public class RingingPhone : MonoBehaviour
{
    [Header("Sonido")]
    [Tooltip("Clip del timbre. Idealmente un timbre de teléfono de la época (un ciclo 'ring-ring').")]
    public AudioClip timbre;
    [Range(0f, 1f)] public float volumen = 0.7f;
    [Tooltip("Distancia a la que el timbre deja de oírse (rolloff 3D).")]
    public float distanciaMaxima = 12f;

    [Header("Ritmo")]
    [Tooltip("Segundos que suena cada vez (el clip se repite durante este tiempo).")]
    public float duracionTimbre = 6f;
    [Tooltip("Intervalo aleatorio de silencio entre llamadas (mín, máx) en segundos.")]
    public float esperaMinima = 25f;
    public float esperaMaxima = 60f;
    [Tooltip("Espera inicial antes de la primera llamada.")]
    public float esperaInicial = 15f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = timbre;
        audioSource.loop = true;            // se repite durante 'duracionTimbre'
        audioSource.playOnAwake = false;
        audioSource.volume = volumen;
        audioSource.spatialBlend = 1f;      // 3D: sale desde el teléfono
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = distanciaMaxima;
    }

    void Start()
    {
        StartCoroutine(CicloDeLlamadas());
    }

    IEnumerator CicloDeLlamadas()
    {
        yield return new WaitForSeconds(esperaInicial);

        while (true)
        {
            if (timbre != null)
            {
                audioSource.Play();
                yield return new WaitForSeconds(duracionTimbre);
                audioSource.Stop();
            }

            yield return new WaitForSeconds(Random.Range(esperaMinima, esperaMaxima));
        }
    }
}
