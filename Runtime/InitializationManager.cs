using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    public class InitializationManager : MonoBehaviour
    {
        public static InitializationManager Instance { get; private set; }
        public static bool IsInitialized { get; private set; }

        

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