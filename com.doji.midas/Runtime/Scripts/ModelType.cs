using System;
using System.IO;

namespace Midas {

    /// <summary>
    /// All the supported MiDaS models.
    /// </summary>
    public enum ModelType {

        Unknown = -1,

        /// <summary>
        /// 63 MB MiDaS v2.1 model.
        /// </summary>
        midas_v21_small_256,

        /// <summary>
        /// 397 MB MiDaS v2.1 model.
        /// </summary>
        midas_v21_384,

        /// <summary>
        /// 1.34 GB MiDaS v3.1 model with a beitl16_512 backbone.
        /// </summary>
        dpt_beit_large_512,

        /// <summary>
        /// 1.34 GB MiDaS v3.1 model with a beitl16_384 backbone.
        /// </summary>
        dpt_beit_large_384,

        /// <summary>
        /// 450 MB MiDaS v3.1 model with a beitb16_384 backbone.
        /// </summary>
        dpt_beit_base_384,

        /// <summary>
        /// 832 MB MiDaS v3.1 model with a swin2l24_384 backbone.
        /// </summary>
        dpt_swin2_large_384,

        /// <summary>
        /// 410 MiDaS v3.1 model with a swin2b24_384 backbone.
        /// </summary>
        dpt_swin2_base_384,

        /// <summary>
        /// 157 MiDaS v3.1 model with a swin2t16_256 backbone.
        /// </summary>
        dpt_swin2_tiny_256,

        /// <summary>
        /// 854 MB MiDaS v3.1 model with a swinl12_384 backbone.
        /// </summary>
        dpt_swin_large_384,

        /// <summary>
        /// 267 MB MiDaS v3.1 model with a next_vit_large_6m backbone.
        /// </summary>
        dpt_next_vit_large_384,

        /// <summary>
        /// 136 MB MiDaS v3.0 model with a levit_384 backbone.
        /// </summary>
        dpt_levit_224,

        /// <summary>
        /// 1.27 GB MiDaS v3.0 model with a vitl16_384 backbone.
        /// </summary>
        dpt_large_384,
    }

    public static class ModelExtensions {

        private const string BASE_URL = "https://github.com/julienkay/com.doji.midas/releases/download/v1.0.0/";

        /// <summary>
        /// Returns the download link to the model weights for this model.
        /// </summary>
        public static Uri GetUrl(this ModelType model) {
            return new Uri($"{BASE_URL}{model.FileName()}.onnx");
        }

        /// <summary>
        /// The file name of the given <paramref name="model"/>.
        /// </summary>
        public static string FileName(this ModelType model) {
            return model switch {
                ModelType.dpt_beit_large_512     => "dpt_beit_large_512",
                ModelType.dpt_beit_large_384     => "dpt_beit_large_384",
                ModelType.dpt_beit_base_384      => "dpt_beit_base_384",
                ModelType.dpt_swin2_large_384    => "dpt_swin2_large_384",
                ModelType.dpt_swin2_base_384     => "dpt_swin2_base_384",
                ModelType.dpt_swin2_tiny_256     => "dpt_swin2_tiny_256",
                ModelType.dpt_swin_large_384     => "dpt_swin_large_384",
                ModelType.dpt_next_vit_large_384 => "dpt_next_vit_large_384",
                ModelType.dpt_levit_224          => "dpt_levit_224",
                ModelType.dpt_large_384          => "dpt_large_384",
                ModelType.midas_v21_384          => "midas_v21_384",
                ModelType.midas_v21_small_256    => "midas_v21_small_256",
                _                                => throw new ArgumentException($"Unknown model type: {model}"),
            };
        }

        /// <summary>
        /// The location of the given <paramref name="model"/> in the Resources folder.
        /// </summary>
        public static string ResourcePath(this ModelType model) {
            return Path.Combine("ONNX", model.FileName());
        }

#if UNITY_EDITOR
        /// <summary>
        /// The path to the given <paramref name="model"/> in the Packages folder.
        /// </summary>
        public static string AssetPath(this ModelType model) {
            return Path.Combine("Packages", "com.doji.midas", "Runtime", "Resources", "ONNX", $"{model.FileName()}.onnx");
        }
#endif
    }
}