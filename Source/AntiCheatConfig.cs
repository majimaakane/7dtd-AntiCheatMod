using System;
using System.IO;
using System.Xml;

namespace AntiCheatMod
{
    /// <summary>
    /// AntiCheatModの設定管理クラス。
    /// Config/AntiCheatConfig.xml から設定を読み込む。
    /// </summary>
    public class AntiCheatConfig
    {
        // --- 管理者設定 ---
        /// <summary>管理者として扱うパーミッションレベル上限（この値以下は管理者）</summary>
        public int AdminPermissionLevel { get; set; } = 1;

        // --- 検出設定 ---
        /// <summary>チャット経由のチートコマンド（/give, /fly 等）をブロック</summary>
        public bool BlockConsoleCommands { get; set; } = true;
        public bool DetectFlying { get; set; } = true;
        public bool DetectTeleport { get; set; } = true;
        public bool DetectSpeedHack { get; set; } = true;
        public bool DetectGodMode { get; set; } = true;

        // --- 閾値設定 ---
        /// <summary>テレポート検出距離(m)</summary>
        public float TeleportDistance { get; set; } = 80f;
        /// <summary>スピードハック判定倍率</summary>
        public float SpeedMultiplier { get; set; } = 2.5f;
        /// <summary>飛行検出連続秒数</summary>
        public float FlyingDuration { get; set; } = 5.0f;
        /// <summary>監視間隔(秒)</summary>
        public float MonitorInterval { get; set; } = 2.0f;

        // --- 違反設定 ---
        public bool WarnPlayer { get; set; } = true;
        public bool NotifyAdmins { get; set; } = true;
        /// <summary>キックまでの警告回数</summary>
        public int MaxWarnings { get; set; } = 3;
        /// <summary>BANまでのキック回数 (0=BANしない)</summary>
        public int KicksBeforeBan { get; set; } = 0;
        /// <summary>違反カウントリセット時間(分) (0=リセットなし)</summary>
        public int ViolationResetMinutes { get; set; } = 30;
        /// <summary>キック後に違反カウントをリセットするか</summary>
        public bool ResetOnKick { get; set; } = true;

        // --- ログ設定 ---
        public bool EnableFileLog { get; set; } = true;
        public string LogFileName { get; set; } = "AntiCheat.log";

        /// <summary>設定ファイルから読み込み。存在しない場合はデフォルト設定ファイルを生成。</summary>
        public static AntiCheatConfig Load(string modPath)
        {
            var config = new AntiCheatConfig();
            string configDir = Path.Combine(modPath, "Config");
            string configPath = Path.Combine(configDir, "AntiCheatConfig.xml");

            if (!File.Exists(configPath))
            {
                Logger.LogWarning("[AntiCheatMod] 設定ファイルが見つかりません。デフォルト設定を使用します。");
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);
                config.Save(configPath);
                return config;
            }

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(configPath);
                XmlElement root = doc.DocumentElement;

                config.AdminPermissionLevel = ReadInt(root, "AdminPermissionLevel", 1);

                XmlNode detection = root.SelectSingleNode("Detection");
                if (detection != null)
                {
                    config.BlockConsoleCommands = ReadBool(detection, "BlockConsoleCommands", true);
                    config.DetectFlying = ReadBool(detection, "DetectFlying", true);
                    config.DetectTeleport = ReadBool(detection, "DetectTeleport", true);
                    config.DetectSpeedHack = ReadBool(detection, "DetectSpeedHack", true);
                    config.DetectGodMode = ReadBool(detection, "DetectGodMode", true);
                }

                XmlNode thresholds = root.SelectSingleNode("Thresholds");
                if (thresholds != null)
                {
                    config.TeleportDistance = ReadFloat(thresholds, "TeleportDistance", 80f);
                    config.SpeedMultiplier = ReadFloat(thresholds, "SpeedMultiplier", 2.5f);
                    config.FlyingDuration = ReadFloat(thresholds, "FlyingDuration", 5.0f);
                    config.MonitorInterval = ReadFloat(thresholds, "MonitorInterval", 2.0f);
                }

                XmlNode violation = root.SelectSingleNode("Violation");
                if (violation != null)
                {
                    config.WarnPlayer = ReadBool(violation, "WarnPlayer", true);
                    config.NotifyAdmins = ReadBool(violation, "NotifyAdmins", true);
                    config.MaxWarnings = ReadInt(violation, "MaxWarnings", 3);
                    config.KicksBeforeBan = ReadInt(violation, "KicksBeforeBan", 0);
                    config.ViolationResetMinutes = ReadInt(violation, "ViolationResetMinutes", 30);
                    config.ResetOnKick = ReadBool(violation, "ResetOnKick", true);
                }

                XmlNode logging = root.SelectSingleNode("Logging");
                if (logging != null)
                {
                    config.EnableFileLog = ReadBool(logging, "EnableFileLog", true);
                    config.LogFileName = ReadString(logging, "LogFileName", "AntiCheat.log");
                }

                Logger.Log("[AntiCheatMod] 設定ファイルを読み込みました。");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AntiCheatMod] 設定ファイルの読み込みに失敗しました: {ex.Message}");
            }

            return config;
        }

        /// <summary>現在の設定をXMLファイルに保存</summary>
        public void Save(string path)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                XmlDeclaration decl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
                doc.AppendChild(decl);

                XmlElement root = doc.CreateElement("AntiCheatConfig");
                doc.AppendChild(root);

                AddElement(doc, root, "AdminPermissionLevel", AdminPermissionLevel.ToString());

                XmlElement detection = doc.CreateElement("Detection");
                root.AppendChild(detection);
                AddElement(doc, detection, "BlockConsoleCommands", BlockConsoleCommands.ToString().ToLower());
                AddElement(doc, detection, "DetectFlying", DetectFlying.ToString().ToLower());
                AddElement(doc, detection, "DetectTeleport", DetectTeleport.ToString().ToLower());
                AddElement(doc, detection, "DetectSpeedHack", DetectSpeedHack.ToString().ToLower());
                AddElement(doc, detection, "DetectGodMode", DetectGodMode.ToString().ToLower());

                XmlElement thresholds = doc.CreateElement("Thresholds");
                root.AppendChild(thresholds);
                AddElement(doc, thresholds, "TeleportDistance", TeleportDistance.ToString());
                AddElement(doc, thresholds, "SpeedMultiplier", SpeedMultiplier.ToString());
                AddElement(doc, thresholds, "FlyingDuration", FlyingDuration.ToString());
                AddElement(doc, thresholds, "MonitorInterval", MonitorInterval.ToString());

                XmlElement violation = doc.CreateElement("Violation");
                root.AppendChild(violation);
                AddElement(doc, violation, "WarnPlayer", WarnPlayer.ToString().ToLower());
                AddElement(doc, violation, "NotifyAdmins", NotifyAdmins.ToString().ToLower());
                AddElement(doc, violation, "MaxWarnings", MaxWarnings.ToString());
                AddElement(doc, violation, "KicksBeforeBan", KicksBeforeBan.ToString());
                AddElement(doc, violation, "ViolationResetMinutes", ViolationResetMinutes.ToString());
                AddElement(doc, violation, "ResetOnKick", ResetOnKick.ToString().ToLower());

                XmlElement logging = doc.CreateElement("Logging");
                root.AppendChild(logging);
                AddElement(doc, logging, "EnableFileLog", EnableFileLog.ToString().ToLower());
                AddElement(doc, logging, "LogFileName", LogFileName);

                doc.Save(path);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[AntiCheatMod] 設定ファイルの保存に失敗しました: {ex.Message}");
            }
        }

        // --- ヘルパーメソッド ---
        private static int ReadInt(XmlNode parent, string name, int defaultValue)
        {
            XmlNode node = parent.SelectSingleNode(name);
            if (node != null && int.TryParse(node.InnerText.Trim(), out int val)) return val;
            return defaultValue;
        }

        private static float ReadFloat(XmlNode parent, string name, float defaultValue)
        {
            XmlNode node = parent.SelectSingleNode(name);
            if (node != null && float.TryParse(node.InnerText.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float val)) return val;
            return defaultValue;
        }

        private static bool ReadBool(XmlNode parent, string name, bool defaultValue)
        {
            XmlNode node = parent.SelectSingleNode(name);
            if (node != null && bool.TryParse(node.InnerText.Trim(), out bool val)) return val;
            return defaultValue;
        }

        private static string ReadString(XmlNode parent, string name, string defaultValue)
        {
            XmlNode node = parent.SelectSingleNode(name);
            if (node != null && !string.IsNullOrEmpty(node.InnerText.Trim())) return node.InnerText.Trim();
            return defaultValue;
        }

        private static void AddElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement elem = doc.CreateElement(name);
            elem.InnerText = value;
            parent.AppendChild(elem);
        }
    }
}
