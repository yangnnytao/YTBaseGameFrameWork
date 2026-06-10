namespace YGZFrameWork
{
    /// <summary>
    /// 网络 URL 常量
    /// </summary>
    public static class URL
    {
        public static string TIMEURL = "http://api.m.taobao.com/rest/api3.do?api=mtop.common.getTimestamp";

        /// <summary>服务器加载地址 —— 请在发布前通过配置注入或环境变量覆盖</summary>
        public static string LOAD = "http://localhost:8080/load.do"; // 默认本地调试地址，生产环境请覆盖
    }
}
