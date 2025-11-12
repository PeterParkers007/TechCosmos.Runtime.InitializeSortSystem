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

        [MenuItem("Tech-Cosmos/初始化依赖分析器")]
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

                // 统计信息
                var abstractBaseCount = _systemInfos.Count(s => s.UsesAbstractBase);
                var hasFieldSupportCount = _systemInfos.Count(s => s.HasPriorityField);

                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField($"使用抽象基类: {abstractBaseCount}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"有字段支持: {hasFieldSupportCount}", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndHorizontal();

                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                {
                    // 按建议优先级排序显示（高优先级在前）
                    foreach (var systemInfo in _systemInfos.Where(s => s != null).OrderByDescending(s => s.SuggestedPriority))
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
                    string typeInfo = $"类型: {systemInfo.Type.Name}";
                    if (systemInfo.UsesAbstractBase)
                    {
                        typeInfo += " ✓ (InitializationBehaviour)";
                    }
                    EditorGUILayout.LabelField(typeInfo);
                }

                // 字段支持状态
                EditorGUILayout.BeginHorizontal();
                {
                    if (systemInfo.UsesAbstractBase)
                    {
                        EditorGUILayout.LabelField("架构: 抽象基类", GetMiniLabelStyle(Color.green));
                    }
                    else if (systemInfo.HasPriorityField)
                    {
                        EditorGUILayout.LabelField("架构: 自定义字段", GetMiniLabelStyle(Color.blue));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("架构: 仅接口", GetMiniLabelStyle(Color.red));
                    }

                    if (systemInfo.HasPriorityField)
                    {
                        EditorGUILayout.LabelField("字段: 支持", GetMiniLabelStyle(Color.green));
                    }
                    else
                    {
                        EditorGUILayout.LabelField("字段: 不支持", GetMiniLabelStyle(Color.yellow));
                    }
                }
                EditorGUILayout.EndHorizontal();

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

                        // 如果支持字段，显示修改建议
                        if (systemInfo.HasPriorityField)
                        {
                            EditorGUILayout.LabelField($"提示: 设置 _priority = {systemInfo.SuggestedPriority}", EditorStyles.miniLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField("提示: 建议继承 InitializationBehaviour 以获得字段支持", EditorStyles.miniLabel);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"建议优先级: {systemInfo.SuggestedPriority} (无需修改)");
                    }
                }

                // 初始化顺序
                EditorGUILayout.LabelField($"初始化顺序: {systemInfo.InitializationOrder}");

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

                // 架构建议
                if (!systemInfo.UsesAbstractBase && !systemInfo.HasPriorityField)
                {
                    EditorGUILayout.HelpBox("建议继承 InitializationBehaviour 以获得更好的字段支持", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        private GUIStyle GetMiniLabelStyle(Color color)
        {
            var style = new GUIStyle(EditorStyles.miniLabel);
            style.normal.textColor = color;
            return style;
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

            // 计算建议的优先级值和初始化顺序
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
                InitializationOrder = 0,
                SuggestedPriority = 0,
                UsesAbstractBase = false,
                HasPriorityField = false
            };

            try
            {
                // 检查架构类型和字段支持
                CheckArchitectureSupport(systemInfo, type);

                // 获取当前优先级
                systemInfo.CurrentPriority = GetCurrentPriority(systemInfo, type);

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

        private void CheckArchitectureSupport(SystemInfo systemInfo, Type type)
        {
            // 检查是否继承自抽象基类
            systemInfo.UsesAbstractBase = typeof(InitializationBehaviour).IsAssignableFrom(type);

            // 检查是否有优先级字段支持
            if (systemInfo.UsesAbstractBase)
            {
                // 继承自抽象基类，肯定有字段支持
                systemInfo.HasPriorityField = true;
            }
            else
            {
                // 检查自定义字段
                systemInfo.HasPriorityField = CheckForPriorityField(type);
            }
        }

        private bool CheckForPriorityField(Type type)
        {
            // 检查是否有_priority字段或其他常见字段名
            var fieldNames = new[] { "_priority", "priority", "m_priority", "initPriority", "_initPriority" };
            foreach (var fieldName in fieldNames)
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    return true;
                }
            }
            return false;
        }

        private int GetCurrentPriority(SystemInfo systemInfo, Type type)
        {
            if (!typeof(IInitialization).IsAssignableFrom(type))
            {
                return 0;
            }

            // 对于有字段支持的类，我们可以尝试获取字段的默认值
            if (systemInfo.HasPriorityField)
            {
                return GetPriorityFromField(type);
            }

            // 对于没有字段支持的类，返回默认值
            return 0;
        }

        private int GetPriorityFromField(Type type)
        {
            // 尝试获取_priority字段的默认值
            var priorityField = type.GetField("_priority", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (priorityField != null)
            {
                // 对于MonoBehaviour，我们可以创建一个临时实例来获取默认值
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                {
                    try
                    {
                        var tempGameObject = new GameObject("TempPriorityCheck");
                        var component = tempGameObject.AddComponent(type);
                        var value = (int)priorityField.GetValue(component);
                        UnityEngine.Object.DestroyImmediate(tempGameObject);
                        return value;
                    }
                    catch
                    {
                        // 如果创建实例失败，返回默认值0
                        return 0;
                    }
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

        private void CalculateSuggestedPriorities()
        {
            // 被依赖的系统需要更高的优先级（先初始化）
            var sortedSystems = TopologicalSort();

            // 分配建议优先级（先初始化的系统获得高优先级）
            int basePriority = 1000;
            int priorityStep = 10;

            foreach (var system in sortedSystems.Where(s => s != null))
            {
                system.SuggestedPriority = basePriority;
                basePriority -= priorityStep;
            }

            // 分配初始化顺序
            int order = 1;
            foreach (var system in sortedSystems.Where(s => s != null))
            {
                system.InitializationOrder = order++;
            }

            Debug.Log($"优先级计算完成：最高优先级 {sortedSystems.First().SuggestedPriority}，最低优先级 {sortedSystems.Last().SuggestedPriority}");
        }

        private List<SystemInfo> TopologicalSort()
        {
            var result = new List<SystemInfo>();
            var visited = new HashSet<string>();
            var tempMark = new HashSet<string>();

            // 找出所有没有依赖的系统（这些应该最先初始化）
            var noDependencySystems = _systemInfos.Where(s => s != null &&
                (s.Dependencies == null || s.Dependencies.Count == 0)).ToList();

            // 从没有依赖的系统开始遍历
            foreach (var system in noDependencySystems)
            {
                if (!visited.Contains(system.SystemId))
                {
                    TopologicalVisit(system, visited, tempMark, result);
                }
            }

            // 处理可能有循环依赖的剩余系统
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

                // 先访问所有依赖这个系统的系统（被依赖的系统应该先初始化）
                if (system.Dependents != null)
                {
                    foreach (var dependentId in system.Dependents)
                    {
                        if (string.IsNullOrEmpty(dependentId)) continue;

                        var dependentSystem = _systemInfos.FirstOrDefault(s => s != null && s.SystemId == dependentId);
                        if (dependentSystem != null && !visited.Contains(dependentSystem.SystemId))
                        {
                            TopologicalVisit(dependentSystem, visited, tempMark, result);
                        }
                    }
                }

                tempMark.Remove(system.SystemId);
                visited.Add(system.SystemId);

                // 被依赖的系统应该排在前面（先初始化）
                result.Insert(0, system);
            }
        }
    }

    [System.Serializable]
    public class SystemInfo
    {
        public Type Type;
        public string SystemId;
        public int CurrentPriority;
        public int SuggestedPriority;
        public int InitializationOrder;
        public HashSet<string> Dependencies;
        public HashSet<string> Dependents;

        // 新增字段：架构支持信息
        public bool UsesAbstractBase;  // 是否使用抽象基类
        public bool HasPriorityField;  // 是否有字段支持
    }
}
#endif