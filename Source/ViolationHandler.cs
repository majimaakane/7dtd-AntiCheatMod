using System;
using System.Collections.Generic;

namespace AntiCheatMod
{
    /// <summary>
    /// 違反の種類
    /// </summary>
    public enum ViolationType
    {
        ConsoleCommand,
        Flying,
        Teleport,
        SpeedHack,
        GodMode
    }

    /// <summary>
    /// 違反処理クラス。警告・キック・BANを管理する。
    /// </summary>
    public class ViolationHandler
    {
        private readonly AntiCheatConfig _config;

        /// <summary>プレイヤーごとの違反カウント</summary>
        private readonly Dictionary<int, PlayerViolationData> _violations = new Dictionary<int, PlayerViolationData>();

        public ViolationHandler(AntiCheatConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 違反を記録し、適切なアクションを実行する。
        /// </summary>
        public void HandleViolation(ClientInfo clientInfo, ViolationType type, string details)
        {
            if (clientInfo == null) return;

            int entityId = clientInfo.entityId;
            string playerName = clientInfo.playerName;
            string platformId = clientInfo.PlatformId?.ReadablePlatformUserIdentifier ?? "Unknown";

            // ログに記録
            Logger.LogCheatDetected(playerName, platformId, type.ToString(), details);

            // 違反データ取得・作成
            if (!_violations.TryGetValue(entityId, out PlayerViolationData data))
            {
                data = new PlayerViolationData();
                _violations[entityId] = data;
            }

            // リセット時間が設定されている場合、古い違反をリセット
            if (_config.ViolationResetMinutes > 0)
            {
                TimeSpan elapsed = DateTime.Now - data.LastViolationTime;
                if (elapsed.TotalMinutes >= _config.ViolationResetMinutes)
                {
                    data.WarningCount = 0;
                    data.KickCount = 0;
                }
            }

            data.WarningCount++;
            data.LastViolationTime = DateTime.Now;

            // 警告メッセージ
            string violationName = GetViolationName(type);

            // BANチェック
            if (_config.KicksBeforeBan > 0 && data.KickCount >= _config.KicksBeforeBan)
            {
                BanPlayer(clientInfo, violationName);
                return;
            }

            // キックチェック
            if (data.WarningCount >= _config.MaxWarnings)
            {
                KickPlayer(clientInfo, violationName);
                data.WarningCount = 0;
                if (_config.ResetOnKick)
                {
                    data.KickCount = 0; // 完全リセット：BANカウントも含め初期化
                }
                else
                {
                    data.KickCount++; // BANに向けて累積
                }
                return;
            }

            // 警告
            if (_config.WarnPlayer)
            {
                WarnPlayer(clientInfo, violationName, data.WarningCount);
            }

            // 管理者通知
            if (_config.NotifyAdmins)
            {
                NotifyAdmins(playerName, violationName, details);
            }
        }

        /// <summary>プレイヤーに警告メッセージを送信</summary>
        private void WarnPlayer(ClientInfo clientInfo, string violationName, int warningCount)
        {
            int remaining = _config.MaxWarnings - warningCount;
            string message = $"[AntiCheat] 警告: {violationName}が検出されました。 (残り{remaining}回でキックされます)";

            // プレイヤーにチャットメッセージを送信
            clientInfo.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(
                EChatType.Whisper, -1, message, null, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported));

            Logger.Log($"[ViolationHandler] {clientInfo.playerName} に警告送信: {message}");
        }

        /// <summary>プレイヤーをキック</summary>
        private void KickPlayer(ClientInfo clientInfo, string violationName)
        {
            string reason = $"[AntiCheat] チート行為を検出: {violationName}";
            Logger.Log($"[ViolationHandler] {clientInfo.playerName} をキックしました: {reason}");

            // 管理者に通知
            if (_config.NotifyAdmins)
            {
                NotifyAdmins(clientInfo.playerName, violationName, "プレイヤーをキックしました");
            }

            // キック実行
            GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.ManualKick, 0, default, reason));
        }

        /// <summary>プレイヤーをBAN</summary>
        private void BanPlayer(ClientInfo clientInfo, string violationName)
        {
            string reason = $"[AntiCheat] 繰り返しのチート行為: {violationName}";
            Logger.Log($"[ViolationHandler] {clientInfo.playerName} をBANしました: {reason}");

            // 管理者に通知
            if (_config.NotifyAdmins)
            {
                NotifyAdmins(clientInfo.playerName, violationName, "プレイヤーをBANしました");
            }

            // BAN実行（バニラのbanコマンド相当）
            GameManager.Instance.adminTools.Blacklist.AddBan(
                clientInfo.playerName,
                clientInfo.PlatformId,
                new DateTime(2099, 12, 31),
                reason);

            // キック
            GameUtils.KickPlayerForClientInfo(clientInfo, new GameUtils.KickPlayerData(GameUtils.EKickReason.Banned, 0, default, reason));
        }

        /// <summary>オンラインの管理者全員にチャット通知</summary>
        private void NotifyAdmins(string playerName, string violationType, string details)
        {
            string message = $"[AntiCheat] {playerName} : {violationType} ({details})";

            var clients = ConnectionManager.Instance.Clients.List;
            foreach (var admin in clients)
            {
                if (AdminChecker.IsAdmin(admin, _config))
                {
                    admin.SendPackage(NetPackageManager.GetPackage<NetPackageChat>().Setup(
                        EChatType.Whisper, -1, message, null, EMessageSender.Server, GeneratedTextManager.BbCodeSupportMode.Supported));
                }
            }
        }

        /// <summary>プレイヤーの違反データをクリア（切断時等）</summary>
        public void ClearPlayer(int entityId)
        {
            _violations.Remove(entityId);
        }

        /// <summary>違反種類の日本語名を取得</summary>
        private string GetViolationName(ViolationType type)
        {
            switch (type)
            {
                case ViolationType.ConsoleCommand: return "不正コマンド使用";
                case ViolationType.Flying: return "飛行チート";
                case ViolationType.Teleport: return "テレポート";
                case ViolationType.SpeedHack: return "スピードハック";
                case ViolationType.GodMode: return "ゴッドモード";
                default: return "不明な違反";
            }
        }

        /// <summary>プレイヤーの違反追跡データ</summary>
        private class PlayerViolationData
        {
            public int WarningCount { get; set; } = 0;
            public int KickCount { get; set; } = 0;
            public DateTime LastViolationTime { get; set; } = DateTime.Now;
        }
    }
}
