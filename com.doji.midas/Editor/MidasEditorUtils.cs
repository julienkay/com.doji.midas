using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine.Networking;

namespace Midas.Editor {

    public static class MidasEditorUtils {

        private static HashSet<ModelType> _downloads = new HashSet<ModelType>();

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad() {
            Midas.OnModelRequested -= Validate;
            Midas.OnModelRequested += Validate;
        }

        private static void Validate(ModelType model) {
            if (model == ModelType.Unknown) {
                return;
            }
            if (File.Exists(model.AssetPath())) {
                return;
            }
            if (InProgress(model)) {
                return;
            }
            if (!ShouldDownload(model)) {
                return;
            }

            EditorApplication.ExitPlaymode();
            DownloadModelAsync(model);
        }

        private async static void DownloadModelAsync(ModelType model) {
            Uri dl = model.GetUrl();
            UnityWebRequest wr = UnityWebRequest.Get(dl);
            var asyncOp = wr.SendWebRequest();

            int dlID = Progress.Start($"Downloading {model}");
            Progress.RegisterCancelCallback(dlID, () => { return true; });
            _downloads.Add(model);
            bool canceled = false;

            while (!asyncOp.isDone) {
                if (Progress.GetStatus(dlID) == Progress.Status.Canceled) {
                    wr.Abort();
                    canceled = true;
                }
                Progress.Report(dlID, wr.downloadProgress, $"{model} download progress...");
                await Task.Yield();
            }
            Progress.Remove(dlID);
            _downloads.Remove(model);

            if (canceled) {
                return;
            }

            byte[] data = asyncOp.webRequest.downloadHandler.data;

            if (wr.error != null || data == null || data.Length == 0) {
                EditorUtility.DisplayDialog(
                    "com.doji.midas | Download Error",
                    $"Downloading {model.FileName()}.onnx failed.\n{wr.error}",
                "OK");
            } else {
                File.WriteAllBytes(
                    model.AssetPath(),
                    data
                );
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Check whether user wants to download
        /// </summary>
        private static bool ShouldDownload(ModelType model) {
            return EditorUtility.DisplayDialog(
               "com.doji.midas | Downloaded Model",
               "You are trying to use a Midas model that is not yet downloaded to your machine.\n\n" +
               "Would you like to exit Play Mode and download the following model?\n\n" +
               $"{model.FileName()}.onnx\n\n" +
               "The download will happen in the background and might take a while.",
               "Download", "Cancel");
        }

        /// <summary>
        /// Is download for this model in progress?
        /// </summary>
        private static bool InProgress(ModelType model) {
            if (_downloads == null) {
                return true;
            }
            return _downloads.Contains(model);
        }
    }
}
