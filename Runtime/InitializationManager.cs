using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
namespace TechCosmos.InitializeSortSystem.Runtime
{
    public class InitializationManager : MonoBehaviour
    {
        private List<InitializeData> _initializationQueue = new();
        public void RegisterInitialization(Action initializeAction, int priority = 0)
        {
            // 可选：添加重复检查
            if (_initializationQueue.Any(x => x.InitializeAction == initializeAction))
                return;

            _initializationQueue.Add(new InitializeData(initializeAction, priority));
        }

        void Awake()
        {
            foreach (var data in _initializationQueue.OrderByDescending(x => x.SortLevel))
            {
                try
                {
                    data.InitializeAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"初始化失败: {ex.Message}");
                    // 继续执行其他初始化，不阻断整个流程
                }
            }
            _initializationQueue.Clear();
        }
        private void OnDestroy()
        {
            _initializationQueue.Clear();
        }
    }
}
