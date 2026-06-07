/// <summary>
/// 线程安全单例基类（非 Mono）
/// 统一项目中所有非 MonoBehaviour 单例的实现方式。
/// </summary>
public class Singleton<T> where T : class, new()
{
    private static readonly object _lock = new object();
    private static volatile T _instance;

    /// <summary>静态实例属性（双重检查锁，线程安全）</summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new T();
                    }
                }
            }
            return _instance;
        }
    }

    /// <summary>获取实例（兼容旧接口）</summary>
    public static T GetInstance()
    {
        return Instance;
    }

    public virtual void InitDataM() { }
    public virtual void DestroyM() { }
}
