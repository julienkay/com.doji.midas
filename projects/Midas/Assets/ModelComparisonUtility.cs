using Chromatic;
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
                Debug.LogError("Input input is not assigned. Please assign it in the Unity Editor.");
                return;
            }

            ModelType[] allModels = (ModelType[])Enum.GetValues(typeof(ModelType));
            List<RenderTexture> tmpTextures = new List<RenderTexture>();
            // add source texture
            List<RenderTexture> outputTextures = new List<RenderTexture> {
                GetTmpCopy(inputTexture)
            };

            // Iterate over all Midas models
            foreach (ModelType modelType in allModels) {
                if (modelType == ModelType.Unknown) {
                    continue;
                }

                if (modelType == ModelType.dpt_next_vit_large_384) {
                    continue; // skip because this one seems broken?
                }

                if (!File.Exists(Path.Combine("Packages", "com.doji.midas", "Runtime", "Resources", "ONNX", $"{modelType.ToString().ToLower()}.onnx"))) {
                    outputTextures.Add(null);
                    continue;
                }

                using (Midas midas = new Midas(modelType)) {
                    RenderTexture depth = midas.Result;
                    midas.EstimateDepth(inputTexture);

                    // resize to inputTexture dimensions
                    int width = inputTexture.width;
                    int height = inputTexture.height;
                    RenderTexture resizedDepth = GetResized(depth, width, height);
                    tmpTextures.Add(resizedDepth);

                    // apply color scheme
                    RenderTexture coloredDepth = GetColorized(resizedDepth, ColorMaps.magma);
                    outputTextures.Add(coloredDepth);
                }
            }

            RenderTexture combinedTexture = GraphicsUtility.Merge(outputTextures, 4);

            // Save the combined input to a file or use it as needed
            SaveTextureToFile(combinedTexture, $"{inputTexture.name}_depth.png");

            Dispose(combinedTexture);
            foreach(RenderTexture t in tmpTextures) {
                RenderTexture.ReleaseTemporary(t);
            }
            foreach (RenderTexture t in outputTextures) {
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
            Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
            texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            texture2D.Apply();

            byte[] bytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(fileName, bytes);

            RenderTexture.active = null;
        }

        /// <summary>
        /// Gets a resized temporary RenderTexture of the original image
        /// </summary>
        private static RenderTexture GetResized(Texture texture, int width, int height) {
            RenderTexture tmp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, tmp);
            return tmp;
        }

        private static RenderTexture GetTmpCopy(Texture texture) {
            RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, tmp);
            return tmp;
        }

        /// <summary>
        /// Gets a colorized temporary RenderTexture.
        /// </summary>
        private static RenderTexture GetColorized(RenderTexture input, Texture2D colorMap) {
            RenderTexture tmp = RenderTexture.GetTemporary(input.width, input.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            GraphicsUtility.MapTexture(input, tmp, colorMap);
            return tmp;
        }
    }
}