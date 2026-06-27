using UnityEngine;

/// <summary>
/// Partículas de polvo flotando lentamente por toda la habitación (efecto
/// "cuarto viejo con luz filtrándose"). Configurado por código y optimizado
/// para Quest 3 standalone (URP, sin depth texture, conteo de partículas bajo).
///
/// Uso: crear un GameObject vacío en el centro de la sala de 1930, agregar
/// este componente y ajustar "tamanoSala" para que cubra la habitación.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class DustMotes : MonoBehaviour
{
    [Header("Apariencia")]
    [Tooltip("Material opcional con shader 'Universal Render Pipeline/Particles/Unlit'. " +
             "Asignarlo garantiza que el shader se incluya en el build de Quest. " +
             "Si se deja vacío se genera uno en runtime con una textura suave.")]
    public Material materialPolvo;
    [ColorUsage(true, false)]
    public Color color = new Color(0.95f, 0.94f, 0.9f, 1f);
    [Tooltip("Tamaño de cada mota en metros. Muy pequeñas para que se lean como polvo.")]
    public float tamanoMinimo = 0.004f;
    public float tamanoMaximo = 0.012f;
    [Range(0f, 1f)] public float opacidad = 0.5f;

    [Header("Volumen / Densidad")]
    [Tooltip("Tamaño de la caja (ancho, alto, profundidad) que llena de polvo. " +
             "Debe cubrir la sala. Centrado en este GameObject.")]
    public Vector3 tamanoSala = new Vector3(6f, 3f, 6f);
    [Tooltip("Cantidad máxima de motas visibles a la vez. " +
             "Quest 3 aguanta varios cientos sin problema; mantenerlo bajo igual.")]
    [Range(50, 800)] public int maxParticulas = 300;

    [Header("Movimiento")]
    [Tooltip("Velocidad del deriva lento. Muy bajo = casi suspendidas en el aire.")]
    public float velocidadDeriva = 0.02f;
    [Tooltip("Fuerza del remolino orgánico (módulo Noise).")]
    public float turbulencia = 0.03f;

    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        ConfigurarSistema();
    }

    void ConfigurarSistema()
    {
        // Las motas viven mucho tiempo y se re-emiten constantemente. Con vidas
        // largas + un rate moderado el cuarto se mantiene lleno de forma estable.
        float vidaMedia = 18f;

        // Main
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true; // el cuarto ya aparece lleno de polvo al cargar la escena
        main.startLifetime = new ParticleSystem.MinMaxCurve(vidaMedia * 0.7f, vidaMedia * 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, velocidadDeriva);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoMinimo, tamanoMaximo);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = color;
        main.gravityModifier = 0f; // el polvo flota, no cae
        main.simulationSpace = ParticleSystemSimulationSpace.World; // queda fijo en la sala
        main.maxParticles = maxParticulas;

        // Emisión: rate calculado para mantener ~maxParticulas vivas dado la vida media.
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = maxParticulas / vidaMedia;

        // Forma: caja que llena toda la habitación.
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = tamanoSala;
        shape.position = Vector3.zero;

        // Deriva lateral aleatoria y muy suave en los tres ejes.
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-velocidadDeriva, velocidadDeriva);
        vel.y = new ParticleSystem.MinMaxCurve(-velocidadDeriva * 0.4f, velocidadDeriva * 0.5f);
        vel.z = new ParticleSystem.MinMaxCurve(-velocidadDeriva, velocidadDeriva);

        // Noise: da el movimiento orgánico de remolino. Frecuencia baja = lento.
        var noise = ps.noise;
        noise.enabled = turbulencia > 0f;
        noise.strength = turbulencia;
        noise.frequency = 0.15f;
        noise.scrollSpeed = 0.05f;
        noise.quality = ParticleSystemNoiseQuality.Low; // barato para Quest
        noise.damping = true;

        // Fade in al nacer y fade out al morir para que no "aparezcan/desaparezcan".
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(opacidad, 0.2f),
                new GradientAlphaKey(opacidad, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Leve parpadeo de tamaño para simular el centelleo del polvo en la luz.
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        AnimationCurve curva = new AnimationCurve(
            new Keyframe(0f, 0.8f),
            new Keyframe(0.5f, 1f),
            new Keyframe(1f, 0.8f)
        );
        size.size = new ParticleSystem.MinMaxCurve(1f, curva);

        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.sortingFudge = -5f;
        renderer.material = CrearMaterial();
    }

    Material CrearMaterial()
    {
        Material mat;
        if (materialPolvo != null)
        {
            // Material asignado desde el Inspector: garantiza que el shader de URP
            // entre en el build de Quest (sin riesgo de shader stripping -> rosa).
            mat = new Material(materialPolvo);
        }
        else
        {
            // Fallback en runtime. El Built-in "Particles/Standard Unlit" no existe
            // en URP, así que se busca el de URP y si no, Sprites/Default.
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            mat = new Material(shader);
        }

        // Textura suave circular generada por código (no requiere importar assets).
        Texture2D tex = CrearTexturaSuave(32);
        mat.SetTexture("_BaseMap", tex);
        mat.mainTexture = tex;
        mat.SetColor("_BaseColor", Color.white);

        // Transparencia alpha blend (no additive: el polvo no debe brillar de más).
        mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
        mat.SetFloat("_Blend", 0f);   // 0 = Alpha
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    // Genera un disco suave con caída radial (alpha gaussiano aproximado).
    Texture2D CrearTexturaSuave(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float centro = (size - 1) * 0.5f;
        float radio = centro;
        Color[] pixeles = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centro, centro)) / radio;
                float a = Mathf.Clamp01(1f - dist);
                a = a * a; // caída más suave hacia el borde
                pixeles[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        }
        tex.SetPixels(pixeles);
        tex.Apply();
        return tex;
    }

#if UNITY_EDITOR
    // Dibuja la caja del volumen de polvo en la vista de escena para ubicarla bien.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, tamanoSala);
    }
#endif
}
