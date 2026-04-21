using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VRFadeScreen : MonoBehaviour
{
    public static VRFadeScreen instance;

    [SerializeField] private Image _fadeImage;
    [SerializeField] private Canvas _canvas;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
            }

            SetupCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupCanvas()
    {
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 9999;

        if (_fadeImage != null)
        {
            _fadeImage.color = new Color(0, 0, 0, 0);
            _fadeImage.raycastTarget = false;
        }
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return Fade(0f, 1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return Fade(1f, 0f, duration);
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (_fadeImage == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            _fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        _fadeImage.color = new Color(0, 0, 0, endAlpha);
    }
}