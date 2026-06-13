using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AntiCheatMod
{
    /// <summary>
    /// AntiCheatMod エントリポイント。
    /// IModApi インターフェースを実装し、ModEventsにハンドラを登録する。
    /// </summary>
    public class Api : IModApi
    {
        private static AntiCheatConfig _config;
        private static ViolationHandler _violationHandler;
        private static CommandInterceptor _commandInterceptor;
        private static PlayerMonitor _playerMonitor;
        private static string _modPath;
        private static bool _initialized = false;

        /// <summary>Mod初期化メソッド（エントリポイント）</summary>
        public void InitMod(Mod _modInstance)
        {
            try
            {
                // Modのパスを取得
                _modPath = _modInstance.Path;

                // ログシステムの仮初期化
                Logger.Init(_modPath, new AntiCheatConfig());

                Logger.Log("===================================");
                Logger.Log("[AntiCheatMod] Mod初期化開始...");
                Logger.Log("===================================");

                // 設定読み込み
                _config = AntiCheatConfig.Load(_modPath);

                // ログシステムを正式設定で再初期化
                Logger.Init(_modPath, _config);

                // コンポーネント初期化
                _violationHandler = new ViolationHandler(_config);
                _commandInterceptor = new CommandInterceptor(_config, _violationHandler);
                _playerMonitor = new PlayerMonitor(_config, _violationHandler);

                // イベント登録
                ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
                ModEvents.GameShutdown.RegisterHandler(OnGameShutdown);
                ModEvents.PlayerSpawnedInWorld.RegisterHandler(OnPlayerSpawnedInWorld);
                ModEvents.PlayerDisconnected.RegisterHandler(OnPlayerDisconnected);
                ModEvents.ChatMessage.RegisterHandler(OnChatMessage);
                ModEvents.GameUpdate.RegisterHandler(OnGameUpdate);

                Logger.Log("[AntiCheatMod] イベントハンドラ登録完了");
                Logger.Log($"[AntiCheatMod] 管理者レベル: {_config.AdminPermissionLevel} 以下は免除");
                Logger.Log($"[AntiCheatMod] コマンドブロック: {_config.BlockConsoleCommands}");
                Logger.Log($"[AntiCheatMod] 飛行検出: {_config.DetectFlying}");
                Logger.Log($"[AntiCheatMod] テレポート検出: {_config.DetectTeleport}");
                Logger.Log($"[AntiCheatMod] スピードハック検出: {_config.DetectSpeedHack}");
                Logger.Log($"[AntiCheatMod] ゴッドモード検出: {_config.DetectGodMode}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AntiCheatMod] 初期化エラー: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>ゲーム開始完了時のハンドラ</summary>
        private void OnGameStartDone(ref ModEvents.SGameStartDoneData data)
        {
            _initialized = true;
            Logger.Log("[AntiCheatMod] ゲーム開始完了 - チーター監視を開始します");
        }

        /// <summary>ゲーム終了時のハンドラ</summary>
        private void OnGameShutdown(ref ModEvents.SGameShutdownData data)
        {
            _initialized = false;
            Logger.Log("[AntiCheatMod] ゲーム終了 - チーター監視を停止します");
        }

        /// <summary>プレイヤーがワールドにスポーンした時のハンドラ</summary>
        private void OnPlayerSpawnedInWorld(ref ModEvents.SPlayerSpawnedInWorldData data)
        {
            if (!_initialized || data.ClientInfo == null) return;

            try
            {
                bool isAdmin = AdminChecker.IsAdmin(data.ClientInfo, _config);
                int permLevel = AdminChecker.GetPermissionLevel(data.ClientInfo);
                string platformId = data.ClientInfo.PlatformId?.ReadablePlatformUserIdentifier ?? "Unknown";

                Logger.Log($"[AntiCheatMod] プレイヤー参加: {data.ClientInfo.playerName} " +
                          $"(ID: {platformId}, 権限: {permLevel}, 管理者: {isAdmin})");

                // 管理者でない場合のみ監視開始
                if (!isAdmin)
                {
                    _playerMonitor.StartTracking(data.ClientInfo.entityId);
                    Logger.Log($"[AntiCheatMod] {data.ClientInfo.playerName} の監視を開始");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AntiCheatMod] PlayerSpawnedInWorld エラー: {ex.Message}");
            }
        }

        /// <summary>プレイヤー切断時のハンドラ</summary>
        private void OnPlayerDisconnected(ref ModEvents.SPlayerDisconnectedData data)
        {
            if (data.ClientInfo == null) return;

            try
            {
                _playerMonitor.StopTracking(data.ClientInfo.entityId);
                _violationHandler.ClearPlayer(data.ClientInfo.entityId);
                Logger.Log($"[AntiCheatMod] {data.ClientInfo.playerName} が切断 - 監視終了");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AntiCheatMod] PlayerDisconnected エラー: {ex.Message}");
            }
        }

        /// <summary>チャットメッセージハンドラ</summary>
        private ModEvents.EModEventResult OnChatMessage(ref ModEvents.SChatMessageData data)
        {
            if (!_initialized || data.ClientInfo == null) return ModEvents.EModEventResult.Continue;
            if (string.IsNullOrEmpty(data.Message)) return ModEvents.EModEventResult.Continue;

            try
            {
                // チャットコマンドのチェック
                if (_commandInterceptor.CheckChatMessage(data.ClientInfo, data.Message))
                {
                    return ModEvents.EModEventResult.StopHandlersAndVanilla; // メッセージをブロック
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AntiCheatMod] ChatMessage エラー: {ex.Message}");
            }

            return ModEvents.EModEventResult.Continue; // メッセージを許可
        }

        /// <summary>ゲーム更新ハンドラ（毎フレーム呼ばれる）</summary>
        private void OnGameUpdate(ref ModEvents.SGameUpdateData data)
        {
            if (!_initialized) return;

            try
            {
                // プレイヤー監視の更新（内部で間隔制御済み）
                _playerMonitor.Update();
            }
            catch (Exception)
            {
                // GameUpdateのエラーは頻発する可能性があるので、ログを抑制
            }
        }
    }
}
