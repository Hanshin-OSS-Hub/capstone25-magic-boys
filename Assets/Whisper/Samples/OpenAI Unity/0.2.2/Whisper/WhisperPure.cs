using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Samples.Whisper;
using System;
using System.Security.Cryptography;

public class WhisperPure : MonoBehaviour
{
    [Header("Microphone Device Name")]
    public string microphoneDevice = ""; // Inspector에서 설정

    [Header("Recording")]
    public KeyCode recordKey = KeyCode.R;
    public int duration = 5;
    public string fileName = "output.wav";

    private AudioClip clip;
    private bool isRecording = false;
    private float time;

    private string lastText = "";

    void Start()
    {
        if (Microphone.devices.Length > 0)
            microphoneDevice = Microphone.devices[0];  // 자동 첫 번째
    }

    void Update()
    {
        

        if (Input.GetKeyDown(recordKey) && !isRecording)
        {
            StartRecording();
        }

        if (isRecording)
        {
            time += Time.deltaTime;
            if (time >= duration)
            {
                EndRecording();
            }
        }
    }

    void StartRecording()
    {
        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogWarning("마이크 디바이스가 비어 있음!");
            return;
        }

        isRecording = true;
        time = 0f;

        clip = Microphone.Start(microphoneDevice, false, duration, 44100);
        Debug.Log("[Whisper] Recording...");
    }

    void EndRecording()
    {
        isRecording = false;
        Microphone.End(null);

        Debug.Log("[Whisper] Sending to server...");

        byte[] wavData = SaveWav.Save(fileName, clip);
        StartCoroutine(SendToServer(wavData));
    }

    IEnumerator SendToServer(byte[] wavData)
    {
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest req = UnityWebRequest.Post("http://localhost:5000/transcribe", form))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                WhisperResponse res = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);
                lastText = res.text;
                Debug.Log("[Whisper] Result = " + lastText);
            }
            else
            {
                Debug.LogError("[Whisper] Error: " + req.error);
            }
        }
        
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
