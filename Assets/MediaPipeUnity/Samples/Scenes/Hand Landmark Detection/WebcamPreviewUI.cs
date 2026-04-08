using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class WebcamPreviewUI : MonoBehaviour
    {
        [SerializeField] private RawImage _rawImage;
        [SerializeField] private AspectRatioFitter _aspectFitter;
        [SerializeField] private bool _flipX = true;
        [SerializeField] private bool _flipY = false;

        private IEnumerator Start()
        {
            if (_rawImage == null)
            {
                _rawImage = GetComponent<RawImage>();
            }

            while (ImageSourceProvider.ImageSource == null)
            {
                yield return null;
            }

            var imageSource = ImageSourceProvider.ImageSource;

            while (!imageSource.isPrepared)
            {
                yield return null;
            }

            _rawImage.uvRect = new UnityEngine.Rect(
                _flipX ? 1f : 0f,
                _flipY ? 1f : 0f,
                _flipX ? -1f : 1f,
                _flipY ? -1f : 1f
            );

            while (true)
            {
                var tex = imageSource.GetCurrentTexture();

                if (tex != null)
                {
                    _rawImage.texture = tex;

                    if (_aspectFitter != null && tex.height > 0)
                    {
                        _aspectFitter.aspectRatio = (float)tex.width / tex.height;
                    }
                }

                yield return null;
            }
        }
    }
}