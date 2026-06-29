using System.Collections;
using UnityEngine;

/// <summary>
/// Da vida a la impresora 3D doméstica de la sala 2080. Cada cierto intervalo
/// aleatorio "fabrica algo": emite un zumbido suave de trabajo con audio
/// espacial y su LED de estado parpadea durante ese rato; luego vuelve a reposo.
/// Es el equivalente al teléfono que suena en 2010: efecto intermitente con
/// sonido, que transmite un hogar que "produce solo".
///
/// Uso: agregar al GameObject de la impresora y asignar el clip del zumbido.
/// Opcionalmente asignar una Light como LED de estado (si no, solo suena).
/// Ligero en Quest: un AudioSource 3D y, durante el trabajo, una luz sin sombras.
/// </summary>
public class Printer3D : MonoBehaviour
{
    [Header("Sonido de trabajo")]
    [Tooltip("Zumbido/motor de la impresora. Idealmente un loop suave y mecánico.")]
    public AudioClip zumbido;
    [Range(0f, 1f)] public float volumen = 0.5f;
    [Tooltip("Distancia a la que el zumbido deja de oírse (rolloff 3D).")]
    public float distanciaMaxima = 8f;

    [Header("LED de estado (opcional)")]
    [Tooltip("Luz que parpadea mientras imprime. Si se deja vacía, solo suena.")]
    public Light ledEstado;
    [Tooltip("Color del LED cuando está imprimiendo.")]
    public Color colorImprimiendo = new Color(0.4f, 1f, 0.5f);
    [Tooltip("Intensidad pico del LED al parpadear.")]
    public float intensidadLed = 1.2f;
    [Tooltip("Velocidad del parpadeo del LED mientras trabaja.")]
    public float velocidadParpadeo = 4f;

    [Header("Ritmo")]
    [Tooltip("Segundos que dura cada trabajo de impresión.")]
    public float duracionTrabajo = 12f;
    [Tooltip("Intervalo aleatorio de reposo entre trabajos (mín, máx) en segundos.")]
    public float esperaMinima = 30f;
    public float esperaMaxima = 75f;
    [Tooltip("Espera inicial antes del primer trabajo.")]
    public float esperaInicial = 10f;

    AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = zumbido;
        audioSource.loop = true;            // se repite durante 'duracionTrabajo'
        audioSource.playOnAwake = false;
        audioSource.volume = volumen;
        audioSource.spatialBlend = 1f;      // 3D: sale desde la impresora
        audioSource.dopplerLevel = 0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.minDistance = 1f;
        audioSource.maxDistance = distanciaMaxima;

        if (ledEstado != null)
        {
            ledEstado.shadows = LightShadows.None;   // barato en Quest
            ledEstado.color = colorImprimiendo;
            ledEstado.intensity = 0f;                // apagado en reposo
        }
    }

    void Start()
    {
        StartCoroutine(CicloDeImpresion());
    }

    IEnumerator CicloDeImpresion()
    {
        yield return new WaitForSeconds(esperaInicial);

        while (true)
        {
            // ── Trabajando ──
            if (zumbido != null) audioSource.Play();

            float t = 0f;
            while (t < duracionTrabajo)
            {
                t += Time.deltaTime;
                if (ledEstado != null)
                {
                    // Parpadeo suave del LED mientras imprime (seno -> [0,1]).
                    float p = (Mathf.Sin(Time.time * velocidadParpadeo) + 1f) * 0.5f;
                    ledEstado.intensity = p * intensidadLed;
                }
                yield return null;
            }

            // ── Reposo ──
            if (zumbido != null) audioSource.Stop();
            if (ledEstado != null) ledEstado.intensity = 0f;

            yield return new WaitForSeconds(Random.Range(esperaMinima, esperaMaxima));
        }
    }
}
