using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class EndingSequenceController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera orbitCam;
    [SerializeField] private CinemachineVirtualCamera topCam;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;

    [Header("End Text")]
    [SerializeField] private CanvasGroup endTextCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float orbitDuration = 3f;
    [SerializeField] private float topViewDuration = 1f;
    [SerializeField] private float fadeDuration = 2.3f;
    [SerializeField] private float textFadeDelay = 0.2f;
    [SerializeField] private float textFadeDuration = 1.2f;

    private float timer = 0f;
    private bool switchedToTop = false;
    private bool isFading = false;
    private bool startTextFade = false;
    private float textTimer = 0f;

    void Start()
    {
        if (orbitCam != null) orbitCam.Priority = 10;
        if (topCam != null) topCam.Priority = 5;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        if (endTextCanvasGroup != null)
        {
            endTextCanvasGroup.alpha = 0f;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        // 1. 공전 카메라 -> 탑 위 카메라 전환
        if (!switchedToTop && timer >= orbitDuration)
        {
            switchedToTop = true;
            timer = 0f;

            if (orbitCam != null) orbitCam.Priority = 5;
            if (topCam != null) topCam.Priority = 20;

            Debug.Log("[Ending] Top camera 전환");
        }

        // 2. 탑 위 카메라를 잠깐 보여준 뒤 페이드 시작
        if (switchedToTop && !isFading && timer >= topViewDuration)
        {
            isFading = true;
            timer = 0f;

            Debug.Log("[Ending] Fade 시작");
        }

        // 3. 화면 페이드아웃
        if (isFading && fadeImage != null)
        {
            float t = Mathf.Clamp01(timer / fadeDuration);

            Color c = fadeImage.color;
            c.a = t;
            fadeImage.color = c;

            // 완전히 검게 덮인 뒤 텍스트 페이드 시작
            if (t >= 1f && !startTextFade)
            {
                startTextFade = true;
                textTimer = -textFadeDelay;
            }
        }

        // 4. 텍스트 스으윽 등장
        if (startTextFade && endTextCanvasGroup != null)
        {
            textTimer += Time.deltaTime;
            float textT = Mathf.Clamp01(textTimer / textFadeDuration);
            endTextCanvasGroup.alpha = textT;
        }
    }
}