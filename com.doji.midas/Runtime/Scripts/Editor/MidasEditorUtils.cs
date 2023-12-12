using System.IO;
using System;
using UnityEngine.Networking;
using Unity.Sentis;

namespace Midas {

    public static class MidasEditorUtils {

        public static void DownloadModel(ModelType model) {
            if (model == ModelType.Unknown) {
                return;
            }
            if (File.Exists(model.AssetPath())) {
                return;
            }

            if (!UnityEditor.EditorUtility.DisplayDialog(
                "com.doji.midas | Downloaded Model",
                "You are trying to use a Midas model that is not yet downloaded to your machine.\n\n" +
                "Would you like to download the following model?\n\n" +
                $"{model.FileName()}.onnx",
                "Download", "Cancel")) {
                return;
            }

            Uri dl = model.GetUrl();
            UnityWebRequest wr = UnityWebRequest.Get(dl);
            var asyncOp = wr.SendWebRequest();
            while (!asyncOp.isDone) { }
            byte[] data = asyncOp.webRequest.downloadHandler.data;
            string packagePath = Path.Combine("Packages", "com.doji.midas", "Runtime", "Resources", "ONNX", $"{model.FileName()}.onnx");

            if (wr.error != null || data == null || data.Length == 0) {
                UnityEditor.EditorUtility.DisplayDialog(
                    "com.doji.midas | Download Error",
                    $"Downloading {model.FileName()}.onnx failed.\n{wr.error}",
                "OK");
            } else {
                File.WriteAllBytes(
                    model.AssetPath(),
                    data
                );
                UnityEditor.AssetDatabase.Refresh();
            }

            if (!File.Exists(model.AssetPath())) {
                throw new FileNotFoundException($"File for model '{model}' not found.");
            }
        }
    }
}