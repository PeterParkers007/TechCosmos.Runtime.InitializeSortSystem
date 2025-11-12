# Initialization Dependency Analyzer

一个用于 Unity 的智能初始化系统依赖分析工具，能够自动扫描项目中的初始化系统，分析依赖关系，并提供优化的初始化顺序建议。

## 功能特性

### 🎯 核心功能
- **自动依赖扫描** - 扫描整个项目中的初始化系统
- **依赖关系分析** - 可视化系统间的依赖关系图
- **智能优先级建议** - 基于拓扑排序计算最优初始化顺序
- **一键应用优化** - 自动更新系统优先级字段

### 🔧 技术特性
- **多架构支持** - 支持抽象基类和自定义字段两种架构
- **循环依赖检测** - 自动检测并报告循环依赖问题
- **实时状态显示** - 显示系统更新状态和可操作性
- **场景对象管理** - 自动查找并更新场景中的实例

## 系统架构

### 核心组件

#### 1. 分析器编辑器窗口 (`InitializationDependencyAnalyzer`)
- 提供可视化界面扫描和分析依赖关系
- 显示系统信息、依赖图和建议优先级
- 支持手动和自动应用优化

#### 2. 运行时系统 (`InitializationFactory`, `InitializationManager`)
- 自动预注册所有初始化系统
- 按优先级顺序执行初始化
- 支持 MonoBehaviour 和普通类的统一管理

#### 3. 属性系统
- `[Initialize]` - 标记需要初始化的系统
- `[DependsOn]` - 声明系统依赖关系

### 支持的架构模式

#### 模式一：抽象基类（推荐）
```csharp
[Initialize("GameConfig")]
[DependsOn("DataManager", "EventSystem")]
public class GameConfigSystem : InitializationBehaviour
{
    public override void Initialize()
    {
        // 初始化逻辑
    }
}
```

#### 模式二：自定义字段
```csharp
[Initialize("DataManager")]
public class DataManager : IInitialization
{
    private int _priority = 0;
    public int Priority => _priority;
    
    public void Initialize()
    {
        // 初始化逻辑
    }
}
```

## 安装和使用

### 1. 标记初始化系统
在你的系统类上添加相应的属性：

```csharp
[Initialize("YourSystemId")]
[DependsOn("DependencySystem1", "DependencySystem2")]
public class YourSystem : InitializationBehaviour
{
    public override void Initialize()
    {
        // 你的初始化代码
    }
}
```

### 2. 打开分析器
在 Unity 编辑器中：
```
Tech-Cosmos -> 初始化依赖分析器
```

### 3. 扫描依赖
点击"扫描依赖关系"按钮，工具将自动：
- 扫描所有程序集中的初始化系统
- 分析依赖关系图
- 计算最优初始化顺序
- 显示优先级建议

### 4. 应用优化
- 查看建议的优先级值
- 点击"应用建议值到字段"一键更新
- 或逐个系统点击"应用"按钮

## 依赖关系规则

### 依赖声明
```csharp
// SystemA 依赖于 SystemB 和 SystemC
[Initialize("SystemA")]
[DependsOn("SystemB", "SystemC")]
public class SystemA : InitializationBehaviour
{
    // ...
}
```

### 初始化顺序
- 被依赖的系统优先初始化
- 无依赖的系统最先初始化
- 依赖多的系统较晚初始化

## 界面说明

### 控制面板
- **扫描依赖关系** - 开始分析
- **显示优先级建议** - 切换建议显示
- **自动应用建议值** - 扫描后自动更新
- **应用建议值到字段** - 手动批量更新

### 系统信息卡片
- **系统ID** - 唯一标识符
- **架构类型** - 抽象基类/自定义字段/仅接口
- **当前优先级** - 字段当前值
- **建议优先级** - 计算出的最优值
- **初始化顺序** - 实际执行顺序
- **依赖关系** - 依赖和被依赖的系统

## 高级配置

### 自定义优先级字段名
工具自动识别以下字段名：
- `_priority`
- `priority` 
- `m_priority`
- `initPriority`
- `_initPriority`

### 手动拓扑排序
如需自定义排序逻辑，可修改 `TopologicalSort()` 方法。

## 故障排除

### 常见问题

**Q: 扫描不到系统？**
A: 确保类满足：
- 实现 `IInitialization` 接口
- 有 `[Initialize]` 属性
- 不是抽象类
- 程序集可访问

**Q: 字段更新失败？**
A: 检查：
- 字段存在且类型为 int
- 场景中有系统实例
- 字段可访问性（private 需标记为 [SerializeField]）

**Q: 循环依赖警告？**
A: 检查依赖关系是否存在循环引用，需要打破循环。

## 技术实现

### 核心算法
1. **依赖图构建** - 基于 `[DependsOn]` 属性
2. **拓扑排序** - 确定初始化顺序
3. **优先级分配** - 基于排序结果计算优先级值

### 性能优化
- 缓存扫描结果
- 按需更新字段
- 异步扫描支持

## 版本信息

- **当前版本**: 1.0.0
- **Unity 版本**: 2019.4+
- **依赖**: 无外部依赖
