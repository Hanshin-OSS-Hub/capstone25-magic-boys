using UnityEngine;
using System;

public class WhisperLatencyTester : MonoBehaviour
{
    public WhisperPure whisper;

    private float recordEndTime;
    private float uploadStartTime;
    private float responseReceiveTime;

    void Start()
    {
        if (whisper == null)
            whisper = GetComponent<WhisperPure>();

        if (whisper == null)
        {
            Debug.LogError("[WhisperLatencyTester] WhisperPure reference not found!");
            return;
        }

        // Subscribe to events added in WhisperPure
        whisper.OnRecordEnd += HandleRecordEnd;
        whisper.OnUploadStart += HandleUploadStart;
        whisper.OnResponseReceived += HandleResponseReceived;
        
        Debug.Log("[WhisperLatencyTester] Latency measurement started.");
    }

    private void HandleRecordEnd()
    {
        recordEndTime = Time.realtimeSinceStartup;
        Debug.Log($"[Latency] Record End at: {recordEndTime:F4}s");
    }

    private void HandleUploadStart()
    {
        uploadStartTime = Time.realtimeSinceStartup;
        Debug.Log($"[Latency] Upload Start at: {uploadStartTime:F4}s");
    }

    private void HandleResponseReceived(string result)
    {
        responseReceiveTime = Time.realtimeSinceStartup;
        
        float processingLatency = responseReceiveTime - uploadStartTime;
        float endToEndLatency = responseReceiveTime - recordEndTime;

        Debug.Log($"[Latency Result] Result Text: {result}");
        Debug.Log($"[Latency Result] (Response - UploadStart): {processingLatency:F4}s (Server Processing + Network)");
        Debug.Log($"[Latency Result] (Response - RecordEnd): {endToEndLatency:F4}s (Total Turnaround)");
    }

    private void OnDestroy()
    {
        if (whisper != null)
        {
            whisper.OnRecordEnd -= HandleRecordEnd;
            whisper.OnUploadStart -= HandleUploadStart;
            whisper.OnResponseReceived -= HandleResponseReceived;
        }
    }
}
