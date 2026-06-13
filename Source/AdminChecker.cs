namespace AntiCheatMod
{
    /// <summary>
    /// プレイヤーの管理者権限をチェックするクラス。
    /// パーミッションレベルが設定値以下の場合、管理者として扱いチェックを免除する。
    /// </summary>
    public static class AdminChecker
    {
        /// <summary>
        /// 指定プレイヤーが管理者かどうかを判定する。
        /// パーミッションレベル0=最高権限、1=管理者。
        /// 設定のAdminPermissionLevel以下であれば管理者として扱う。
        /// </summary>
        public static bool IsAdmin(ClientInfo clientInfo, AntiCheatConfig config)
        {
            if (clientInfo == null) return false;

            try
            {
                // AdminToolsからプレイヤーのパーミッションレベルを取得
                AdminTools adminTools = GameManager.Instance.adminTools;
                if (adminTools == null) return false;

                // プレイヤーのパーミッションレベルを取得
                // GetUserPermissionLevel はプレイヤーが登録されていない場合1000を返す
                int permissionLevel = adminTools.Users.GetUserPermissionLevel(clientInfo.PlatformId);

                // CrossplatformIdでも確認（EOS対応）
                if (permissionLevel > config.AdminPermissionLevel && clientInfo.CrossplatformId != null)
                {
                    int crossPermLevel = adminTools.Users.GetUserPermissionLevel(clientInfo.CrossplatformId);
                    if (crossPermLevel < permissionLevel)
                        permissionLevel = crossPermLevel;
                }

                return permissionLevel <= config.AdminPermissionLevel;
            }
            catch (System.Exception ex)
            {
                Logger.LogError($"[AdminChecker] 管理者チェックでエラー: {ex.Message}");
                // エラーの場合は安全側（管理者として扱う）
                return true;
            }
        }

        /// <summary>
        /// プレイヤーのパーミッションレベルを取得する。
        /// </summary>
        public static int GetPermissionLevel(ClientInfo clientInfo)
        {
            if (clientInfo == null) return 1000;

            try
            {
                AdminTools adminTools = GameManager.Instance.adminTools;
                if (adminTools == null) return 1000;

                int permissionLevel = adminTools.Users.GetUserPermissionLevel(clientInfo.PlatformId);

                if (clientInfo.CrossplatformId != null)
                {
                    int crossPermLevel = adminTools.Users.GetUserPermissionLevel(clientInfo.CrossplatformId);
                    if (crossPermLevel < permissionLevel)
                        permissionLevel = crossPermLevel;
                }

                return permissionLevel;
            }
            catch
            {
                return 1000;
            }
        }
    }
}
