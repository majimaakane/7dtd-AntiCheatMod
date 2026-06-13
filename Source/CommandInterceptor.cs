using System;
using System.Collections.Generic;

namespace AntiCheatMod
{
    /// <summary>
    /// コンソールコマンドを傍受し、非管理者による不正コマンド実行を防止する。
    /// </summary>
    public class CommandInterceptor
    {
        private readonly AntiCheatConfig _config;
        private readonly ViolationHandler _violationHandler;

        /// <summary>非管理者に対してブロックするコマンドのリスト</summary>
        private static readonly HashSet<string> BlockedCommands = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // クリエイティブ・デバッグモード
            "cm",
            "creativemenu",
            "dm",
            "debugmenu",
            "debug",

            // アイテムスポーン
            "give",
            "giveselfxp",
            "gi",

            // バフ・デバフ操作
            "buff",
            "debuff",
            "buffplayer",
            "debuffplayer",

            // エンティティ・物資スポーン
            "spawnentity",
            "se",
            "spawnsupply",
            "spawnsupplycrate",

            // テレポート
            "teleport",
            "tp",
            "teleportplayer",
            "tele",

            // ワールド操作
            "settime",
            "st",
            "weather",
            "killall",
            "kill",

            // その他の危険なコマンド
            "shutdown",
            "ban",
            "kick",
            "whitelist",
            "admin",
            "removequest",
            "givequest",
            "setgamepref",
            "setgamestat",
            "ggs",
            "sgp",
            "exhausted",
            "thirsty",
            "starving",
            "chunkcache",
            "shownexthordetime",
            "enablescope",
            "spectrum",
        };

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
        /// コンソールコマンド実行時のチェック。
        /// 管理者以外がブロック対象コマンドを実行しようとした場合に違反処理を行う。
        /// trueを返すとコマンドをブロックする。
        /// </summary>
        public bool CheckCommand(ClientInfo clientInfo, string command)
        {
            if (!_config.BlockConsoleCommands) return false;
            if (clientInfo == null) return false;

            // 管理者はすべて許可
            if (AdminChecker.IsAdmin(clientInfo, _config)) return false;

            // コマンド名を抽出（最初の単語）
            string cmdName = ExtractCommandName(command);
            if (string.IsNullOrEmpty(cmdName)) return false;

            // ブロック対象コマンドかチェック
            if (BlockedCommands.Contains(cmdName))
            {
                Logger.Log($"[CommandInterceptor] ブロック: {clientInfo.playerName} がコマンド '{command}' を実行しようとしました");
                _violationHandler.HandleViolation(clientInfo, ViolationType.ConsoleCommand,
                    $"コマンド実行試行: {command}");
                return true; // コマンドをブロック
            }

            return false;
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

        /// <summary>コマンド文字列からコマンド名を抽出</summary>
        private string ExtractCommandName(string command)
        {
            if (string.IsNullOrEmpty(command)) return null;
            string trimmed = command.Trim();
            int spaceIdx = trimmed.IndexOf(' ');
            return spaceIdx > 0 ? trimmed.Substring(0, spaceIdx) : trimmed;
        }
    }
}
