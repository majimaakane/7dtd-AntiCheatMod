using System;
using System.IO;

namespace AntiCheatMod
{
    /// <summary>
    /// ログ出力管理クラス。サーバーコンソールと専用ログファイルの両方に記録する。
    /// </summary>
    public static class Logger
    {
        private static string _logFilePath;
        private static bool _enableFileLog = true;
        private static readonly object _lock = new object();

        /// <summary>ログシステムを初期化</summary>
        public static void Init(string modPath, AntiCheatConfig config)
        {
            _enableFileLog = config.EnableFileLog;
            _logFilePath = Path.Combine(modPath, config.LogFileName);

            Log("[AntiCheatMod] ログシステム初期化完了");
        }

        /// <summary>情報ログ</summary>
        public static void Log(string message)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [INFO] {message}";
            UnityEngine.Debug.Log(formatted);
            WriteToFile(formatted);
        }

        /// <summary>警告ログ</summary>
        public static void LogWarning(string message)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARN] {message}";
            UnityEngine.Debug.LogWarning(formatted);
            WriteToFile(formatted);
        }

        /// <summary>エラーログ</summary>
        public static void LogError(string message)
        {
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ERROR] {message}";
            UnityEngine.Debug.LogError(formatted);
            WriteToFile(formatted);
        }

        /// <summary>チート検出ログ（特別なフォーマット）</summary>
        public static void LogCheatDetected(string playerName, string platformId, string cheatType, string details)
        {
            string message = $"[CHEAT] プレイヤー: {playerName} (ID: {platformId}) | 種類: {cheatType} | 詳細: {details}";
            string formatted = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            UnityEngine.Debug.LogWarning(formatted);
            WriteToFile(formatted);
        }

        private static void WriteToFile(string message)
        {
            if (!_enableFileLog || string.IsNullOrEmpty(_logFilePath)) return;

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
                catch
                {
                    // ファイル書き込みエラーはスキップ
                }
            }
        }
    }
}
