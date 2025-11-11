# InitializeSortSystem

一个轻量级的Unity初始化管理系统，支持按优先级排序执行初始化操作。

## 功能特性

### 🚀 核心功能
- **优先级排序**：支持按数字优先级控制初始化顺序
- **自动执行**：在Awake阶段自动按优先级顺序执行初始化
- **容错处理**：单个初始化失败不影响其他操作执行
- **重复检查**：自动防止重复注册相同的初始化方法

### 📦 技术特点
- 基于Unity MonoBehaviour的生命周期
- 使用C# Action委托封装初始化逻辑
- LINQ排序确保执行顺序
- 异常捕获和日志记录

## 安装说明

### 要求
- Unity 2019.4 LTS 或更高版本
- .NET 4.x 或更高版本

### 安装方法
1. 将 `InitializationManager` 和 `InitializeData` 脚本放入项目
2. 将 `InitializationManager` 组件挂载到场景中的GameObject上

## 快速开始

### 基本用法

```csharp
public class ExampleClass : MonoBehaviour
{
    private void Start()
    {
        // 获取InitializationManager实例
        var initManager = FindObjectOfType<InitializationManager>();
        
        // 注册初始化方法，默认优先级为0
        initManager.RegisterInitialization(InitializePlayer);
        
        // 注册高优先级初始化（数字越大优先级越高）
        initManager.RegisterInitialization(InitializeGameManager, 100);
        
        // 注册低优先级初始化
        initManager.RegisterInitialization(InitializeUI, -10);
    }
    
    private void InitializePlayer()
    {
        Debug.Log("初始化玩家系统");
    }
    
    private void InitializeGameManager()
    {
        Debug.Log("初始化游戏管理器 - 高优先级");
    }
    
    private void InitializeUI()
    {
        Debug.Log("初始化UI系统 - 低优先级");
    }
}
```

### 执行顺序
基于上面的例子，执行顺序为：
1. `InitializeGameManager()` (优先级: 100)
2. `InitializePlayer()` (优先级: 0)  
3. `InitializeUI()` (优先级: -10)

## API 文档

### InitializationManager

#### 方法

##### `RegisterInitialization`
```csharp
public void RegisterInitialization(Action initializeAction, int priority = 0)
```

**参数**：
- `initializeAction` (Action): 要执行的初始化方法
- `priority` (int): 优先级，数字越大越先执行（默认: 0）

**功能**：注册一个初始化方法到执行队列。

#### 生命周期
- **Awake()**: 按优先级顺序执行所有注册的初始化方法
- **OnDestroy()**: 清理初始化队列

### InitializeData

#### 构造函数
```csharp
public InitializeData(Action InitializeAction, int SortLevel)
```

**参数**：
- `InitializeAction`: 初始化动作委托
- `SortLevel`: 排序级别

## 使用场景

### 🎮 游戏初始化
```csharp
// 游戏核心系统优先初始化
initManager.RegisterInitialization(InitSaveSystem, 200);
initManager.RegisterInitialization(InitAudioSystem, 150);
initManager.RegisterInitialization(InitInputSystem, 100);

// 游戏逻辑系统
initManager.RegisterInitialization(InitGameState, 50);
initManager.RegisterInitialization(InitNPCSystem, 30);

// UI系统最后初始化
initManager.RegisterInitialization(InitHUD, 10);
```

### 🔧 系统模块初始化
```csharp
// 基础服务优先
initManager.RegisterInitialization(InitDatabase, 300);
initManager.RegisterInitialization(InitNetwork, 250);

// 业务模块
initManager.RegisterInitialization(InitUserManager, 200);
initManager.RegisterInitialization(InitInventory, 100);
```

## 最佳实践

### ✅ 推荐做法
1. **明确的优先级规划**：提前规划好各系统的初始化顺序
2. **错误处理**：在每个初始化方法内部处理可能的异常
3. **依赖管理**：确保被依赖的系统具有更高的优先级
4. **性能考虑**：避免在初始化中执行耗时操作

### ❌ 避免做法
1. 不要在初始化方法中注册新的初始化
2. 避免循环依赖
3. 不要依赖未注册系统的功能

## 故障排除

### 常见问题

#### Q: 初始化方法没有执行？
**A**: 检查是否在Awake之前注册，确保InitializationManager在场景中

#### Q: 执行顺序不符合预期？
**A**: 确认优先级设置正确，数字越大越先执行

#### Q: 遇到异常导致流程中断？
**A**: 系统已内置异常捕获，单个失败不会影响其他初始化

### 调试技巧
```csharp
// 添加调试日志
initManager.RegisterInitialization(() => 
{
    Debug.Log("开始初始化XXX系统");
    InitializeXXX();
    Debug.Log("完成初始化XXX系统");
}, 50);
```

## 版本历史

### v1.0.0
- 基础初始化排序功能
- 优先级支持
- 异常处理机制
- 重复注册防护

## 技术支持

如有问题或建议，请联系开发团队或提交Issue。

---

**注意**: 确保在所有需要初始化的系统之前注册初始化方法，通常在Start或Awake方法中注册。