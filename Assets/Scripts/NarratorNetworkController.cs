using System;
using System.Collections;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class NarratorNetworkController : MonoBehaviour
{
    [Serializable]
    private struct NarrationStep
    {
        public AudioClip clip;
        // Scene to load after this clip ends. Leave empty on the last step.
        public string nextScene;
    }

    [Header("Narration sequence")]
    [SerializeField] private NarrationStep[] steps;
    // Scene everyone returns to automatically after the experience ends.
    [SerializeField] private string resetScene = "1930";

    [Header("Timing (seconds)")]
    // Buffer after a scene finishes loading before narration starts.
    // Gives peers with slower loads enough time to finish.
    [SerializeField] private float narrationStartDelay = 2f;
    // How long to wait after the exit animation before sending everyone back to resetScene.
    [SerializeField] private float exitReturnDelay = 15f;
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private float blackScreenDelay = 1f;

    private NetworkContext context;
    // Static so it survives DontDestroyOnLoad between scenes.
    private static int currentStepIndex = 0;
    private bool isAuthority = false;
    private AudioSource audioSource;
    // Guards against Update firing OnNarrationEnded before audio has even started.
    private bool audioWasPlaying = false;

    [Serializable]
    private struct NetMessage
    {
        public string type;
        public int stepIndex;
        public string sceneName;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        context = NetworkScene.Register(this);
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Connect to the "1930" (or any scene) button in SampleScene.
    /// Sends everyone in the room to the given scene simultaneously.
    /// </summary>
    public void GoToSceneForAll(string sceneName)
    {
        context.SendJson(new NetMessage { type = "GoToScene", sceneName = sceneName });
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Connect to the staff button inside the 1930 scene.
    /// Starts narration for every connected headset and drives the full experience.
    /// </summary>
    public void StartNarration()
    {
        if (isAuthority) return;
        isAuthority = true;
        currentStepIndex = 0;
        context.SendJson(new NetMessage { type = "PlayAudio", stepIndex = 0 });
        PlayLocalAudio(0);
    }

    // ─── Internal flow ───────────────────────────────────────────────────────

    private void PlayLocalAudio(int stepIndex)
    {
        if (stepIndex >= steps.Length) return;
        audioSource.clip = steps[stepIndex].clip;
        if (audioSource.clip != null)
        {
            audioSource.Play();
            audioWasPlaying = true;
        }
        else
        {
            // No clip assigned: skip narration and advance immediately.
            if (isAuthority)
                StartCoroutine(SkipToNextStep());
        }
    }

    private IEnumerator SkipToNextStep()
    {
        yield return null;
        OnNarrationEnded();
    }

    private void Update()
    {
        if (!isAuthority || !audioWasPlaying) return;
        if (audioSource.clip != null && !audioSource.isPlaying)
        {
            audioWasPlaying = false;
            OnNarrationEnded();
        }
    }

    private void OnNarrationEnded()
    {
        string next = steps[currentStepIndex].nextScene;
        if (!string.IsNullOrEmpty(next))
        {
            currentStepIndex++;
            TransitionToNextScene(next);
        }
        else
        {
            // Last step: trigger exit animation for everyone, then auto-return.
            context.SendJson(new NetMessage { type = "PlayExitAnimation" });
            TriggerExitDoor();
            StartCoroutine(ExitReturnSequence());
        }
    }

    private void TransitionToNextScene(string sceneName)
    {
        context.SendJson(new NetMessage { type = "GoToScene", sceneName = sceneName });
        StartCoroutine(LoadSceneAndStartNarration(sceneName));
    }

    private IEnumerator LoadSceneAndStartNarration(string sceneName)
    {
        yield return StartCoroutine(LoadSceneCoroutine(sceneName));
        yield return new WaitForSeconds(narrationStartDelay);
        context.SendJson(new NetMessage { type = "PlayAudio", stepIndex = currentStepIndex });
        PlayLocalAudio(currentStepIndex);
    }

    // Shared fade → load → fade coroutine used for all scene transitions.
    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        if (VRFadeScreen.instance != null)
            yield return StartCoroutine(VRFadeScreen.instance.FadeOut(fadeOutDuration));

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return new WaitForSeconds(blackScreenDelay);

        if (VRFadeScreen.instance != null)
            yield return StartCoroutine(VRFadeScreen.instance.FadeIn(fadeInDuration));
    }

    // Waits for the exit animation to play, then sends everyone back to resetScene.
    private IEnumerator ExitReturnSequence()
    {
        yield return new WaitForSeconds(exitReturnDelay);
        isAuthority = false;
        currentStepIndex = 0;
        context.SendJson(new NetMessage { type = "GoToScene", sceneName = resetScene });
        StartCoroutine(LoadSceneCoroutine(resetScene));
    }

    private void TriggerExitDoor()
    {
        var door = GameObject.FindWithTag("ExitDoor");
        door?.GetComponent<Animator>()?.SetTrigger("Open");
    }

    // ─── Ubiq message receiver ───────────────────────────────────────────────

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<NetMessage>();
        switch (msg.type)
        {
            case "GoToScene":
                StartCoroutine(LoadSceneCoroutine(msg.sceneName));
                break;
            case "PlayAudio":
                PlayLocalAudio(msg.stepIndex);
                break;
            case "PlayExitAnimation":
                TriggerExitDoor();
                break;
        }
    }
}
