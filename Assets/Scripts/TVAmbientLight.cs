using UnityEngine;

/// <summary>
/// Simula el resplandor parpadeante que un televisor encendido proyecta sobre
/// la habitación. Anima la intensidad y el color de una Light con ruido Perlin
/// (no lee el video, así que es muy barato en Quest 3 standalone).
///
/// Uso: crear un GameObject vacío frente al televisor, apuntando hacia la sala,
/// agregarle una Light (Point o Spot) y este componente. Si no hay Light se crea
/// una Point Light automáticamente. Mantener "Sin sombras" para el rendimiento.
/// </summary>
[RequireComponent(typeof(Light))]
public class TVAmbientLight : MonoBehaviour
{
    [Header("Brillo")]
    [Tooltip("Intensidad base de la luz del TV.")]
    public float intensidadBase = 1.2f;
    [Tooltip("Cuánto varía la intensidad con el parpadeo (0 = fijo).")]
    [Range(0f, 1f)] public float variacionIntensidad = 0.4f;
    [Tooltip("Velocidad del parpadeo. Más alto = cambios más nerviosos (escena de acción).")]
    public float velocidadParpadeo = 6f;

    [Header("Color")]
    [Tooltip("El color del TV oscila lentamente entre estos dos (azulado <-> cálido).")]
    public Color colorFrio = new Color(0.6f, 0.75f, 1f);
    public Color colorCalido = new Color(1f, 0.85f, 0.7f);
    [Tooltip("Velocidad del cambio de color (cambios de escena del programa).")]
    public float velocidadColor = 0.5f;

    Light luz;
    float semillaIntensidad;
    float semillaColor;

    void Awake()
    {
        luz = GetComponent<Light>();
        // Sin sombras: una luz realtime sin sombras es barata en Quest.
        luz.shadows = LightShadows.None;
        if (luz.type == LightType.Directional) luz.type = LightType.Point;

        // Semillas distintas para que intensidad y color no vayan sincronizados.
        semillaIntensidad = Random.value * 100f;
        semillaColor = Random.value * 100f;
    }

    void Update()
    {
        // Parpadeo de intensidad: ruido Perlin centrado en 0.5 -> [-1, 1].
        float ruido = (Mathf.PerlinNoise(semillaIntensidad, Time.time * velocidadParpadeo) - 0.5f) * 2f;
        luz.intensity = Mathf.Max(0f, intensidadBase + ruido * variacionIntensidad);

        // Deriva lenta de color entre frío y cálido.
        float t = Mathf.PerlinNoise(semillaColor, Time.time * velocidadColor);
        luz.color = Color.Lerp(colorFrio, colorCalido, t);
    }
}
