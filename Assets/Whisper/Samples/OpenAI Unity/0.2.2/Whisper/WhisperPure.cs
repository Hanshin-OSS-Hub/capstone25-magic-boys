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
        if (isRecording) return;
        if (string.IsNullOrEmpty(microphoneDevice))
        {
            Debug.LogWarning("Microphone device not found!");
            return;
        }

        isRecording = true;
        time = 0f;

        //clip = Microphone.Start(microphoneDevice, false, duration, 44100);
        clip = Microphone.Start(microphoneDevice, false, duration, 16000);
        Debug.Log("[Whisper] Recording...");
    }

    public void EndRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        Microphone.End(microphoneDevice);

        OnRecordEnd?.Invoke();
        Debug.Log("[Whisper] Sending to server...");

        byte[] wavData = SaveWav.Save(fileName, clip);
        StartCoroutine(SendToServer(wavData));
    }

    IEnumerator SendToServer(byte[] wavData)
    {
        OnUploadStart?.Invoke();
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
                OnResponseReceived?.Invoke(lastText);
            }
            else
            {
                Debug.LogError("[Whisper] Error: " + req.error);
                OnResponseReceived?.Invoke("Error: " + req.error);
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
