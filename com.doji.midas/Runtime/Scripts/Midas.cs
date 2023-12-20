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
        /// Which of the MiDaS models to run.
        /// </summary>
        public ModelType ModelType {
            get => _modelType;
            set {
                if (_modelType != value) {
                    Dispose();
                    _modelType = value;
                    InitializeNetwork();
                }
            }
        }
        private ModelType _modelType;


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
        /// A RenderTexture containing the estimated depth.
        /// </summary>
        private RenderTexture _result { get; set; }

        /// <summary>
        /// The runtime model.
        /// </summary>
        private Model _model;

        private IWorker _worker;
        private ITensorAllocator _allocator;
        private Ops _ops;

        /// <summary>
        /// the name of the Midas model
        /// </summary>
        private string _name;

        /// <summary>
        /// the (possibly resized) input texture;
        /// </summary>
        private RenderTexture _resizedInput;

#if UNITY_EDITOR
        public static event Action<ModelType> OnModelRequested = (x) => {};
#endif
        /// <summary>
        /// Initializes a new instance of MiDaS.
        /// </summary>
        public Midas(ModelType modelType = ModelType.midas_v21_small_256) {
            _modelType = modelType;
            InitializeNetwork();
        }

        internal Midas(ModelAsset modelAsset) {
            _modelType = ModelType.Unknown;
            InitializeNetwork(modelAsset);
        }

        private void InitializeNetwork() {
            if (_modelType == ModelType.Unknown) {
                throw new InvalidOperationException("Not a valid model type.");
            }

#if UNITY_EDITOR
            OnModelRequested?.Invoke(_modelType);
#endif

            ModelAsset modelAsset = Resources.Load<ModelAsset>(_modelType.ResourcePath());

            if (modelAsset == null) {
                throw new Exception($"Could not load model '{ModelType}'. Make sure the model exists in your project.");
            }

            InitializeNetwork(modelAsset);
        }

        private void InitializeNetwork(ModelAsset modelAsset) {
            if (modelAsset == null) {
                throw new ArgumentException("ModelAsset was null", "modelAsset");
            }

            _model = ModelLoader.Load(modelAsset);
            _worker = WorkerFactory.CreateWorker(Backend, _model);
            _allocator = new TensorCachingAllocator();

            int width = _model.inputs[0].shape[2].value;
            int height = _model.inputs[0].shape[3].value;
            _name = modelAsset.name;

            _resizedInput = new RenderTexture(width, height, 0) {
                autoGenerateMips = false,
            };

            _result = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat) {
                name = $"depth_{_name}"
            };
            Resources.UnloadAsset(modelAsset);
        }

        public RenderTexture EstimateDepth(Texture input, bool autoResize = true) {
            // resize
            if (autoResize) {
                Resize(ref input);
            }

            using (var tensor = TextureConverter.ToTensor(_resizedInput, _resizedInput.width, _resizedInput.height, 3)) {
                _worker.Execute(tensor);
            }

            Tensor predictedDepth = _worker.PeekOutput();
            int height = predictedDepth.shape[1];
            int width = predictedDepth.shape[2];

            // normalize
            if (NormalizeDepth) {
                Tensor normalized = Normalize(predictedDepth);
                TextureConverter.RenderToTexture(normalized.ShallowReshape(new TensorShape(1, 1, height, width)) as TensorFloat, _result);
                _ops.Dispose();
            } else {
                TextureConverter.RenderToTexture(predictedDepth.ShallowReshape(new TensorShape(1, 1, height, width)) as TensorFloat, _result);
            }

            _result.name = $"{input.name}_depth_{_name}";
            return _result;
        }

        private void Resize(ref Texture input) {
            Graphics.Blit(input, _resizedInput);
            _resizedInput.name = input.name;
            input = _resizedInput;
        }

        /// <summary>
        /// Normalize on-device using Tensor Ops.
        /// </summary>
        private Tensor Normalize(Tensor depth) {
            _ops = WorkerFactory.CreateOps(Backend, _allocator);
            TensorFloat minT = _ops.ReduceMin(depth as TensorFloat, null, false);
            TensorFloat maxT = _ops.ReduceMax(depth as TensorFloat, null, false);

            TensorFloat a = _ops.Sub(depth as TensorFloat, minT);
            TensorFloat b = _ops.Sub(maxT, minT);
            Tensor normalized = _ops.Div(a, b);

            return normalized;
        }

        public void Dispose() {
            _worker?.Dispose();
            _allocator?.Dispose();
            _ops?.Dispose();
            if (_resizedInput!= null) {
                _resizedInput.Release();
                UnityEngine.Object.Destroy(_resizedInput);
            }
        }
    }
}