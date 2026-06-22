using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RadioMusicParticles : MonoBehaviour
{
    [Header("Apariencia")]
    public Texture2D notaTextura;
    [ColorUsage(true, false)]
    public Color colorInicial = new Color(1f, 0.85f, 0.3f, 1f);
    public float tamanoMinimo = 0.04f;
    public float tamanoMaximo = 0.07f;

    [Header("Comportamiento")]
    [Range(0.5f, 5f)] public float velocidadEmision = 1.5f;
    public float alturaMax = 0.6f;

    ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        ConfigurarSistema();
    }

    void ConfigurarSistema()
    {
        // Main
        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoMinimo, tamanoMaximo);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.gravityModifier = -0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 20;

        // Emision
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = velocidadEmision;

        // Forma: esfera pequena en la parte superior de la radio
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.03f;

        // Velocidad lateral aleatoria (drift suave)
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0.05f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

        // Tamaño: crece un poco al inicio, luego se achica al final
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curvaTamano = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.75f, 0.9f),
            new Keyframe(1f, 0f)
        );
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curvaTamano);

        // Rotacion lenta
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

        // Color: dorado -> amarillo palido -> transparente
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorInicial, 0f),
                new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0.6f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.1f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradiente);

        // Renderer: billboard, con la textura de nota si esta asignada
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge = -10f;

        if (notaTextura != null)
        {
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.mainTexture = notaTextura;
            mat.SetFloat("_Mode", 2f); // Fade
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }
    }
}
