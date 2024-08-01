using System;
using Unity.Sentis;

namespace Doji.AI.Depth {

    /// <summary>
    /// Helper class to process the output of Midas models
    /// (like normalization and min/max calculation).
    /// </summary>
    internal class Normalization : IDisposable {

        private IWorker _normalizationModel;
        private IBackend _backend;
        private TensorFloat _min;
        private TensorFloat _max;

        public Normalization(int width, int height, BackendType backendType) {
            _normalizationModel = InitModel(width, height);
            _backend = WorkerFactory.CreateBackend(backendType);
            _min = TensorFloat.AllocNoData(new TensorShape(1));
            _max = TensorFloat.AllocNoData(new TensorShape(1));
        }

        public static IWorker InitModel(int width, int height) {
            var shape = new TensorShape(1, 1, 256, 256);

            var inputDefs = InputDef.Float(shape);

            FunctionalTensor normalizationModel(FunctionalTensor depth) {
                var min = Functional.ReduceMin(depth, new int[] { }, false);
                var max = Functional.ReduceMax(depth, new int[] { }, false);
                return (depth - min ) / (max - min);
            }
            var model = Functional.Compile(normalizationModel, inputDefs);
            var worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, model);

            return worker;
        }

        public TensorFloat Execute(TensorFloat input) {
            _normalizationModel.Execute(input);
            return _normalizationModel.PeekOutput("output_0") as TensorFloat;
        }

        public (float min, float max) GetMinMax(TensorFloat depth) {
            if (depth == null) {
                throw new ArgumentException("Depth to normalize can not be null", nameof(depth));
            }

            _backend.ReduceMin(depth, _min, null);
            _backend.ReduceMax(depth, _max, null);
            using TensorFloat min = _min.ReadbackAndClone();
            using TensorFloat max = _max.ReadbackAndClone();

            return (min[0], max[0]);
        }

        public void Dispose() {
            _normalizationModel?.Dispose();
            _backend?.Dispose();
            _min?.Dispose();
            _max?.Dispose();
        }
    }
}