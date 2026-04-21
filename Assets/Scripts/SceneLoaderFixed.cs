using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.Samples
{
    public class SceneLoaderFixed : MonoBehaviour
    {
        private bool _loading = false;
        public Action<string> WhenLoadingScene = delegate { };
        public Action<string> WhenSceneLoaded = delegate { };
        private int _waitingCount = 0;

        [Header("Fade Settings")]
        [SerializeField] private bool _useFade = true;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _blackScreenDelay = 0.5f;

        public void Load(string sceneName)
        {
            if (_loading) return;
            _loading = true;
            _waitingCount = WhenLoadingScene.GetInvocationList().Length - 1;
            if (_waitingCount == 0)
            {
                HandleReadyToLoad(sceneName);
            }
            else
            {
                WhenLoadingScene.Invoke(sceneName);
            }
        }

        public void HandleReadyToLoad(string sceneName)
        {
            _waitingCount--;
            if (_waitingCount <= 0)
            {
                StartCoroutine(LoadSceneAsync(sceneName));
            }
        }

#if UNITY_EDITOR
        private Dictionary<string, string> _sceneNameToPath = new();
        internal void LoadByPath(string sceneName, string scenePath)
        {
            if (_loading)
            {
                return;
            }
            _sceneNameToPath[sceneName] = scenePath;
            Load(sceneName);
        }
#endif

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // Fade a negro antes de cargar
            if (_useFade && VRFadeScreen.instance != null)
            {
                yield return StartCoroutine(VRFadeScreen.instance.FadeOut(_fadeOutDuration));
            }

            AsyncOperation asyncLoad;
#if UNITY_EDITOR
            if (_sceneNameToPath.TryGetValue(sceneName, out string scenePath))
            {
                asyncLoad = UnityEditor.SceneManagement.EditorSceneManager
                    .LoadSceneAsyncInPlayMode(scenePath, new LoadSceneParameters(LoadSceneMode.Single));
            }
            else
#endif
            {
                asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            }

            while (!asyncLoad.isDone)
            {
                yield return null;
            }

            // Espera extra en negro para que todo se inicialice
            if (_useFade)
            {
                yield return new WaitForSeconds(_blackScreenDelay);
                yield return new WaitForEndOfFrame();
            }

            _loading = false;
            WhenSceneLoaded.Invoke(sceneName);

            // Fade desde negro después de cargar
            if (_useFade && VRFadeScreen.instance != null)
            {
                yield return StartCoroutine(VRFadeScreen.instance.FadeIn(_fadeInDuration));
            }
        }
    }
}