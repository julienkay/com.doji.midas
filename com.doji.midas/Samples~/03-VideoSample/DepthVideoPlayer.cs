using UnityEngine;
using UnityEngine.Video;

namespace Doji.AI.Depth.Samples {

    public class DepthVideoPlayer : MonoBehaviour {

        public ModelType Model;
        private Midas _midas;

        public DepthPointRenderer PointRendererPrefab;
        private DepthPointRenderer _pointRenderer;

        private void Start() {
            _midas = new Midas(Model) {
                NormalizeDepth = false,
            };

            _pointRenderer = Instantiate(PointRendererPrefab);

            _pointRenderer.Source = GetComponent<VideoPlayer>().targetTexture;
            _pointRenderer.Depth = _midas.Result;
        }

        private void OnDestroy() {
            _midas?.Dispose();
        }

        private void Update() {
            if (_midas != null) {
                _midas.EstimateDepth(_pointRenderer.Source);
                var (min, max) = _midas.GetMinMax();
                if (_pointRenderer != null) {
                    _pointRenderer.MinPred = min;
                    _pointRenderer.MaxPred = max;
                }
            }
        }
    }
}