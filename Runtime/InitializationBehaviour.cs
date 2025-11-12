using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    public abstract class InitializationBehaviour : MonoBehaviour, IInitialization
    {
        [SerializeField] protected int _priority = 0;

        public virtual int Priority => _priority;

        public abstract void Initialize();

        // 可选：提供一些公共功能
        protected virtual void OnInitialized() { }
    }
}
