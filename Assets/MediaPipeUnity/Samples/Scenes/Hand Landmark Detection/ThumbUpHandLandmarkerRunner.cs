using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using TaskNormalizedLandmark = Mediapipe.Tasks.Components.Containers.NormalizedLandmark;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class ThumbUpHandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;
        [SerializeField] private MagicAttackk _magicAttackk;

        [Header("Thumb Up Detection")]
        [SerializeField] private float _thumbRaiseMinDelta = 0.03f;
        [SerializeField] private float _fingerFoldMinDelta = 0.01f;
        [SerializeField] private float _triggerCooldown = 0.8f;
        [SerializeField] private bool _logWhenTriggered = true;
        [SerializeField] private UnityEvent _onThumbUpDetected;

        [Header("Debug View")]
        [SerializeField] private bool _showHandAnnotation = false;

        private Experimental.TextureFramePool _textureFramePool;

        private readonly object _thumbStateLock = new object();
        private bool _latestThumbUpState;
        private bool _hasThumbStateUpdate;

        private bool _wasThumbUp;
        private float _lastTriggerTime = -999f;

        public bool IsThumbUpNow { get; private set; }

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        private void Update()
        {
            bool shouldProcess = false;
            bool thumbUpState = false;

            lock (_thumbStateLock)
            {
                if (_hasThumbStateUpdate)
                {
                    thumbUpState = _latestThumbUpState;
                    _hasThumbStateUpdate = false;
                    shouldProcess = true;
                }
            }

            if (!shouldProcess)
            {
                return;
            }

            IsThumbUpNow = thumbUpState;

            if (thumbUpState && !_wasThumbUp && Time.time - _lastTriggerTime >= _triggerCooldown)
            {
                _lastTriggerTime = Time.time;

                Telekinesis();

                if (_logWhenTriggered)
                {
                    Debug.Log("염동력 스킬 발동");
                }

                _onThumbUpDetected?.Invoke();
            }

            _wasThumbUp = thumbUpState;
        }

        private void Telekinesis()
        {
            if (_magicAttackk != null)
            {
                _magicAttackk.Telekinesis();
            }
            else
            {
                Debug.LogWarning("MagicAttackk가 연결되지 않았습니다.");
            }
        }

        private void DrawResultNow(HandLandmarkerResult result)
        {
            if (_showHandAnnotation && _handLandmarkerResultAnnotationController != null)
            {
                _handLandmarkerResultAnnotationController.DrawNow(result);
            }
        }

        private void DrawResultLater(HandLandmarkerResult result)
        {
            if (_showHandAnnotation && _handLandmarkerResultAnnotationController != null)
            {
                _handLandmarkerResultAnnotationController.DrawLater(result);
            }
        }

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
            ResetThumbState();
        }

        protected override IEnumerator Run()
        {
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Image Read Mode = {config.ImageReadMode}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumHands = {config.NumHands}");
            Debug.Log($"MinHandDetectionConfidence = {config.MinHandDetectionConfidence}");
            Debug.Log($"MinHandPresenceConfidence = {config.MinHandPresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetHandLandmarkerOptions(
                config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null
            );

            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource, exiting...");
                yield break;
            }

            _textureFramePool = new Experimental.TextureFramePool(
                imageSource.textureWidth,
                imageSource.textureHeight,
                TextureFormat.RGBA32,
                10
            );

            screen.Initialize(imageSource);
            SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);

            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally;
            var flipVertically = transformationOptions.flipVertically;
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(
                rotationDegrees: (int)transformationOptions.rotationAngle
            );

            AsyncGPUReadbackRequest req = default;
            var waitUntilReqDone = new WaitUntil(() => req.done);
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var result = HandLandmarkerResult.Alloc(options.numHands);

            var canUseGpuImage =
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 &&
                GpuManager.GpuResources != null;

            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                Image image;

                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU:
                        if (!canUseGpuImage)
                        {
                            throw new Exception("ImageReadMode.GPU is not supported");
                        }
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildGPUImage(glContext);
                        yield return waitForEndOfFrame;
                        break;

                    case ImageReadMode.CPU:
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;

                    case ImageReadMode.CPUAsync:
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        yield return waitUntilReqDone;

                        if (req.hasError)
                        {
                            Debug.LogWarning("Failed to read texture from the image source");
                            continue;
                        }

                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                        {
                            DrawResultNow(result);
                            UpdateThumbStateFromResult(result);
                        }
                        else
                        {
                            DrawResultNow(default);
                            ResetThumbState();
                        }
                        break;

                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                        {
                            DrawResultNow(result);
                            UpdateThumbStateFromResult(result);
                        }
                        else
                        {
                            DrawResultNow(default);
                            ResetThumbState();
                        }
                        break;

                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            DrawResultLater(result);
            UpdateThumbStateFromResult(result);
        }

        private void UpdateThumbStateFromResult(HandLandmarkerResult result)
        {
            bool isThumbUp = false;

            if (result.handLandmarks != null && result.handLandmarks.Count > 0)
            {
                var landmarks = result.handLandmarks[0].landmarks;

                if (landmarks != null && landmarks.Count >= 21)
                {
                    isThumbUp = IsThumbUp(landmarks);
                }
            }

            lock (_thumbStateLock)
            {
                _latestThumbUpState = isThumbUp;
                _hasThumbStateUpdate = true;
            }
        }

        private bool IsThumbUp(IReadOnlyList<TaskNormalizedLandmark> landmarks)
        {
            var wrist = landmarks[0];

            var thumbMcp = landmarks[2];
            var thumbIp = landmarks[3];
            var thumbTip = landmarks[4];

            var indexMcp = landmarks[5];
            var indexPip = landmarks[6];
            var indexTip = landmarks[8];

            var middlePip = landmarks[10];
            var middleTip = landmarks[12];

            var ringPip = landmarks[14];
            var ringTip = landmarks[16];

            var pinkyPip = landmarks[18];
            var pinkyTip = landmarks[20];

            float wristY = GetLandmarkY(wrist);

            float thumbMcpY = GetLandmarkY(thumbMcp);
            float thumbIpY = GetLandmarkY(thumbIp);
            float thumbTipY = GetLandmarkY(thumbTip);

            float indexMcpY = GetLandmarkY(indexMcp);
            float indexPipY = GetLandmarkY(indexPip);
            float indexTipY = GetLandmarkY(indexTip);

            float middlePipY = GetLandmarkY(middlePip);
            float middleTipY = GetLandmarkY(middleTip);

            float ringPipY = GetLandmarkY(ringPip);
            float ringTipY = GetLandmarkY(ringTip);

            float pinkyPipY = GetLandmarkY(pinkyPip);
            float pinkyTipY = GetLandmarkY(pinkyTip);

            bool thumbRaised =
                thumbTipY < thumbIpY &&
                thumbIpY < thumbMcpY &&
                thumbTipY < indexMcpY &&
                (thumbMcpY - thumbTipY) > _thumbRaiseMinDelta &&
                (wristY - thumbTipY) > (_thumbRaiseMinDelta * 0.5f);

            bool indexFolded = (indexTipY - indexPipY) > _fingerFoldMinDelta;
            bool middleFolded = (middleTipY - middlePipY) > _fingerFoldMinDelta;
            bool ringFolded = (ringTipY - ringPipY) > _fingerFoldMinDelta;
            bool pinkyFolded = (pinkyTipY - pinkyPipY) > _fingerFoldMinDelta;

            return thumbRaised && indexFolded && middleFolded && ringFolded && pinkyFolded;
        }

        private float GetLandmarkY(TaskNormalizedLandmark landmark)
        {
            return GetCoordinateValue(landmark, "y", "Y");
        }

        private float GetCoordinateValue(object landmark, string lowerName, string upperName)
        {
            if (landmark == null)
            {
                return 0f;
            }

            var type = landmark.GetType();

            var lowerProp = type.GetProperty(lowerName, BindingFlags.Instance | BindingFlags.Public);
            if (lowerProp != null)
            {
                return ConvertToFloat(lowerProp.GetValue(landmark));
            }

            var upperProp = type.GetProperty(upperName, BindingFlags.Instance | BindingFlags.Public);
            if (upperProp != null)
            {
                return ConvertToFloat(upperProp.GetValue(landmark));
            }

            var lowerField = type.GetField(lowerName, BindingFlags.Instance | BindingFlags.Public);
            if (lowerField != null)
            {
                return ConvertToFloat(lowerField.GetValue(landmark));
            }

            var upperField = type.GetField(upperName, BindingFlags.Instance | BindingFlags.Public);
            if (upperField != null)
            {
                return ConvertToFloat(upperField.GetValue(landmark));
            }

            Debug.LogError($"랜드마크 좌표 '{lowerName}/{upperName}' 를 찾을 수 없습니다. 타입: {type.FullName}");
            return 0f;
        }

        private float ConvertToFloat(object value)
        {
            if (value == null)
            {
                return 0f;
            }

            if (value is float f) return f;
            if (value is double d) return (float)d;
            if (value is int i) return i;

            return Convert.ToSingle(value);
        }

        private void ResetThumbState()
        {
            IsThumbUpNow = false;
            _wasThumbUp = false;

            lock (_thumbStateLock)
            {
                _latestThumbUpState = false;
                _hasThumbStateUpdate = false;
            }
        }
    }
}