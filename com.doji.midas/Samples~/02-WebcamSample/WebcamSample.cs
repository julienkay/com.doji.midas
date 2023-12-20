using UnityEngine;
using UnityEngine.UI;

namespace Midas.Samples {

    public class WebcamSample : MonoBehaviour {

        public ModelType Model;

        private Midas _midas;
        public Texture Texture;
        public RawImage InputImage;
        public RawImage OutputImage;

        public void Start() {
            _midas = new Midas();
            OutputImage.texture = _midas.Result;

            WebCamDevice[] devices = WebCamTexture.devices;
            for (int i = 0; i < devices.Length; i++) {
                var webcam = new WebCamTexture(devices[i].name);
                webcam.Play();
                Texture = webcam;
                InputImage.texture = webcam;
                InputImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)webcam.width / webcam.height;
                OutputImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)webcam.width / webcam.height;
            }
        }

        private void OnDestroy() {
            _midas?.Dispose();
        }

        private void Update() {
            if (Texture != null) {
                _midas.EstimateDepth(Texture);
            }
        }
    }
}