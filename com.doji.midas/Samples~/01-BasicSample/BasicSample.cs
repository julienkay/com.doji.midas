using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Doji.AI.Depth.Samples {

    public class BasicSample : MonoBehaviour {

        public ModelType Model;

        public Texture2D SampleImage;
        private Midas _midas;

        public RawImage InputImage;
        public RawImage OutputImage;

        public GameObject ExportButton;

        public void Start () {
            _midas = new Midas(Model);
        }

        private void OnDestroy() {
            _midas?.Dispose();
        }

        public void EstimateDepth() {
            if (SampleImage == null) {
                Debug.LogError("No input image found.");
                return;
            }
            var result = _midas.EstimateDepth(SampleImage);
            OutputImage.texture = result;
            ExportButton.SetActive(true);
        }

        public void ExportDepth() {
            RenderTexture result = OutputImage.texture as RenderTexture;
            if (result == null) {
                Debug.LogError("No depth estimation computed yet.");
                return;
            }

#if UNITY_EDITOR
            string defaultName = $"{result.name}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save grayscale depth map as .png", "", defaultName, "png");
            if (string.IsNullOrEmpty(path)) {
                return;
            }
            SaveAs(result, path);
#endif
        }

        private void SaveAs(RenderTexture texture, string filePath) {
            int width = texture.width;
            int height = texture.height;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RFloat, false);
            RenderTexture.active = texture;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            File.WriteAllBytes(filePath, tex.EncodeToPNG());
            System.Diagnostics.Process.Start(Path.GetDirectoryName(filePath));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (InputImage != null) {
                InputImage.texture = SampleImage;
                if (SampleImage != null) {
                    float ratio = ((float)SampleImage.width) / SampleImage.height;
                    InputImage.GetComponent<AspectRatioFitter>().aspectRatio = ratio;
                    OutputImage.GetComponent<AspectRatioFitter>().aspectRatio = ratio;
                }
            }
            if (_midas != null) {
                if (_midas.ModelType != Model) {
                    _midas.ModelType = Model;
                    OutputImage.texture = null;
                    ExportButton.SetActive(false);
                }
            }
        }
#endif
    }
}