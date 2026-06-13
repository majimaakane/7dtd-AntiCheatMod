using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntiCheatMod
{
    /// <summary>
    /// プレイヤーの行動を監視し、不正な移動や無敵状態を検出する。
    /// </summary>
    public class PlayerMonitor
    {
        private readonly AntiCheatConfig _config;
        private readonly ViolationHandler _violationHandler;

        /// <summary>プレイヤーごとの監視データ</summary>
        private readonly Dictionary<int, PlayerTrackingData> _tracking = new Dictionary<int, PlayerTrackingData>();

        private float _lastCheckTime = 0f;

        public PlayerMonitor(AntiCheatConfig config, ViolationHandler violationHandler)
        {
            _config = config;
            _violationHandler = violationHandler;
        }

        /// <summary>
        /// プレイヤーの監視を開始する。
        /// </summary>
        public void StartTracking(int entityId)
        {
            if (!_tracking.ContainsKey(entityId))
            {
                _tracking[entityId] = new PlayerTrackingData();
            }
        }

        /// <summary>
        /// プレイヤーの監視を停止する。
        /// </summary>
        public void StopTracking(int entityId)
        {
            _tracking.Remove(entityId);
        }

        /// <summary>
        /// GameUpdateごとに呼ばれる定期監視メソッド。
        /// </summary>
        public void Update()
        {
            float currentTime = Time.time;

            // 監視間隔チェック
            if (currentTime - _lastCheckTime < _config.MonitorInterval) return;
            _lastCheckTime = currentTime;

            World world = GameManager.Instance.World;
            if (world == null) return;

            var clients = ConnectionManager.Instance.Clients.List;
            if (clients == null) return;

            foreach (var clientInfo in clients)
            {
                if (clientInfo == null) continue;

                // 管理者は監視対象外
                if (AdminChecker.IsAdmin(clientInfo, _config)) continue;

                int entityId = clientInfo.entityId;
                if (entityId == -1) continue;

                EntityPlayer player = world.GetEntity(entityId) as EntityPlayer;
                if (player == null) continue;

                // トラッキングデータ取得
                if (!_tracking.TryGetValue(entityId, out PlayerTrackingData data))
                {
                    data = new PlayerTrackingData();
                    _tracking[entityId] = data;
                }

                Vector3 currentPos = player.GetPosition();
                float deltaTime = currentTime - data.LastCheckTime;

                // 初回は位置を記録するだけ
                if (!data.HasPreviousPosition)
                {
                    data.PreviousPosition = currentPos;
                    data.HasPreviousPosition = true;
                    data.LastCheckTime = currentTime;
                    continue;
                }

                // --- テレポート検出 ---
                if (_config.DetectTeleport)
                {
                    CheckTeleport(clientInfo, data, currentPos, deltaTime);
                }

                // --- スピードハック検出 ---
                if (_config.DetectSpeedHack)
                {
                    CheckSpeedHack(clientInfo, data, currentPos, deltaTime);
                }

                // --- 飛行検出 ---
                if (_config.DetectFlying)
                {
                    CheckFlying(clientInfo, player, data, currentTime);
                }

                // --- ゴッドモード検出 ---
                if (_config.DetectGodMode)
                {
                    CheckGodMode(clientInfo, player);
                }

                // 位置を更新
                data.PreviousPosition = currentPos;
                data.LastCheckTime = currentTime;
            }
        }

        /// <summary>テレポート検出</summary>
        private void CheckTeleport(ClientInfo clientInfo, PlayerTrackingData data, Vector3 currentPos, float deltaTime)
        {
            if (deltaTime <= 0) return;

            float distance = Vector3.Distance(data.PreviousPosition, currentPos);

            // 閾値を超える距離の瞬間移動を検出
            // ただし、ゲーム内の正常なテレポート（ベッドロール等）を考慮して、
            // 一定時間内に複数回発生した場合のみ違反とする
            if (distance > _config.TeleportDistance)
            {
                // 落下による高低差を除外（Y方向の大きな変化のみは許可）
                float horizontalDist = new Vector2(
                    currentPos.x - data.PreviousPosition.x,
                    currentPos.z - data.PreviousPosition.z).magnitude;

                if (horizontalDist > _config.TeleportDistance * 0.5f)
                {
                    data.TeleportCount++;
                    if (data.TeleportCount >= 2) // 2回以上で違反
                    {
                        _violationHandler.HandleViolation(clientInfo, ViolationType.Teleport,
                            $"異常な移動検出 距離: {distance:F1}m ({data.PreviousPosition} → {currentPos})");
                        data.TeleportCount = 0;
                    }
                }
            }
            else
            {
                // 正常な移動ならカウントを徐々にリセット
                if (data.TeleportCount > 0)
                    data.TeleportCount--;
            }
        }

        /// <summary>スピードハック検出</summary>
        private void CheckSpeedHack(ClientInfo clientInfo, PlayerTrackingData data, Vector3 currentPos, float deltaTime)
        {
            if (deltaTime <= 0.1f) return; // 非常に短い間隔は無視

            // 水平方向の移動速度を計算
            float horizontalDist = new Vector2(
                currentPos.x - data.PreviousPosition.x,
                currentPos.z - data.PreviousPosition.z).magnitude;

            float speed = horizontalDist / deltaTime;

            // 7DTDの通常の最大走行速度は約6-7 m/s（ミニバイクなどの乗り物除く）
            // 乗り物を考慮して基準速度を高めに設定
            float maxNormalSpeed = 25f; // 乗り物考慮
            float threshold = maxNormalSpeed * _config.SpeedMultiplier;

            if (speed > threshold)
            {
                data.SpeedViolationCount++;
                if (data.SpeedViolationCount >= 3) // 3回連続で違反
                {
                    _violationHandler.HandleViolation(clientInfo, ViolationType.SpeedHack,
                        $"異常な移動速度: {speed:F1} m/s (閾値: {threshold:F1} m/s)");
                    data.SpeedViolationCount = 0;
                }
            }
            else
            {
                data.SpeedViolationCount = Math.Max(0, data.SpeedViolationCount - 1);
            }
        }

        /// <summary>飛行検出</summary>
        private void CheckFlying(ClientInfo clientInfo, EntityPlayer player, PlayerTrackingData data, float currentTime)
        {
            // プレイヤーが地面に接地しているかチェック
            bool isGrounded = player.onGround;

            // 水中・はしご付近は除外
            bool isInWater = player.IsInWater();

            // ワールドのブロックを確認してはしご判定（一つ飛ばし設置にも対応するため上下2ブロック範囲）
            World world = GameManager.Instance.World;
            bool isNearLadder = world != null && IsNearLadder(world, player);

            // 足元3ブロック以内に固体ブロックがある場合はジャンプしながらブロック設置中とみなす
            bool isNearGround = world != null && IsNearGround(world, player);

            if (!isGrounded && !isInWater && !isNearLadder && !isNearGround)
            {
                if (data.AirborneStartTime <= 0)
                {
                    data.AirborneStartTime = currentTime;
                }
                else
                {
                    float airborneTime = currentTime - data.AirborneStartTime;

                    // 落下は通常数秒以内に着地する
                    // ジャイロコプター等の乗り物飛行は考慮が必要
                    if (airborneTime > _config.FlyingDuration)
                    {
                        // 乗り物に乗っているかチェック
                        bool isOnVehicle = player.AttachedToEntity != null;

                        if (!isOnVehicle)
                        {
                            data.FlyViolationCount++;
                            if (data.FlyViolationCount >= 2)
                            {
                                _violationHandler.HandleViolation(clientInfo, ViolationType.Flying,
                                    $"空中滞在時間: {airborneTime:F1}秒");
                                data.FlyViolationCount = 0;
                                data.AirborneStartTime = currentTime; // リセットして再監視
                            }
                        }
                    }
                }
            }
            else
            {
                data.AirborneStartTime = 0;
                data.FlyViolationCount = Math.Max(0, data.FlyViolationCount - 1);
            }
        }

        /// <summary>足元5ブロック以内に非エアブロックがあるか確認する（ジャンプしながらブロック設置の誤検出対策）</summary>
        private bool IsNearGround(World world, EntityPlayer player)
        {
            Vector3 pos = player.GetPosition();
            int px = Mathf.FloorToInt(pos.x);
            int py = Mathf.FloorToInt(pos.y);
            int pz = Mathf.FloorToInt(pos.z);

            // IsSolidCubeは土・石等の通常ブロックでfalseになる場合があるため使わない
            // 5ブロックまで拡張してジャンプ頂点でも確実に検出できるようにする
            for (int dy = 1; dy <= 5; dy++)
            {
                BlockValue bv = world.GetBlock(new Vector3i(px, py - dy, pz));
                if (!bv.isair && bv.Block != null)
                    return true;
            }
            return false;
        }

        /// <summary>プレイヤーの周囲にはしごブロックがあるか確認する</summary>
        /// <remarks>
        /// 梯子はブロックの面に設置されるため水平方向にずれる場合がある。
        /// また一つ飛ばし設置に対応するため垂直方向を広めにチェックする。
        /// </remarks>
        private bool IsNearLadder(World world, EntityPlayer player)
        {
            Vector3 pos = player.GetPosition();
            int px = Mathf.FloorToInt(pos.x);
            int py = Mathf.FloorToInt(pos.y);
            int pz = Mathf.FloorToInt(pos.z);

            // 水平方向：プレイヤー足元ブロック＋東西南北1ブロック隣
            var horizontalOffsets = new (int dx, int dz)[]
            {
                (0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)
            };

            // 垂直方向：足元-1 〜 頭上+2（2ブロック飛ばし設置を考慮して広めに）
            for (int dy = -1; dy <= 3; dy++)
            {
                foreach (var (dx, dz) in horizontalOffsets)
                {
                    BlockValue bv = world.GetBlock(new Vector3i(px + dx, py + dy, pz + dz));
                    if (IsLadderBlock(bv))
                        return true;
                }
            }
            return false;
        }

        /// <summary>ブロックがはしご系かどうか判定する</summary>
        private static bool IsLadderBlock(BlockValue bv)
        {
            if (bv.isair || bv.Block == null) return false;
            string name = bv.Block.GetBlockName().ToLower();
            return name.Contains("ladder") || name.Contains("railing");
        }

        /// <summary>ゴッドモード検出</summary>
        private void CheckGodMode(ClientInfo clientInfo, EntityPlayer player)
        {
            // EntityPlayerのIsFlyMode（デバッグモードの飛行/無敵）をチェック
            // 注: この値はサーバーサイドで取得可能 
            try
            {
                // プレイヤーのバフにgodmodeがあるかチェック
                if (player.Buffs != null && player.Buffs.HasBuff("god"))
                {
                    _violationHandler.HandleViolation(clientInfo, ViolationType.GodMode,
                        "ゴッドモードバフ検出");
                }
            }
            catch
            {
                // バフチェックでエラーが出ても無視
            }
        }

        /// <summary>プレイヤーの追跡データ</summary>
        private class PlayerTrackingData
        {
            public Vector3 PreviousPosition { get; set; }
            public bool HasPreviousPosition { get; set; } = false;
            public float LastCheckTime { get; set; } = 0f;

            // テレポート検出用
            public int TeleportCount { get; set; } = 0;

            // スピードハック検出用
            public int SpeedViolationCount { get; set; } = 0;

            // 飛行検出用
            public float AirborneStartTime { get; set; } = 0f;
            public int FlyViolationCount { get; set; } = 0;
        }
    }
}
