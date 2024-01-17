using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace MidasSample {

    public static class GraphicsUtility {

        /// <summary>
        /// Puts all <paramref name="textures"/> into a larger texture.
        /// Assumes all the textures have same dimensions and format.
        /// null textures are allowed and will be skipped and result in an empty block
        /// in the combined texture.
        /// </summary>
        public static RenderTexture Merge(IEnumerable<Texture> textures, int numTexturesPerRow) {
            if (textures == null) {
                throw new ArgumentNullException(nameof(textures));
            }
            if (textures.Count() == 0) {
                return null;
            }

            Texture reference = textures.FirstOrDefault(element => element != null);
            if (reference == null) {
                throw new InvalidOperationException("No non-null texture found.");
            }

            return MergeInternal(textures, numTexturesPerRow);
        }

        /// <summary>
        /// Merges all <paramref name="textures"/> into a larger texture.
        /// TODO: rescale to largest needed dimension if textures have different dimensions
        /// </summary>
        private static RenderTexture MergeInternal(IEnumerable<Texture> textures, int numTexturesPerRow) {
            Texture reference = textures.FirstOrDefault(element => element != null);
            int width = reference.width;
            int height = reference.height;
            GraphicsFormat format = reference.graphicsFormat;

            // Calculate the dimensions of the combined texture based on input textures
            int numRows = (int)Math.Ceiling((float)textures.Count() / numTexturesPerRow);
            int numCols = numTexturesPerRow;
            int combinedWidth = numCols * width;
            int combinedHeight = numRows * height;

            // Create a texture to store all depth maps
            RenderTexture combinedTexture = new RenderTexture(combinedWidth, combinedHeight, 0, format);

            int row = 0, col = 0;
            foreach (Texture texture in textures) {
                if (textures == null) {
                    continue;
                }

                int targetX = col * width;
                int targetY = combinedHeight - (row * height) - height; // because textures origin = bottom left
                
                // Copy the texture to the combined texture
                Graphics.CopyTexture(texture, 0, 0, 0, 0, width, height, combinedTexture, 0, 0, targetX, targetY);

                // Move to the next column
                col++;

                // Move to the next row if the current row is complete
                if (col >= 4) {
                    col = 0;
                    row++;
                }
            }

            return combinedTexture;
        }

        public static Texture2D GetCPUCopy(RenderTexture texture) {
            int width = texture.width;
            int height = texture.height;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RFloat, false);
            RenderTexture.active = texture;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        public static void Resize(Texture2D texture, int width, int height) {
            RenderTexture tmp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture.active = tmp;
            Graphics.Blit(texture, tmp);
#if UNITY_2021_2_OR_NEWER
            texture.Reinitialize(width, height, texture.format, false);
#else
            texture.Resize(width, height, texture.format, false);
#endif
            texture.filterMode = FilterMode.Bilinear;
            texture.ReadPixels(new Rect(Vector2.zero, new Vector2(width, height)), 0, 0);
            texture.Apply();
            RenderTexture.ReleaseTemporary(tmp);
            RenderTexture.active = null;
        }

        /// <summary>
        /// Applies a color mapping to a source RenderTexture based on a gradient texture and writes the result to a destination RenderTexture.
        /// </summary>
        /// <param name="sourceTexture">The source RenderTexture to be mapped.</param>
        /// <param name="destinationTexture">The destination RenderTexture where the mapped result will be written.</param>
        /// <param name="gradientTexture">The gradient texture used for color mapping.</param>
        public static void MapTexture(Texture sourceTexture, RenderTexture destinationTexture, Texture gradientTexture) {
            Material material = new Material(Shader.Find("Doji/Chromatic/ColorRamp"));
            material.SetTexture("_MainTex", sourceTexture);
            material.SetTexture("_ColorRamp", gradientTexture);

            Graphics.Blit(sourceTexture, destinationTexture, material);

            // Clean up the temporary material
#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(material);
#else
            UnityEngine.Object.Destroy(material);
#endif
        }
    }
}
