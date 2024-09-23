using System.Collections;
using UnityEngine;

namespace Doji.AI.Depth {
    
    /// <summary>
    /// A helper object to run coroutines.
    /// </summary>
    internal class CoroutineRunner : MonoBehaviour {

        public static CoroutineRunner Instance {
            get {
                if (_instance == null) {
                    GameObject coroutineRunner = new GameObject("com.doji.midas_CoroutineRunner");
                    _instance = coroutineRunner.AddComponent<CoroutineRunner>();
                    coroutineRunner.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    DontDestroyOnLoad(coroutineRunner);
                }
                return _instance;
            }
        }
        private static CoroutineRunner _instance;

        public static Coroutine Start(IEnumerator coroutine) {
            return Instance.StartCoroutine(coroutine);
        }

        public static void Stop(Coroutine coroutine) {
            if (_instance != null) {
                Instance.StopCoroutine(coroutine);
            }
        }
    }
}