using System;
using System.Collections;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

namespace Doji.AI.Depth {

    public delegate void EstimationFinished(Texture estimatedDepth);

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
        /// Whether to automatically resize the texture passed to <see cref="EstimateDepth"/>
        /// to the resolution that the specific neural network expects.
        /// </summary>
        public bool AutoResize { get; set; } = true;

        /// <summary>
        /// A RenderTexture that contains the estimated depth.
        /// </summary>
        public RenderTexture Result { get; set; }

        /// <summary>
        /// The runtime model.
        /// </summary>
        private Model _model;

        private Worker _worker;
        private Normalization _normalization;

        /// <summary>
        /// the name of the Midas model
        /// </summary>
        private string _name;

        /// <summary>
        /// the (possibly resized) input texture;
        /// </summary>
        private RenderTexture _resizedInput;

        /// <summary>
        /// Caches the last predicted output
        /// </summary>
        private Tensor<float> _predictedDepth;

#if UNITY_EDITOR
        public static event Action<ModelType> OnModelRequested = (x) => {};
#endif

        private bool _estimationRunning = false;

        /// <summary>
        /// Initializes a new instance of MiDaS.
        /// </summary>
        public Midas(ModelType modelType = ModelType.midas_v21_small_256) {
            _modelType = modelType;
            InitializeNetwork();
        }

        public Midas(ModelAsset modelAsset) {
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
                throw new ArgumentException("ModelAsset was null", nameof(modelAsset));
            }

            _model = ModelLoader.Load(modelAsset);
            _name = modelAsset.name;
            Resources.UnloadAsset(modelAsset);
            _worker = new Worker(_model, Backend);

            int width = _model.inputs[0].shape.Get(2);
            int height = _model.inputs[0].shape.Get(3);
            _normalization = new Normalization(_model.inputs[0].shape, Backend);

            InitInputTexture(width, height);
            InitOutputTexture(width, height);
        }

        private void InitInputTexture(int width, int height) {
            if (_resizedInput == null || _resizedInput.width != width || _resizedInput.height != height) {
                _resizedInput = new RenderTexture(width, height, 0) {
                    autoGenerateMips = false,
                };
            }
        }
        
        private void InitOutputTexture(int width, int height) {
            if (Result != null) {
                if (Result.width != width || Result.height != height) {
                    Result.Release();
                    Result.width = width;
                    Result.height = height;
                    Result.Create();
                }
            } else {
                Result = new RenderTexture(width, height, 0, RenderTextureFormat.RFloat);
            }
            Result.name = $"depth_{_name}";
        }

        /// <summary>
        /// Run depth estimation on the given <paramref name="input"/> texture.
        /// </summary>
        /// <param name="input"></param>
        public void EstimateDepth(Texture input) {
            if (CheckIsRunning()) {
                return;
            }

            if (AutoResize) {
                Resize(ref input);
            }

            using var tensor = TextureConverter.ToTensor(_resizedInput, _resizedInput.width, _resizedInput.height, 3);
            _worker.Schedule(tensor);

            _predictedDepth = _worker.PeekOutput() as Tensor<float>;
            int height = _predictedDepth.shape[1];
            int width = _predictedDepth.shape[2];
            _predictedDepth.Reshape(_predictedDepth.shape.Unsqueeze(1));

            // normalize
            if (NormalizeDepth) {
                _predictedDepth = Normalize(_predictedDepth);
            }
            TextureConverter.RenderToTexture(_predictedDepth, Result);

            Result.name = $"{input.name}_depth_{_name}";
            _estimationRunning = false;
        }

        /// <summary>
        /// Runs depth estimation, but distributes the computation over several frames.
        /// This is done by running only the given number of layers of the neural network at a time.
        /// To call this method use <see cref="MonoBehaviour.StartCoroutine"/>.
        /// </summary>
        public IEnumerator _EstimateDepth(Texture input, EstimationFinished callback = null, int numLayersPerFrame = 20) {
            if (CheckIsRunning()) {
                yield break;
            }

            if (AutoResize) {
                Resize(ref input);
            }

            using var tensor = TextureConverter.ToTensor(_resizedInput, _resizedInput.width, _resizedInput.height, 3);
            var schedule = _worker.ScheduleIterable(tensor);
            int i = 0;
            while (schedule.MoveNext()) {
                if (++i % numLayersPerFrame == 0) {
                    yield return null;
                }
            }

            _predictedDepth = _worker.PeekOutput() as Tensor<float>;
            int height = _predictedDepth.shape[1];
            int width = _predictedDepth.shape[2];
            _predictedDepth.Reshape(_predictedDepth.shape.Unsqueeze(1));

            // normalize
            if (NormalizeDepth) {
                _predictedDepth = Normalize(_predictedDepth);
            }
            TextureConverter.RenderToTexture(_predictedDepth, Result);

            Result.name = $"{input.name}_depth_{_name}";
            _estimationRunning = false;

            callback?.Invoke(Result);
        }

        /// <summary>
        /// Runs depth estimation, but distributes the computation over several frames.
        /// This is done by running only the given number of layers of the neural network per frame.
        /// </summary>
        public void EstimateDepthAsync(Texture input, EstimationFinished callback, int numLayersPerFrame = 20) {
            CoroutineRunner.Start(_EstimateDepth(input, callback, numLayersPerFrame));
        }

        private async Task Test() {
            await Task.Delay(5000);
        }

        /// <summary>
        /// Returns the minimum and maximum values of the last depth prediction.
        /// </summary>
        /// <remarks>
        /// Keep in mind that the predictions are relative *inverse* depth values,
        /// i.e. min refers to the furthest away point and max to the closest point.
        /// </remarks>
        public (float min, float max) GetMinMax() {
            if (_predictedDepth == null) {
                throw new InvalidOperationException("No depth estimation has been executed yet. " +
                    "Call 'EstimateDepth' before trying to retrieve min/max");
            }
            // TODO: In cases where we already normalize the _predictedDepth anyway, we could
            // get the min/max data without having to run the normalization again.
            return _normalization.GetMinMax(_predictedDepth);
        }

        private void Resize(ref Texture input) {
            Graphics.Blit(input, _resizedInput);
            _resizedInput.name = input.name;
            input = _resizedInput;
        }

        /// <summary>
        /// Normalize on-device using Tensor Ops.
        /// </summary>
        private Tensor<float> Normalize(Tensor<float> depth) {
            return _normalization.Execute(depth);
        }

        private bool CheckIsRunning() {
            if (_estimationRunning) {
                Debug.LogWarning("You are trying to run the model while that last prediction has not yet finished.");
                return true;
            }
            _estimationRunning = true;
            return false;
        }

        public void Dispose() {
            _worker?.Dispose();
            _normalization?.Dispose();
            if (_resizedInput != null) {
                _resizedInput.Release();
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(_resizedInput);
#else
                UnityEngine.Object.Destroy(_resizedInput);
#endif
            }
        }

        public void EstimateDepth(Texture webCam, object onNewDepthPredicted) {
            throw new NotImplementedException();
        }
    }
}