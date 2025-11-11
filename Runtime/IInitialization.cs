namespace TechCosmos.InitializeSortSystem.Runtime
{
    public interface IInitialization
    {
        void Initialize(); // 直接提供初始化方法
        int Priority { get; } // 优先级作为属性
    }
}
