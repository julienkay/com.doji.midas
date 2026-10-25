using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Doji.AI.Depth.Editor.Tests
{
    /// <summary>
    /// Locks MiDaS-to-HF-Hub mapping so runtime lookup stays stable across package updates.
    /// </summary>
    public sealed class ModelTypeExtensionsTests
    {
        [Test]
        public void RepoId_ReturnsSharedMidasRepository_ForEveryKnownModel()
        {
            foreach (ModelType modelType in System.Enum.GetValues(typeof(ModelType)))
            {
                if (modelType == ModelType.Unknown)
                {
                    continue;
                }

                Assert.That(modelType.RepoId(), Is.EqualTo("julienkay/sentis-MiDaS"));
            }
        }

        [Test]
        public void RepoPath_UsesOnnxFolderAndModelFileName()
        {
            Assert.That(ModelType.midas_v21_small_256.RepoPath(), Is.EqualTo("onnx/midas_v21_small_256.onnx"));
            Assert.That(ModelType.dpt_beit_large_384.RepoPath(), Is.EqualTo("onnx/dpt_beit_large_384.onnx"));
            Assert.That(ModelType.dpt_next_vit_large_384.RepoPath(), Is.EqualTo("onnx/dpt_next_vit_large_384.onnx"));
        }

        [System.Serializable]
        class Manifest
        {
            public ManifestModel[] models;
        }

        [System.Serializable]
        class ManifestModel
        {
            public string id;
            public ManifestFile[] files;
        }

        [System.Serializable]
        class ManifestFile
        {
            public string path;
            public bool optional;
        }

        [Test]
        public void Manifest_SelectsEveryModelTypeAndOnlyTheDefaultIsRequired()
        {
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText("Packages/com.doji.midas/hf-hub-manifest.json"));
            var model = manifest.models.Single(m => m.id == ModelExtensions.RepositoryId);

            foreach (ModelType modelType in System.Enum.GetValues(typeof(ModelType)))
            {
                if (modelType == ModelType.Unknown)
                {
                    continue;
                }

                var file = model.files.SingleOrDefault(f => f.path == modelType.RepoPath());
                Assert.That(file, Is.Not.Null, $"{modelType.RepoPath()} is not in hf-hub-manifest.json");
                Assert.That(file.optional, Is.EqualTo(modelType != ModelType.midas_v21_small_256), modelType.ToString());
            }
        }

        [Test]
        public void RepoId_ThrowsForUnknownModel()
        {
            Assert.That(
                () => ModelType.Unknown.RepoId(),
                Throws.TypeOf<System.ArgumentException>());
        }
    }
}
