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
        /// Normalizing maps these values to the (0, 1) range, which is mostly
        /// useful for visualization. To
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
            _ops = WorkerFactory.CreateOps(BackendType.GPUCompute, _allocator);

            int width = _model.inputs[0].shape[2].value;
            int height = _model.inputs[0].shape[3].value;
            Result = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat) {
                name = $"{_modelAsset.name}_output"
            };
        }

        public void EstimateDepth(Texture source) {
            using (var tensor = TextureConverter.ToTensor(source, source.width, source.height, 3)) {
                _worker.Execute(tensor);
            }

            Tensor output = _worker.PeekOutput();

            int height = output.shape[1];
            int width = output.shape[2];

            // normalize
            if (NormalizeDepth) {
                output = Normalize(output);
            }

            TextureConverter.RenderToTexture(output.ShallowReshape(new TensorShape(1, 1, height, width)) as TensorFloat, Result);
        }

        /// <summary>
        /// Normalize on-device using Tensor Ops.
        /// </summary>
        private Tensor Normalize(Tensor output) {
            TensorFloat minT = _ops.ReduceMin(output as TensorFloat, null, false);
            TensorFloat maxT = _ops.ReduceMax(output as TensorFloat, null, false);

            TensorFloat a = _ops.Sub(output as TensorFloat, minT);
            TensorFloat b = _ops.Sub(maxT, minT);
            Tensor normalized = _ops.Div(a, b);
            return normalized;
        }

        private void Dispose() {
            _ops?.Dispose();
            _allocator?.Dispose();
            _worker?.Dispose();
        }

        void IDisposable.Dispose() {
            Dispose();
        }
    }
}