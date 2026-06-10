using System;
using System.Collections.Generic;
using UnityEngine;

namespace YGZFrameWork
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// 日志接口 —— 支持分级、过滤、异常上报。
    /// </summary>
    public interface ILogger
    {
        void Log(LogLevel level, string message);
        void LogException(Exception ex);
        void SetLevel(LogLevel minLevel);
    }

    /// <summary>
    /// Unity 日志实现 —— 包装 UnityEngine.Debug，支持分级过滤。
    /// </summary>
    public class UnityLogger : ILogger
    {
        public static readonly UnityLogger Instance = new UnityLogger();

        private LogLevel _minLevel = LogLevel.Debug;
        private readonly Queue<string> _recentLogs = new Queue<string>();
        private const int MAX_RECENT_LOGS = 100;

        public void SetLevel(LogLevel minLevel)
        {
            _minLevel = minLevel;
        }

        public void Log(LogLevel level, string message)
        {
            if (level < _minLevel) return;

            string formatted = $"[{DateTime.Now:HH:mm:ss}][{level}] {message}";
            _recentLogs.Enqueue(formatted);
            if (_recentLogs.Count > MAX_RECENT_LOGS) _recentLogs.Dequeue();

            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formatted);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formatted);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(formatted);
                    break;
            }
        }

        public void LogException(Exception ex)
        {
            if (ex == null) return;
            string msg = $"[Exception] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            Log(LogLevel.Error, msg);
            // TODO: 远程上报可在此接入
        }

        /// <summary>获取最近的日志（用于崩溃时上报）</summary>
        public string[] GetRecentLogs()
        {
            return _recentLogs.ToArray();
        }
    }

    /// <summary>
    /// 日志门面 —— 全局统一入口，支持运行时切换 Logger 实现。
    /// </summary>
    public static class YGZLog
    {
        private static ILogger _logger = UnityLogger.Instance;
        private static LogLevel _minLevel = LogLevel.Debug;

        /// <summary>设置日志实现（可替换为远程上报 Logger）</summary>
        public static void SetLogger(ILogger logger)
        {
            _logger = logger ?? UnityLogger.Instance;
        }

        /// <summary>设置最低输出级别</summary>
        public static void SetLevel(LogLevel level)
        {
            _minLevel = level;
            _logger?.SetLevel(level);
        }

        public static void Debug(string message) => _logger?.Log(LogLevel.Debug, message);
        public static void Info(string message) => _logger?.Log(LogLevel.Info, message);
        public static void Warning(string message) => _logger?.Log(LogLevel.Warning, message);
        public static void Error(string message) => _logger?.Log(LogLevel.Error, message);
        public static void Fatal(string message) => _logger?.Log(LogLevel.Fatal, message);

        public static void Exception(Exception ex) => _logger?.LogException(ex);
    }
}
