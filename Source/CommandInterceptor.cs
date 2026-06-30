using System;

namespace AntiCheatMod
{
    /// <summary>
    /// チャット経由のチートコマンドを傍受し、非管理者による不正操作を防止する。
    /// 注: 7DTD ModAPI にはサーバーコンソールコマンドを傍受するイベントが存在しないため、
    ///     非管理者プレイヤーがチャットから送信するコマンド形式のメッセージのみ対象。
    /// </summary>
    public class CommandInterceptor
    {
        private readonly AntiCheatConfig _config;
        private readonly ViolationHandler _violationHandler;

        /// <summary>チャットコマンドとしてブロックするプレフィックス</summary>
        private static readonly string[] BlockedChatPrefixes = new string[]
        {
            "/give",
            "/tp",
            "/teleport",
            "/god",
            "/fly",
            "/creative",
            "/debug",
            "/spawn",
        };

        public CommandInterceptor(AntiCheatConfig config, ViolationHandler violationHandler)
        {
            _config = config;
            _violationHandler = violationHandler;
        }

        /// <summary>
        /// チャットメッセージを監視し、チートコマンドのプレフィックスをチェック。
        /// </summary>
        public bool CheckChatMessage(ClientInfo clientInfo, string message)
        {
            if (!_config.BlockConsoleCommands) return false;
            if (clientInfo == null || string.IsNullOrEmpty(message)) return false;

            // 管理者はすべて許可
            if (AdminChecker.IsAdmin(clientInfo, _config)) return false;

            string lowerMsg = message.ToLower().Trim();

            foreach (string prefix in BlockedChatPrefixes)
            {
                if (lowerMsg == prefix || lowerMsg.StartsWith(prefix + " "))
                {
                    Logger.Log($"[CommandInterceptor] チャットコマンドブロック: {clientInfo.playerName} が '{message}' を送信");
                    _violationHandler.HandleViolation(clientInfo, ViolationType.ConsoleCommand,
                        $"チャットコマンド: {message}");
                    return true;
                }
            }

            return false;
        }


    }
}
