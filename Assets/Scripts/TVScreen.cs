using System.IO;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Reproduce un video en la pantalla del televisor de la sala 2010.
/// Pipeline optimizado para Quest 3 standalone:
///   VideoPlayer -> RenderTexture -> material Unlit emisivo en el mesh "Screen".
///
/// Usar UN solo MP4 H.264 720p 30fps en loop (un único stream = decodificación
/// por hardware, costo mínimo). Ver notas de códec en el comentario de abajo.
///
/// Uso: agregar este componente al GameObject "Screen" (la superficie de la
/// pantalla) o a cualquier objeto y asignar "pantalla" manualmente. Asignar
/// un VideoClip en el Inspector, o dejar "nombreArchivoStreamingAssets" para
/// cargar desde Assets/StreamingAssets/.
/// </summary>
public class TVScreen : MonoBehaviour
{
    [Header("Fuente de video")]
    [Tooltip("VideoClip importado en el proyecto. Tiene prioridad sobre el archivo de StreamingAssets.")]
    public VideoClip clip;
    [Tooltip("Alternativa: nombre del archivo dentro de Assets/StreamingAssets/ " +
             "(ej. 'colombia2010.mp4'). Se usa solo si 'clip' está vacío. " +
             "Mantiene el video fuera del peso de compilación de escenas.")]
    public string nombreArchivoStreamingAssets = "colombia2010.mp4";

    [Header("Pantalla")]
    [Tooltip("MeshRenderer de la superficie de la pantalla. Si se deja vacío, " +
             "se usa el MeshRenderer de este mismo GameObject.")]
    public MeshRenderer pantalla;
    [Tooltip("Resolución del RenderTexture. 1280x720 es suficiente para un TV en VR.")]
    public int ancho = 1280;
    public int alto = 720;
    [Tooltip("Brillo de la pantalla. >1 la hace ver encendida/emisiva en ambientes oscuros.")]
    [Range(0.5f, 3f)] public float brillo = 1.2f;

    [Header("Reproducción")]
    public bool reproducirAlIniciar = true;
    public bool loop = true;
    [Tooltip("Stretch = llena toda la pantalla (sin barras negras). " +
             "FitInside = respeta proporción y deja barras si no coincide.")]
    public VideoAspectRatio modoAspecto = VideoAspectRatio.Stretch;

    [Header("Audio")]
    [Tooltip("Si está activo, el audio del video sale espacializado desde el TV.")]
    public bool audioEspacial = true;
    [Range(0f, 1f)] public float volumen = 0.6f;

    VideoPlayer player;
    RenderTexture rt;
    AudioSource audioSource;

    void Awake()
    {
        if (pantalla == null) pantalla = GetComponent<MeshRenderer>();

        Configurar();

        if (reproducirAlIniciar) Play();
    }

    void Configurar()
    {
        // RenderTexture donde el VideoPlayer vuelca cada frame.
        rt = new RenderTexture(ancho, alto, 0, RenderTextureFormat.Default)
        {
            name = "TV_RenderTexture"
        };
        rt.Create();

        // Material Unlit (no le afectan las luces -> parece pantalla encendida).
        // Se busca el shader de URP en runtime para evitar shader stripping en Quest.
        if (pantalla != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Texture");
            Material mat = new Material(shader);
            mat.SetTexture("_BaseMap", rt);
            mat.mainTexture = rt;
            // Sube el color base por encima de 1 para simular emisión sin shader extra.
            Color c = new Color(brillo, brillo, brillo, 1f);
            mat.SetColor("_BaseColor", c);
            mat.color = c;
            pantalla.material = mat;
        }

        // VideoPlayer
        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.isLooping = loop;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = rt;
        player.skipOnDrop = true; // mantiene sincronía sin trabar el render
        player.waitForFirstFrame = true;
        player.aspectRatio = modoAspecto;

        // Fuente: VideoClip importado o archivo en StreamingAssets.
        if (clip != null)
        {
            player.source = VideoSource.VideoClip;
            player.clip = clip;
        }
        else
        {
            player.source = VideoSource.Url;
            // En Android (Quest) streamingAssetsPath ya es una URL jar:// válida.
            player.url = Path.Combine(Application.streamingAssetsPath, nombreArchivoStreamingAssets);
        }

        // Audio
        if (audioEspacial)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D, sale desde el TV
            audioSource.volume = volumen;
            audioSource.playOnAwake = false;
            audioSource.dopplerLevel = 0f;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audioSource);
        }
        else
        {
            player.audioOutputMode = VideoAudioOutputMode.None;
        }
    }

    // ─── API pública (llamable desde NarratorNetworkController) ──────────────
    public void Play()
    {
        if (player != null) player.Play();
    }

    public void Stop()
    {
        if (player != null) player.Stop();
    }

    public void Pause()
    {
        if (player != null) player.Pause();
    }

    void OnDestroy()
    {
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }
    }
}
