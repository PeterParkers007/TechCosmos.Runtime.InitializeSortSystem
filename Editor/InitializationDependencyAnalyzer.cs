#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TechCosmos.InitializeSortSystem.Runtime;
using UnityEditor;
using UnityEngine;

namespace TechCosmos.InitializeSortSystem.Editor
{
    public class InitializationDependencyAnalyzer : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<SystemInfo> _systemInfos = new List<SystemInfo>();
        private bool _autoApplyPriorities = true;

        [MenuItem("Tools/TechCosmos/初始化依赖分析器")]
        public static void ShowWindow()
        {
            GetWindow<InitializationDependencyAnalyzer>("初始化依赖分析器");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // 控制选项
            EditorGUILayout.BeginVertical("box");
            {
                _autoApplyPriorities = EditorGUILayout.Toggle("自动应用优先级", _autoApplyPriorities);

                GUILayout.Space(5);

                if (GUILayout.Button("扫描依赖关系", GUILayout.Height(30)))
                {
                    ScanDependencies();
                }

                if (GUILayout.Button("应用计算后的优先级", GUILayout.Height(30)))
                {
                    ApplyCalculatedPriorities();
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 结果显示
            if (_systemInfos.Count > 0)
            {
                GUILayout.Label($"找到 {_systemInfos.Count} 个系统:", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    foreach (var systemInfo in _systemInfos.OrderBy(s => s.CalculatedPriority))
                    {
                        if (systemInfo != null)
                        {
                            DrawSystemInfo(systemInfo);
                        }
                        
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSystemInfo(SystemInfo systemInfo)
        {
            EditorGUILayout.BeginVertical("box");
            {
                // 系统名称和ID
                EditorGUILayout.LabelField(systemInfo.SystemId, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"类型: {systemInfo.Type.Name}");

                // 优先级信息
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"当前优先级: {systemInfo.CurrentPriority}");
                    EditorGUILayout.LabelField($"计算优先级: {systemInfo.CalculatedPriority}",
                        systemInfo.NeedsUpdate ? EditorStyles.whiteLabel : EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                // 依赖信息
                if (systemInfo.Dependencies.Count > 0)
                {
                    EditorGUILayout.LabelField($"依赖: {string.Join(", ", systemInfo.Dependencies)}");
                }

                if (systemInfo.Dependents.Count > 0)
                {
                    EditorGUILayout.LabelField($"被依赖: {string.Join(", ", systemInfo.Dependents)}", EditorStyles.miniLabel);
                }

                // 操作按钮
                if (systemInfo.NeedsUpdate)
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("更新优先级", GUILayout.Width(100)))
                        {
                            UpdateSystemPriority(systemInfo);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        private void ScanDependencies()
        {
            _systemInfos.Clear();

            // 扫描所有程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var systemTypes = new Dictionary<string, Type>();

            foreach (var assembly in assemblies)
            {
                if (assembly.FullName.StartsWith("System.") ||
                    assembly.FullName.StartsWith("Unity.") ||
                    assembly.FullName.StartsWith("UnityEngine."))
                    continue;

                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass &&
                                   !t.IsAbstract &&
                                   t.IsDefined(typeof(InitializeAttribute), false));

                    foreach (var type in types)
                    {
                        var attribute = type.GetCustomAttribute<InitializeAttribute>();
                        if (attribute != null && !systemTypes.ContainsKey(attribute.InitializationId))
                        {
                            systemTypes[attribute.InitializationId] = type;
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {e.Message}");
                }
            }

            // 构建系统信息
            foreach (var kvp in systemTypes)
            {
                var systemInfo = CreateSystemInfo(kvp.Value, kvp.Key);
                _systemInfos.Add(systemInfo);
            }

            // 计算依赖关系图
            BuildDependencyGraph();

            // 拓扑排序计算优先级
            CalculatePriorities();

            Debug.Log($"依赖分析完成！共扫描到 {_systemInfos.Count} 个系统");

            // 自动应用优先级
            if (_autoApplyPriorities)
            {
                ApplyCalculatedPriorities();
            }
        }

        private SystemInfo CreateSystemInfo(Type type, string systemId)
        {
            var systemInfo = new SystemInfo
            {
                Type = type,
                SystemId = systemId,
                Dependencies = new HashSet<string>(),
                Dependents = new HashSet<string>()
            };

            // 获取当前优先级
            var instance = Activator.CreateInstance(type) as IInitialization;
            systemInfo.CurrentPriority = instance?.Priority ?? 0;

            // 解析依赖关系
            var dependsOnAttributes = type.GetCustomAttributes<DependsOnAttribute>();
            foreach (var attr in dependsOnAttributes)
            {
                foreach (var dependency in attr.SystemIds)
                {
                    systemInfo.Dependencies.Add(dependency);
                }
            }

            return systemInfo;
        }

        private void BuildDependencyGraph()
        {
            // 构建依赖图
            foreach (var system in _systemInfos)
            {
                foreach (var dependency in system.Dependencies)
                {
                    var dependentSystem = _systemInfos.FirstOrDefault(s => s.SystemId == dependency);
                    if (dependentSystem != null)
                    {
                        dependentSystem.Dependents.Add(system.SystemId);
                    }
                }
            }
        }

        private void CalculatePriorities()
        {
            // 拓扑排序计算优先级
            var sortedSystems = TopologicalSort();

            // 基于依赖深度分配优先级（深度越大，优先级越高）
            int basePriority = 1000;
            foreach (var system in sortedSystems)
            {
                system.CalculatedPriority = basePriority;
                basePriority -= 10; // 每个系统间隔10个优先级单位
            }
        }

        private List<SystemInfo> TopologicalSort()
        {
            var result = new List<SystemInfo>();
            var visited = new HashSet<string>();
            var tempMark = new HashSet<string>();

            foreach (var system in _systemInfos)
            {
                if (!visited.Contains(system.SystemId))
                {
                    Visit(system, visited, tempMark, result);
                }
            }

            return result;
        }

        private void Visit(SystemInfo system, HashSet<string> visited, HashSet<string> tempMark, List<SystemInfo> result)
        {
            if (tempMark.Contains(system.SystemId))
            {
                Debug.LogError($"发现循环依赖！系统: {system.SystemId}");
                return;
            }

            if (!visited.Contains(system.SystemId))
            {
                tempMark.Add(system.SystemId);

                // 先访问所有依赖
                foreach (var dependency in system.Dependencies)
                {
                    var dependentSystem = _systemInfos.FirstOrDefault(s => s.SystemId == dependency);
                    if (dependentSystem != null)
                    {
                        Visit(dependentSystem, visited, tempMark, result);
                    }
                }

                tempMark.Remove(system.SystemId);
                visited.Add(system.SystemId);
                result.Add(system);
            }
        }

        private void ApplyCalculatedPriorities()
        {
            int updatedCount = 0;

            foreach (var systemInfo in _systemInfos.Where(s => s.NeedsUpdate))
            {
                UpdateSystemPriority(systemInfo);
                updatedCount++;
            }

            if (updatedCount > 0)
            {
                Debug.Log($"已更新 {updatedCount} 个系统的优先级");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("所有系统的优先级都是最新的");
            }
        }

        private void UpdateSystemPriority(SystemInfo systemInfo)
        {
            // 这里需要根据你的具体实现来更新优先级
            // 可能需要修改源代码文件或使用SerializedObject
            Debug.Log($"更新系统 {systemInfo.SystemId} 的优先级: {systemInfo.CurrentPriority} -> {systemInfo.CalculatedPriority}");

            // 标记为已更新
            systemInfo.CurrentPriority = systemInfo.CalculatedPriority;
        }
    }

    [System.Serializable]
    public class SystemInfo
    {
        public Type Type;
        public string SystemId;
        public int CurrentPriority;
        public int CalculatedPriority;
        public HashSet<string> Dependencies;
        public HashSet<string> Dependents;

        public bool NeedsUpdate => CurrentPriority != CalculatedPriority;
    }
}
#endif