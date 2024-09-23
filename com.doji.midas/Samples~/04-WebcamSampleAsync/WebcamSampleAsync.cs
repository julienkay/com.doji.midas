using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Doji.AI.Depth.Samples {

    public class WebcamSampleAsync : MonoBehaviour {

        public ModelType Model;

        private Midas _midas;
        public Texture WebCam;

        public RawImage Image;
        public Dropdown Dropdown;

        public DepthPointRenderer PointRendererPrefab;
        private DepthPointRenderer _pointRenderer;

        private bool _paused;

        public void Start() {
            _midas = new Midas(Model) {
                NormalizeDepth = true,
            };
            Image.texture = _midas.Result;

            if (WebCamTexture.devices == null || WebCamTexture.devices.FirstOrDefault().Equals(default(WebCamDevice))) {
                Debug.LogError("No WebCamDevice found.");
                return;
            }
            var device = WebCamTexture.devices.FirstOrDefault();

            Debug.Log($"Selected camera device: { device.name}");
            WebCamTexture webcam = new WebCamTexture(device.name);
            WebCam = webcam;
            webcam.Play();
            Image.GetComponent<AspectRatioFitter>().aspectRatio = (float)webcam.width / webcam.height;

            _pointRenderer = Instantiate(PointRendererPrefab);
            _pointRenderer.Source = webcam;
            _pointRenderer.Depth = _midas.Result;
            _pointRenderer.enabled = false;

            Dropdown.options = new List<Dropdown.OptionData>() {
                new Dropdown.OptionData("Source"),
                new Dropdown.OptionData("Depth"),
                new Dropdown.OptionData("3D Points"),
            };
            Dropdown.onValueChanged.AddListener(OnModeChanged);
            Dropdown.value = 1;

            StartCoroutine(UpdateCoroutine());
        }

        private IEnumerator UpdateCoroutine() {
            while (true) {
                if (WebCam != null && _midas != null && !_paused) {
                    yield return _midas._EstimateDepth(WebCam);
                    if (_pointRenderer != null && _pointRenderer.enabled) {
                        var (min, max) = _midas.GetMinMax();
                        _pointRenderer.MinPred = min;
                        _pointRenderer.MaxPred = max;
                    }
                }
            }
        }

        private void OnModeChanged(int mode) {
            switch (mode) {
                case 0: // source
                    Image.enabled = true;
                    Image.texture = WebCam;
                    _pointRenderer.enabled = true;
                    _paused = true;
                    break;
                case 1: // depth map
                    Image.enabled = true;
                    Image.texture = _midas.Result;
                    _pointRenderer.enabled = false;
                    _midas.NormalizeDepth = true;
                    _paused = false;
                    break;
                case 2: // points
                    Image.enabled = false;
                    _pointRenderer.enabled = true;
                    _midas.NormalizeDepth = false;
                    _paused = false;
                    break;
                default:
                    break;
            }
        }

        private void OnDestroy() {
            StopAllCoroutines();
            _midas?.Dispose();
        }
    }
}