using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;

        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private float time;

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(new Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
                dropdown.options.Add(new Dropdown.OptionData(device));

            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);

            var index = PlayerPrefs.GetInt("user-mic-device-index");
            dropdown.SetValueWithoutNotify(index);
#endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }

        private void StartRecording()
        {
            isRecording = true;
            recordButton.enabled = false;

            var index = PlayerPrefs.GetInt("user-mic-device-index");
            clip = Microphone.Start(dropdown.options[index].text, false, duration, 44100);
        }

        private void EndRecording()
        {
            message.text = "Processing...";

            Microphone.End(null);
            byte[] wavData = SaveWav.Save(fileName, clip);

            StartCoroutine(SendToLocalServer(wavData));

            progressBar.fillAmount = 0;
            recordButton.enabled = true;
        }


        private IEnumerator SendToLocalServer(byte[] wavData)
        {
            WWWForm form = new WWWForm();
            form.AddBinaryData("audio", wavData, "audio.wav", "audio/wav");

            using (UnityWebRequest req = UnityWebRequest.Post("http://localhost:5000/transcribe", form))
            {
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    message.text = "❌ Error: " + req.error;
                }
                else
                {
                    // JSON → C# 구조체 변환
                    WhisperResponse response = JsonUtility.FromJson<WhisperResponse>(req.downloadHandler.text);

                    // 텍스트만 출력
                    message.text = response.text;
                }
            }
        }


        private void Update()
        {
            if (isRecording)
            {
                time += Time.deltaTime;
                progressBar.fillAmount = time / duration;

                if (time >= duration)
                {
                    time = 0;
                    isRecording = false;
                    EndRecording();
                }
            }
        }
    }
}
