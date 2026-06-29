using UnityEngine;

/// <summary>
/// Luz "viva" para la cápsula de cultivo doméstico de la sala 2080. La luz
/// respira: sube y baja de intensidad en un ciclo lento (seno) y deriva de
/// color entre un violeta de cultivo y un blanco cálido. Es el equivalente
/// tecnológico a las motas de polvo de 1930: pasivo, constante, hipnótico.
///
/// Uso: crear un GameObject dentro/junto a la cápsula, agregarle una Light
/// (Point) y este componente. Si no hay Light se crea una Point Light. Sin
/// sombras para que sea barato en Quest 3 standalone.
/// </summary>
[RequireComponent(typeof(Light))]
public class GrowLight : MonoBehaviour
{
    [Header("Respiración (intensidad)")]
    [Tooltip("Intensidad media de la luz de cultivo.")]
    public float intensidadBase = 1.0f;
    [Tooltip("Cuánto sube/baja la intensidad respecto a la base (0 = fija).")]
    [Range(0f, 1f)] public float amplitud = 0.4f;
    [Tooltip("Duración de un ciclo completo de respiración, en segundos. Alto = muy lento.")]
    public float periodoRespiracion = 6f;

    [Header("Color")]
    [Tooltip("La luz deriva lentamente entre estos dos colores (cultivo <-> cálido).")]
    public Color colorCultivo = new Color(0.65f, 0.45f, 1f);   // violeta de grow-light
    public Color colorCalido = new Color(1f, 0.95f, 0.85f);
    [Tooltip("Duración de un ciclo completo de cambio de color, en segundos.")]
    public float periodoColor = 11f;

    Light luz;
    float faseColor;

    void Awake()
    {
        luz = GetComponent<Light>();
        luz.shadows = LightShadows.None;          // barato en Quest
        if (luz.type == LightType.Directional) luz.type = LightType.Point;

        // Fase aleatoria para que intensidad y color no arranquen sincronizados
        // (y para que dos cápsulas en la misma sala no respiren al unísono).
        faseColor = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        // Respiración suave: seno centrado en intensidadBase.
        float resp = Mathf.Sin(Time.time * (Mathf.PI * 2f / Mathf.Max(0.01f, periodoRespiracion)));
        luz.intensity = Mathf.Max(0f, intensidadBase + resp * amplitud);

        // Deriva de color: seno remapeado a [0,1] entre los dos colores.
        float c = (Mathf.Sin(faseColor + Time.time * (Mathf.PI * 2f / Mathf.Max(0.01f, periodoColor))) + 1f) * 0.5f;
        luz.color = Color.Lerp(colorCultivo, colorCalido, c);
    }
}
