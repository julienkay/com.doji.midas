using System;
using Unity.Sentis;

namespace Doji.AI.Depth {

    /// <summary>
    /// Helper class to process the output of Midas models
    /// (like normalization and min/max calculation).
    /// </summary>
    internal class Normalization : IDisposable {

        private Worker _worker;

        public Normalization(DynamicTensorShape inputShape, BackendType backendType) {
            inputShape.Set(1, 1); // we pass the shape of model input (rgb), depth predictions only have a single channel
            _worker = InitModel(inputShape, backendType);
        }

        public static Worker InitModel(DynamicTensorShape shape, BackendType backendType) {
            FunctionalGraph graph = new FunctionalGraph();
            FunctionalTensor depth = graph.AddInput<float>(shape);
            var min = Functional.ReduceMin(depth, new int[] { }, false);
            var max = Functional.ReduceMax(depth, new int[] { }, false);
            var normalized = (depth - min ) / (max - min);
            var model = graph.Compile(normalized, min, max);
            var worker = new Worker(model, backendType);
            return worker;
        }

        public Tensor<float> Execute(Tensor<float> depth) {
            if (depth == null) {
                throw new ArgumentNullException("Depth to normalize can not be null", nameof(depth));
            }
            _worker.Schedule(depth);
            return _worker.PeekOutput("output_0") as Tensor<float>;
        }

        /// <summary>
        /// Returns the min and max values of the depth prediction.
        /// </summary>
        public (float min, float max) GetMinMax(Tensor<float> depth) {
            if (depth == null) {
                throw new ArgumentNullException("Depth to normalize can not be null", nameof(depth));
            }
            _worker.Schedule(depth);
            Tensor<float> min = _worker.PeekOutput("output_1") as Tensor<float>;
            Tensor<float> max = _worker.PeekOutput("output_2") as Tensor<float>;
            min = min.ReadbackAndClone();
            max = max.ReadbackAndClone();
            var minmax = (min[0], max[0]);
            min.Dispose();
            max.Dispose();
            return minmax;
        }

        public void Dispose() {
            _worker?.Dispose();
        }
    }
}