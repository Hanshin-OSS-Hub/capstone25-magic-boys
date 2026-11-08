using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Samples.Whisper;
using System;
using System.Security.Cryptography;

public class WhisperPure : MonoBehaviour
{
    [Header("Microphone Device Name")]
    public string microphoneDevice = "";

    [Header("Recording")]
    public KeyCode recordKey = KeyCode.R;
    public int duration = 5;
    public string fileName = "output.wav";

    private AudioClip clip;
    private bool isRecording = false;
    private float time;

    private string lastText = "";
    private UnityWebRequest pendingRequest;
    private int recordingNumber;
    private int transitionVersion;

    private void OnDisable()
    {
        transitionVersion++;
        if (isRecording) Microphone.End(microphoneDevice);
        isRecording = false;
        lastText = "";
        if (pendingRequest != null)
        {
            pendingRequest.Abort();
            pendingRequest.Dispose();
            pendingRequest = null;
        }
        Debug.Log("[VoiceFlow] 전환 중 녹음·대기 요청·이전 결과 정리");
    }

    public event Action OnRecordEnd;
    public event Action OnUploadStart;
    public event Action<string> OnResponseReceived;

    void Start()
{
        if (Microphone.devices.Length > 0)
            microphoneDevice = Microphone.devices[0];  
    }

    void Update()
    {
        if (Input.GetKeyDown(recordKey) && !isRecording)
        {
            StartRecording();
        }

        if (isRecording)
        {
            if (Input.GetKeyUp(recordKey))
            {
                EndRecording();
            }
            else
            {
                time += Time.deltaTime;
                if (time >= duration)
                {
                    EndRecording();
                }
            }
        }
    }

    public void StartRecording()
    {
        if (isRecording || pendingRequest != null) return;
        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogWarning("Microphone device not found!");
            return;
        }

        isRecording = true;
        recordingNumber++;
        time = 0f;

        //clip = Microphone.Start(microphoneDevice, false, duration, 44100);
        clip = Microphone.Start(microphoneDevice, false, duration, 16000);
        Debug.Log("[Whisper] Recording...");
        Debug.Log($"[VoiceFlow] 녹음 #{recordingNumber} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
    }

    public void EndRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        // 녹음 버퍼 전체(duration초)가 아니라 실제로 녹음된 길이만 보냄 → 무음이 줄어 인식이 빨라짐
        int recordedSamples = Microphone.GetPosition(microphoneDevice);
        Microphone.End(microphoneDevice);

        OnRecordEnd?.Invoke();
        Debug.Log("[Whisper] Sending to server...");

        AudioClip toSend = TrimToRecorded(clip, recordedSamples);
        byte[] wavData = SaveWav.Save(fileName, toSend);
        StartCoroutine(SendToServer(wavData, recordingNumber, transitionVersion));
    }

    IEnumerator SendToServer(byte[] wavData, int number, int version)
    {
        OnUploadStart?.Invoke();
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post("http://localhost:5000/transcribe", form))
        {
            pendingRequest = req;
            yield return req.SendWebRequest();

            if (version != transitionVersion) yield break;
            pendingRequest = null;

            if (req.result == UnityWebRequest.Result.Success)
            {
                WhisperResponse res = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
                lastText = res.text;
                Debug.Log("[Whisper] Result = " + lastText);
                Debug.Log($"[VoiceFlow] 응답 #{number} scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} text='{lastText}'");
                OnResponseReceived?.Invoke(lastText);
            }
            else
            {
                Debug.LogError("[Whisper] Error: " + req.error);
                OnResponseReceived?.Invoke("Error: " + req.error);
            }
}
        
    }

    private static AudioClip TrimToRecorded(AudioClip source, int samples)
    {
        if (source == null || samples <= 0 || samples >= source.samples) return source;
        // 너무 짧으면 인식이 불안정하므로 최소 0.5초
        samples = Mathf.Min(source.samples, Mathf.Max(samples, source.frequency / 2));
        var data = new float[samples * source.channels];
        source.GetData(data, 0);
        var trimmed = AudioClip.Create("recorded", samples, source.channels, source.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    public string GetText()
    {
        return lastText;
    }

    public void ClearText()
    {
        lastText = "";
    }

}

public class WhisperResponse
{
    public string text;
}
