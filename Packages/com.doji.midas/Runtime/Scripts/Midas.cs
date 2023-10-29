using System;
using Unity.Sentis;
using UnityEngine;

namespace Midas {

    /// <summary>
    /// A class that allows to run Midas models
    /// to do monocular depth estimation.
    /// </summary>
    public class Midas : IDisposable {

        /// <summary>
        /// A RenderTexture containing the estimated depth.
        /// </summary>
        public RenderTexture Result { get; set; }

        /// <summary>
        /// Which <see cref="BackendType"/> to run the model with.
        /// </summary>
        public BackendType Backend {
            get => _backend;
            set {
                if (_backend != value) {
                    Dispose();
                    _backend = value;
                    InitializeNetwork();
                }
            }
        }
        private BackendType _backend = BackendType.GPUCompute;

        /// <summary>
        /// Whether to normalize the estimated depth.
        /// </summary>
        /// <remarks>
        /// MiDaS predicts depth values as inverse relative depth.
        /// (small values for far away objects, large values for near objects)
        /// If NormalizeDepth is enabled, these values are mapped to the (0, 1) range,
        /// which is mostly useful for visualization.
        /// </remarks>
        public bool NormalizeDepth { get; set; } = true;

        /// <summary>
        /// The reference to a ModelAsset.
        /// </summary>
        private ModelAsset _modelAsset;

        /// <summary>
        /// The runtime model.
        /// </summary>
        private Model _model;

        private IWorker _worker;
        private ITensorAllocator _allocator;
        private Ops _ops;

        /// <summary>
        /// the (possibly resized) input texture;
        /// </summary>
        private RenderTexture _resizedInput;

        /// <summary>
        /// Initializes a new instance of MiDaS.
        /// </summary>
        /// <param name="model">the reference to a MiDaS ONNX model</param>
        public Midas(ModelAsset model) {
            _modelAsset = model;
            InitializeNetwork();
        }

        private void InitializeNetwork() {
            if (_modelAsset == null) {
                throw new ArgumentException("Model was null", "model");
            }

            _model = ModelLoader.Load(_modelAsset);
            _worker = WorkerFactory.CreateWorker(Backend, _model);
            _allocator = new TensorCachingAllocator();
            _ops = WorkerFactory.CreateOps(Backend, _allocator);

            int width = _model.inputs[0].shape[2].value;
            int height = _model.inputs[0].shape[3].value;

            _resizedInput = new RenderTexture(width, height, 0);
            Result = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat) {
                name = $"{_modelAsset.name}_output"
            };
        }

        public void EstimateDepth(Texture input, bool autoResize = true) {
            // resize
            if (autoResize) {
                Graphics.Blit(input, _resizedInput);
                input = _resizedInput;
            }

            using (var tensor = TextureConverter.ToTensor(input, input.width, input.height, 3)) {
                _worker.Execute(tensor);
            }

            using (Tensor predictedDepth = _worker.PeekOutput()) {
                int height = predictedDepth.shape[1];
                int width = predictedDepth.shape[2];

                // normalize
                if (NormalizeDepth) {
                    Normalize(predictedDepth, out Tensor normalized);
                    TextureConverter.RenderToTexture(normalized.ShallowReshape(new TensorShape(1, 1, height, width)) as TensorFloat, Result);
                } else {
                    TextureConverter.RenderToTexture(predictedDepth.ShallowReshape(new TensorShape(1, 1, height, width)) as TensorFloat, Result);
                }
            }
        }

        /// <summary>
        /// Normalize on-device using Tensor Ops.
        /// </summary>
        private void Normalize(Tensor depth, out Tensor normalized) {
            TensorFloat minT = _ops.ReduceMin(depth as TensorFloat, null, false);
            TensorFloat maxT = _ops.ReduceMax(depth as TensorFloat, null, false);

            TensorFloat a = _ops.Sub(depth as TensorFloat, minT);
            TensorFloat b = _ops.Sub(maxT, minT);
            normalized = _ops.Div(a, b);
        }

        public void Dispose() {
            _worker?.Dispose();
            _allocator?.Dispose();
            _ops?.Dispose();
        }
    }
}