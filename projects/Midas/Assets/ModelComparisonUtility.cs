using Doji.AI.Depth;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MidasSample {
    public class ModelComparisonUtility : MonoBehaviour {

        public Texture2D InputTexture;

        [MenuItem("CONTEXT/ModelComparisonUtility/Create Depth Maps for all Models")]
        static void DoSomething(MenuCommand command) {
            var x = (ModelComparisonUtility)command.context;
            CreateDepthMapTexture(x.InputTexture);
        }

        private static void CreateDepthMapTexture(Texture inputTexture) {
            if (inputTexture == null) {
                Debug.LogError("Input texture is not assigned. Please assign it in the Unity Editor.");
                return;
            }

            // Calculate the dimensions of the combined texture based on inputTexture
            int combinedWidth = 4 * inputTexture.width;
            int combinedHeight = 3 * inputTexture.height;


            ModelType[] allModels = (ModelType[])Enum.GetValues(typeof(ModelType));
            List<Texture> resizedDepthMaps = new List<Texture>();

            // Iterate over all Midas models
            foreach (ModelType modelType in allModels) {
                if (modelType == ModelType.Unknown) {
                    continue;
                }

                if (!File.Exists(Path.Combine("Packages", "com.doji.midas", "Runtime", "Resources", "ONNX", $"{modelType.ToString().ToLower()}.onnx"))) {
                    resizedDepthMaps.Add(null);
                    continue;
                }

                using (Midas midas = new Midas(modelType)) {
                    RenderTexture depth = midas.EstimateDepth(inputTexture);

                    // resize to inputTexture dimensions
                    int width = inputTexture.width;
                    int height = inputTexture.height;
                    RenderTexture depthMap = GetResized(depth, width, height);
                    resizedDepthMaps.Add(depthMap);
                }
            }

            RenderTexture combinedTexture = GraphicsUtility.Merge(resizedDepthMaps, 4);

            // Save the combined texture to a file or use it as needed
            SaveTextureToFile(combinedTexture, "CombinedDepthMaps.png");

            Dispose(combinedTexture);
            foreach(RenderTexture t in resizedDepthMaps.Cast<RenderTexture>()) {
                RenderTexture.ReleaseTemporary(t);
            }
        }

        private static void Dispose(RenderTexture texture) {
            texture.Release();
#if UNITY_EDITOR
            DestroyImmediate(texture);
#else
            Destroy(texture);
#endif
        }

        private static void SaveTextureToFile(RenderTexture texture, string fileName) {
            RenderTexture.active = texture;

            Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RFloat, false);
            texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture2D.Apply();

            byte[] bytes = texture2D.EncodeToPNG();
            System.IO.File.WriteAllBytes(fileName, bytes);

            RenderTexture.active = null;
        }

        /// <summary>
        /// Gets a resized temporary RenderTexture of the original image
        /// </summary>
        private static RenderTexture GetResized(RenderTexture texture, int width, int height) {
            RenderTexture tmp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Default);
            Graphics.Blit(texture, tmp);
            return tmp;
        }
    }
}