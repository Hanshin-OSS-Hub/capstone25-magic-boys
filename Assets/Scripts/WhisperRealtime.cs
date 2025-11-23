using UnityEngine;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System;

public class WhisperHttp : MonoBehaviour
{
    private AudioClip mic;
    private int sampleRate = 16000;
    private HttpClient client = new HttpClient();

    async void Start()
    {
        Debug.Log("Starting Whisper HTTP Client...");

        mic = Microphone.Start(null, true, 2, sampleRate);

        InvokeRepeating(nameof(SendAudioToWhisper), 1f, 1f);
    }

    async void SendAudioToWhisper()
    {
        int samples = sampleRate;
        float[] data = new float[samples];
        int micPos = Microphone.GetPosition(null);

        mic.GetData(data, Mathf.Max(0, micPos - samples));

        // Convert float audio to 16-bit PCM
        byte[] wavData = new byte[data.Length * 2];
        int res = 0;

        for (int i = 0; i < data.Length; i++)
        {
            short sample = (short)(data[i] * short.MaxValue);
            wavData[res++] = (byte)(sample & 0xff);
            wavData[res++] = (byte)((sample >> 8) & 0xff);
        }

        var content = new ByteArrayContent(wavData);
        var result = await client.PostAsync("http://127.0.0.1:8080/inference", content);
        string response = await result.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(response))
        {
            Debug.Log("Recognized: " + response);
        }
    }

    private void OnApplicationQuit()
    {
        Microphone.End(null);
    }
}
