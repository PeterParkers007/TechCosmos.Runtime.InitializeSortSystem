using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    public static class InitializationFactory
    {
        private static Dictionary<string, Type> _initializationTypes = new Dictionary<string, Type>();
        private static List<InitializeData> _preRegisteredData = new List<InitializeData>();
        private static bool _scanned = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PreRegisterAllSystems()
        {
            if (_scanned) return;

            ScanAllInitializationTypes();
            CreatePreRegisteredData();
            _scanned = true;

            Debug.Log($"[Initialization] 预注册完成，共 {_preRegisteredData.Count} 个系统");
        }

        private static void ScanAllInitializationTypes()
        {
            _initializationTypes.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("System.") ||
                    assembly.FullName.StartsWith("Unity.") ||
                    assembly.FullName.StartsWith("UnityEngine.") ||
                    assembly.FullName.StartsWith("UnityEditor."))
                    continue;

                try
                {
                    var types = assembly.GetTypes();
                    var initializationTypes = types
                        .Where(t =>
                            t.IsClass &&
                            !t.IsAbstract &&
                            typeof(IInitialization).IsAssignableFrom(t) &&
                            t.IsDefined(typeof(InitializeAttribute), false))
                        .ToList();

                    if (initializationTypes.Count > 0)
                    {
                        Debug.Log($"[Initialization]   找到 {initializationTypes.Count} 个初始化类型:");
                        foreach (var type in initializationTypes)
                        {
                            Debug.Log($"[Initialization]     - {type.FullName}");
                        }
                    }

                    foreach (var type in initializationTypes)
                    {
                        var attribute = type.GetCustomAttribute<InitializeAttribute>();
                        if (attribute != null && !_initializationTypes.ContainsKey(attribute.InitializationId))
                        {
                            _initializationTypes[attribute.InitializationId] = type;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {e.Message}");
                }
            }

            Debug.Log($"[Initialization] 扫描完成，共找到 {_initializationTypes.Count} 个初始化类型");
        }

        private static void CreatePreRegisteredData()
        {
            _preRegisteredData.Clear();

            foreach (var kvp in _initializationTypes)
            {
                var type = kvp.Value;

                try
                {
                    if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        // MonoBehaviour：查找场景中已存在的实例
                        var existingComponents = UnityEngine.Object.FindObjectsOfType(type)
                            .Cast<IInitialization>()
                            .ToList();

                        foreach (var component in existingComponents)
                        {
                            int priority = GetPriorityFromComponent(component);
                            _preRegisteredData.Add(new InitializeData(
                                component.Initialize, // 直接引用场景中组件的初始化方法
                                priority
                            ));
                            Debug.Log($"[Initialization] 注册场景组件: {type.Name} (优先级: {priority})");
                        }
                    }
                    else
                    {
                        // 普通类：创建新实例
                        int priority = GetPriorityFromType(type);
                        _preRegisteredData.Add(new InitializeData(
                            () => CreateAndInitializeClass(type),
                            priority
                        ));
                        Debug.Log($"[Initialization] 注册普通类: {type.Name} (优先级: {priority})");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"注册系统 {kvp.Key} 失败: {e.Message}");
                }
            }
        }

        private static int GetPriorityFromComponent(IInitialization component)
        {
            
            return component.Priority; // 使用接口的Priority属性
        }

        private static int GetPriorityFromType(Type type)
        {
            // 应该从类型获取优先级，比如通过Attribute或默认值
            // 临时方案：创建实例获取Priority
            try
            {
                var instance = (IInitialization)Activator.CreateInstance(type);
                return instance.Priority;
            }
            catch
            {
                return 0; // 默认优先级
            }
        }

        private static void CreateAndInitializeClass(Type type)
        {
            try
            {
                var instance = (IInitialization)Activator.CreateInstance(type);
                instance.Initialize();
                Debug.Log($"[Initialization] 普通类初始化完成: {type.Name}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Initialization] 普通类初始化 {type.Name} 失败: {e.Message}");
            }
        }

        public static void ExecutePreRegisteredSystems()
        {
            if (!_scanned)
            {
                Debug.LogWarning("[Initialization] 系统未预注册，立即执行扫描");
                PreRegisterAllSystems();
            }

            var orderedData = _preRegisteredData.OrderByDescending(x => x.SortLevel).ToList();

            Debug.Log($"[Initialization] 开始执行 {orderedData.Count} 个系统");

            foreach (var data in orderedData)
            {
                try
                {
                    data.InitializeAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Initialization] 执行失败: {ex.Message}");
                }
            }

            Debug.Log($"[Initialization] 所有系统执行完成");
        }

        // 调试方法
        public static void DebugRegisteredSystems()
        {
            foreach (var data in _preRegisteredData.OrderByDescending(x => x.SortLevel))
            {
                Debug.Log($"系统优先级: {data.SortLevel}");
            }
        }
    }
}