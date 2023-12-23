using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MidasSample {

    public static class GraphicsUtility {

        /// <summary>
        /// Puts all <paramref name="textures"/> into a larger texture.
        /// Assumes all the textures have same dimensions.
        /// null textures are allowed and will be skipped and result in an empty block
        /// in the combined texture.
        /// </summary>
        public static RenderTexture Merge(IList<Texture> textures, int numTexturesPerRow) {
            if (textures == null) {
                throw new ArgumentNullException(nameof(textures));
            }
            if (textures.Count == 0) {
                return null;
            }

            Texture reference = textures.FirstOrDefault(element => element != null);
            if (reference == null) {
                throw new InvalidOperationException("No non-null texture found.");
            }

            return MergeInternal(textures, reference.width, reference.height, numTexturesPerRow);
        }

        /// <summary>
        /// Merges all <paramref name="textures"/> into a larger texture.
        /// TODO: rescale to largest needed dimension if textures have different dimensions
        /// </summary>
        private static RenderTexture MergeInternal(IList<Texture> textures, int width, int height, int numTexturesPerRow) {
            int row = 0;
            int col = 0;

            // Calculate the dimensions of the combined texture based on input textures
            int combinedWidth = 4 * width;
            int combinedHeight = 3 * height;

            // Create a texture to store all depth maps
            RenderTexture combinedTexture = new RenderTexture(combinedWidth, combinedHeight, 0, RenderTextureFormat.RFloat);

            for (int i = 0; i < textures.Count; i++) {
                if (textures[i] == null) {
                    continue;
                }

                int targetX = col * width;
                int targetY = combinedHeight - (row * height) - height; // because textures origin = bottom left
                
                // Copy the texture to the combined texture
                Graphics.CopyTexture(textures[i], 0, 0, 0, 0, width, height, combinedTexture, 0, 0, targetX, targetY);

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
    }
}
