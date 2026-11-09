using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using TMPro;
using System.Collections;

[System.Serializable]
public struct SubtitleData
{
    [TextArea(2, 4)]
    public string text;       // 검은 화면에 띄울 자막 문구
    public float duration;    // 각 자막이 유지될 시간 (초)
}

public class EndingSequenceController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineVirtualCamera orbitCam;
    [SerializeField] private CinemachineVirtualCamera topCam;

    [Header("Optional Continuous Tower Shot")]
    [SerializeField] private TowerEndingCameraPath cameraPath;

    [Header("Fade")]
    [SerializeField] private Image fadeImage;

    [Header("Subtitles (Black Screen)")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private SubtitleData[] subtitles;

    [Header("End Text")]
    [SerializeField] private CanvasGroup endTextCanvasGroup;

    [Header("Timing")]
    [SerializeField] private float orbitDuration = 3f;
    [SerializeField] private float topViewDuration = 1f;
    [SerializeField] private float fadeDuration = 2.3f;
    [SerializeField] private float textFadeDelay = 0.2f;
    [SerializeField] private float textFadeDuration = 1.2f;

    [Header("Audio")]
    [SerializeField] private string endingBgmName = "EndingBGM"; // SoundManager에 등록한 엔딩 BGM 이름
    [SerializeField] private bool useFadeTransition = true;       // 기존 BGM 페이드아웃 후 전환 여부
    [SerializeField] private float bgmFadeOutDuration = 1.5f;     // BGM 페이드아웃 시간

    private enum SequenceState
    {
        TowerShot,
        FadeToBlack,
        Subtitles,
        EndTextFade
    }

    private SequenceState currentState = SequenceState.TowerShot;
    private float timer = 0f;
    private bool switchedToTop = false;
    private bool isFading = false;
    private bool startTextFade = false;
    private float textTimer = 0f;
    private bool continuousShot;
    private float shotTime;

    private int currentSubtitleIndex = 0;
    private float subtitleTimer = 0f;

    void Start()
    {
        if (orbitCam != null) orbitCam.Priority = 10;
        if (topCam != null) topCam.Priority = 5;

        continuousShot = cameraPath != null && cameraPath.Begin();
        if (continuousShot) orbitDuration = cameraPath.Duration;

        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }

        if (subtitleText != null)
        {
            subtitleText.text = "";
        }

        if (endTextCanvasGroup != null)
        {
            endTextCanvasGroup.alpha = 0f;
        }

        // 씬 시작 시 기존 BGM 정지/페이드 및 엔딩 BGM 재생
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(endingBgmName))
        {
            if (useFadeTransition)
            {
                StartCoroutine(FadeToEndingBGM());
            }
            else
            {
                SoundManager.Instance.StopBGM();
                SoundManager.Instance.PlayBGM(endingBgmName, true);
            }
        }
    }

    private IEnumerator FadeToEndingBGM()
    {
        AudioSource bgmSource = SoundManager.Instance.bgm2DSource;
        float startVolume = SoundManager.Instance.bgmVolume;

        if (bgmSource != null && bgmSource.isPlaying)
        {
            float t = 0f;
            while (t < bgmFadeOutDuration)
            {
                t += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t / bgmFadeOutDuration);
                yield return null;
            }
            bgmSource.Stop();
        }

        SoundManager.Instance.PlayBGM(endingBgmName, true);
        SoundManager.Instance.ApplyVolumeToSources();
    }

    void Update()
    {
        switch (currentState)
        {
            // 1. 공전 카메라 -> 탑 위 카메라 전환 (타워 시네마틱 연출)
            case SequenceState.TowerShot:
                if (continuousShot)
                {
                    shotTime += Time.deltaTime;
                    cameraPath.EvaluateTime(shotTime);
                }
                timer += Time.deltaTime;

                if (!switchedToTop && timer >= orbitDuration)
                {
                    switchedToTop = true;

                    if (!continuousShot)
                    {
                        if (orbitCam != null) orbitCam.Priority = 5;
                        if (topCam != null) topCam.Priority = 20;
                    }

                    Debug.Log(continuousShot ? "[Ending] Summit hold" : "[Ending] Top camera 전환");
                }

                // 2. 탑 위 카메라를 잠깐 보여준 뒤 페이드 시작
                if (switchedToTop && timer >= (orbitDuration + topViewDuration))
                {
                    currentState = SequenceState.FadeToBlack;
                    isFading = true;
                    timer = 0f;

                    Debug.Log("[Ending] Fade 시작");
                }
                break;

            // 3. 화면 페이드아웃
            case SequenceState.FadeToBlack:
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / fadeDuration);

                if (fadeImage != null)
                {
                    Color c = fadeImage.color;
                    c.a = t;
                    fadeImage.color = c;
                }

                // 완전히 검게 덮인 뒤 자막 단계 시작
                if (t >= 1f)
                {
                    currentState = SequenceState.Subtitles;
                    timer = 0f;
                    subtitleTimer = 0f;
                    currentSubtitleIndex = 0;
                }
                break;

            // 검은 화면 위에서 자막 출력
            case SequenceState.Subtitles:
                HandleSubtitlesOnBlackScreen();
                break;

            // 4. 텍스트 스으윽 등장
            case SequenceState.EndTextFade:
                if (!startTextFade)
                {
                    startTextFade = true;
                    textTimer = -textFadeDelay;
                }

                if (endTextCanvasGroup != null)
                {
                    textTimer += Time.deltaTime;
                    float textT = Mathf.Clamp01(textTimer / textFadeDuration);
                    endTextCanvasGroup.alpha = textT;
                }
                break;
        }
    }

    private void HandleSubtitlesOnBlackScreen()
    {
        if (subtitles == null || subtitles.Length == 0 || currentSubtitleIndex >= subtitles.Length)
        {
            // 모든 자막 출력이 끝나면 텍스트 지우고 EndText 단계로 전환
            if (subtitleText != null) subtitleText.text = "";
            currentState = SequenceState.EndTextFade;
            return;
        }

        subtitleTimer += Time.deltaTime;
        SubtitleData currentData = subtitles[currentSubtitleIndex];

        if (subtitleTimer < currentData.duration)
        {
            if (subtitleText != null)
            {
                subtitleText.text = currentData.text;
            }
        }
        else
        {
            currentSubtitleIndex++;
            subtitleTimer = 0f;
        }
    }
}