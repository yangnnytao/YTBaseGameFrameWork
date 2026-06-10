using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 协程运行器 —— 为非 MonoBehaviour 类提供 StartCoroutine 能力。
    /// 
    /// 挂载在场景中的持久 GameObject 上，全局唯一。
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("CoroutineRunner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineRunner>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
    }
}
