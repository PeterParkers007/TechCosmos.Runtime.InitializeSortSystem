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
        private bool _showPrioritySuggestions = true;

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
                if (GUILayout.Button("扫描依赖关系", GUILayout.Height(30)))
                {
                    ScanDependencies();
                }

                _showPrioritySuggestions = EditorGUILayout.Toggle("显示优先值建议", _showPrioritySuggestions);
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            // 结果显示
            if (_systemInfos.Count > 0)
            {
                GUILayout.Label($"找到 {_systemInfos.Count} 个系统:", EditorStyles.boldLabel);

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    // 按初始化顺序排序显示
                    foreach (var systemInfo in _systemInfos.Where(s => s != null).OrderBy(s => s.InitializationOrder))
                    {
                        DrawSystemInfo(systemInfo);
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawSystemInfo(SystemInfo systemInfo)
        {
            if (systemInfo == null) return;

            EditorGUILayout.BeginVertical("box");
            {
                // 系统名称和ID
                EditorGUILayout.LabelField(systemInfo.SystemId ?? "未知系统ID", EditorStyles.boldLabel);

                // 类型显示
                if (systemInfo.Type != null)
                {
                    EditorGUILayout.LabelField($"类型: {systemInfo.Type.FullName}");
                }

                // 当前优先级
                EditorGUILayout.LabelField($"当前优先级: {systemInfo.CurrentPriority}");

                // 建议优先级（如果启用）
                if (_showPrioritySuggestions)
                {
                    var style = new GUIStyle(EditorStyles.label);
                    if (systemInfo.SuggestedPriority != systemInfo.CurrentPriority)
                    {
                        style.normal.textColor = Color.yellow;
                        EditorGUILayout.LabelField($"建议优先级: {systemInfo.SuggestedPriority}", style);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"建议优先级: {systemInfo.SuggestedPriority} (无需修改)");
                    }
                }

                // 依赖深度和初始化顺序
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"依赖深度: {systemInfo.DependencyDepth}", GUILayout.Width(120));
                    EditorGUILayout.LabelField($"初始化顺序: {systemInfo.InitializationOrder}", GUILayout.Width(120));
                }
                EditorGUILayout.EndHorizontal();

                // 依赖信息
                if (systemInfo.Dependencies != null && systemInfo.Dependencies.Count > 0)
                {
                    EditorGUILayout.LabelField($"依赖的系统 ({systemInfo.Dependencies.Count}个):", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(string.Join(", ", systemInfo.Dependencies), EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("依赖的系统: 无", EditorStyles.miniLabel);
                }

                // 被依赖信息
                if (systemInfo.Dependents != null && systemInfo.Dependents.Count > 0)
                {
                    EditorGUILayout.LabelField($"被依赖的系统 ({systemInfo.Dependents.Count}个):", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField(string.Join(", ", systemInfo.Dependents), EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("被依赖的系统: 无", EditorStyles.miniLabel);
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
                        .Where(t => t != null &&
                                   t.IsClass &&
                                   !t.IsAbstract &&
                                   t.IsDefined(typeof(InitializeAttribute), false));

                    foreach (var type in types)
                    {
                        var attribute = type.GetCustomAttribute<InitializeAttribute>();
                        if (attribute != null &&
                            !string.IsNullOrEmpty(attribute.InitializationId) &&
                            !systemTypes.ContainsKey(attribute.InitializationId))
                        {
                            systemTypes[attribute.InitializationId] = type;
                        }
                    }
                }
                catch (ReflectionTypeLoadException e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时类型加载失败: {e.Message}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"扫描程序集 {assembly.FullName} 时出错: {e.Message}");
                }
            }

            // 构建系统信息
            foreach (var kvp in systemTypes)
            {
                if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Key))
                {
                    var systemInfo = CreateSystemInfo(kvp.Value, kvp.Key);
                    if (systemInfo != null)
                    {
                        _systemInfos.Add(systemInfo);
                    }
                }
            }

            // 计算依赖关系图
            BuildDependencyGraph();

            // 计算依赖深度和初始化顺序
            CalculateDependencyDepth();

            // 计算建议的优先级值
            CalculateSuggestedPriorities();

            Debug.Log($"依赖分析完成！共扫描到 {_systemInfos.Count} 个系统");
        }

        private SystemInfo CreateSystemInfo(Type type, string systemId)
        {
            if (type == null || string.IsNullOrEmpty(systemId))
            {
                Debug.LogError($"创建SystemInfo失败：类型为null或系统ID为空");
                return null;
            }

            var systemInfo = new SystemInfo
            {
                Type = type,
                SystemId = systemId,
                Dependencies = new HashSet<string>(),
                Dependents = new HashSet<string>(),
                CurrentPriority = 0,
                DependencyDepth = 0,
                InitializationOrder = 0,
                SuggestedPriority = 0
            };

            try
            {
                // 获取当前优先级 - 简化版本，不创建实例
                if (typeof(IInitialization).IsAssignableFrom(type))
                {
                    systemInfo.CurrentPriority = GetPriorityWithoutInstantiation(type);
                }

                // 解析依赖关系
                var dependsOnAttributes = type.GetCustomAttributes<DependsOnAttribute>(false);
                foreach (var attr in dependsOnAttributes)
                {
                    if (attr != null && attr.SystemIds != null)
                    {
                        foreach (var dependency in attr.SystemIds.Where(d => !string.IsNullOrEmpty(d)))
                        {
                            systemInfo.Dependencies.Add(dependency);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"创建 {type.FullName} 的SystemInfo时出错: {e.Message}");
                return null;
            }

            return systemInfo;
        }

        private int GetPriorityWithoutInstantiation(Type type)
        {
            // 尝试通过反射字段获取优先级，避免实例化
            var priorityField = type.GetField("_priority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (priorityField != null && priorityField.FieldType == typeof(int))
            {
                return 0; // 返回默认值
            }

            // 检查其他可能的字段名
            var possibleFieldNames = new[] { "priority", "m_priority", "initPriority", "_initPriority" };
            foreach (var fieldName in possibleFieldNames)
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    return 0; // 返回默认值
                }
            }

            return 0;
        }

        private void BuildDependencyGraph()
        {
            // 构建依赖图
            foreach (var system in _systemInfos.Where(s => s != null))
            {
                if (system.Dependencies == null) continue;

                foreach (var dependency in system.Dependencies)
                {
                    if (string.IsNullOrEmpty(dependency)) continue;

                    var dependentSystem = _systemInfos.FirstOrDefault(s => s != null && s.SystemId == dependency);
                    if (dependentSystem != null)
                    {
                        dependentSystem.Dependents.Add(system.SystemId);
                    }
                    else
                    {
                        Debug.LogWarning($"系统 {system.SystemId} 依赖的 {dependency} 不存在");
                    }
                }
            }
        }

        private void CalculateDependencyDepth()
        {
            // 计算每个系统的依赖深度
            foreach (var system in _systemInfos.Where(s => s != null))
            {
                system.DependencyDepth = CalculateDepth(system, new HashSet<string>());
            }

            // 根据依赖深度分配初始化顺序（深度越大，越先初始化）
            int order = 1;
            foreach (var system in _systemInfos.Where(s => s != null).OrderByDescending(s => s.DependencyDepth))
            {
                system.InitializationOrder = order++;
            }
        }

        private int CalculateDepth(SystemInfo system, HashSet<string> visited)
        {
            if (system == null || string.IsNullOrEmpty(system.SystemId)) return 0;

            if (visited.Contains(system.SystemId))
            {
                Debug.LogError($"发现循环依赖！系统: {system.SystemId}");
                return 0;
            }

            visited.Add(system.SystemId);

            int maxDepth = 0;
            if (system.Dependencies != null)
            {
                foreach (var dependency in system.Dependencies)
                {
                    if (string.IsNullOrEmpty(dependency)) continue;

                    var dependentSystem = _systemInfos.FirstOrDefault(s => s != null && s.SystemId == dependency);
                    if (dependentSystem != null)
                    {
                        int depth = CalculateDepth(dependentSystem, new HashSet<string>(visited)) + 1;
                        maxDepth = Math.Max(maxDepth, depth);
                    }
                }
            }

            visited.Remove(system.SystemId);
            return maxDepth;
        }

        private void CalculateSuggestedPriorities()
        {
            // 基于依赖深度和拓扑排序计算建议的优先级
            // 基本原则：依赖深度越大（依赖链越长），优先级应该越高（越先初始化）

            // 获取拓扑排序结果
            var sortedSystems = TopologicalSort();

            // 分配建议优先级（从高到低）
            int basePriority = 1000;
            int priorityStep = 10; // 每个系统间隔10个优先级单位

            foreach (var system in sortedSystems.Where(s => s != null))
            {
                system.SuggestedPriority = basePriority;
                basePriority -= priorityStep;
            }

            // 确保优先级不会为负数
            var minPriority = sortedSystems.Where(s => s != null).Min(s => s.SuggestedPriority);
            if (minPriority < 0)
            {
                int offset = -minPriority;
                foreach (var system in sortedSystems.Where(s => s != null))
                {
                    system.SuggestedPriority += offset;
                }
            }
        }

        private List<SystemInfo> TopologicalSort()
        {
            var result = new List<SystemInfo>();
            var visited = new HashSet<string>();
            var tempMark = new HashSet<string>();

            foreach (var system in _systemInfos.Where(s => s != null))
            {
                if (!visited.Contains(system.SystemId))
                {
                    TopologicalVisit(system, visited, tempMark, result);
                }
            }

            return result;
        }

        private void TopologicalVisit(SystemInfo system, HashSet<string> visited, HashSet<string> tempMark, List<SystemInfo> result)
        {
            if (system == null || string.IsNullOrEmpty(system.SystemId)) return;

            if (tempMark.Contains(system.SystemId))
            {
                Debug.LogError($"发现循环依赖！系统: {system.SystemId}");
                return;
            }

            if (!visited.Contains(system.SystemId))
            {
                tempMark.Add(system.SystemId);

                // 先访问所有依赖
                if (system.Dependencies != null)
                {
                    foreach (var dependency in system.Dependencies)
                    {
                        if (string.IsNullOrEmpty(dependency)) continue;

                        var dependentSystem = _systemInfos.FirstOrDefault(s => s != null && s.SystemId == dependency);
                        if (dependentSystem != null)
                        {
                            TopologicalVisit(dependentSystem, visited, tempMark, result);
                        }
                    }
                }

                tempMark.Remove(system.SystemId);
                visited.Add(system.SystemId);
                result.Insert(0, system); // 插入到开头，这样依赖项会在后面
            }
        }
    }

    [System.Serializable]
    public class SystemInfo
    {
        public Type Type;
        public string SystemId;
        public int CurrentPriority;
        public int SuggestedPriority; // 建议的优先级值
        public int DependencyDepth;   // 依赖深度（0表示没有依赖）
        public int InitializationOrder; // 初始化顺序（1表示最先初始化）
        public HashSet<string> Dependencies;
        public HashSet<string> Dependents;
    }
}
#endif