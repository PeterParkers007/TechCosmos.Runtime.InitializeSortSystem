using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    public class InitializationManager : MonoBehaviour
    {
        public static InitializationManager Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (Instance != null) return;

            var managerObject = new GameObject("InitializationManager");
            Instance = managerObject.AddComponent<InitializationManager>();
            DontDestroyOnLoad(managerObject);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ExecuteAllSystems();
        }

        private void ExecuteAllSystems()
        {
            InitializationFactory.ExecutePreRegisteredSystems();
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                IsInitialized = false;
            }
        }
    }
}