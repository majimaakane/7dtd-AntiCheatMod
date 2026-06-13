# AntiCheatMod - 7DTD チーター防止Mod

マルチサーバー向けのサーバーサイドチーター防止Modです。  
非管理者によるチートコマンド実行・飛行・テレポート・スピードハック・ゴッドモードを検出し、警告・キック・BANを自動で行います。

## 機能

- **コンソールコマンドブロック**: `cm`, `dm`, `give`, `tp` などの不正コマンドを非管理者から遮断
- **チャットコマンドブロック**: チャットから `/give`, `/fly`, `/god` 等を送信しても無効化
- **飛行検出**: 一定秒数以上の空中滞在を検出（はしご・水中・乗り物は除外）
- **テレポート検出**: 異常な瞬間移動を検出（ベッドロール等の正常なテレポートは許容）
- **スピードハック検出**: 通常の最大速度を大幅に超える移動速度を検出
- **ゴッドモード検出**: `god` バフの付与を検出
- **管理者免除**: パーミッションレベルが設定値以下の管理者は全検出をバイパス
- **段階的ペナルティ**: 違反ごとに警告 → キック → BAN（各閾値は設定可能）
- **管理者通知**: 検出時にオンライン管理者全員にチャットで通知
- **ファイルログ**: チート検出の詳細をログファイルに記録

## 前提条件

- .NET SDK（.NET Framework 4.8 対応）
- 7 Days to Die Dedicated Server
- **EAC（Easy Anti-Cheat）を無効にする必要があります**

## ビルド方法

### 1. サーバーパスの設定

`Source\AntiCheatMod.csproj` 内の `<HintPath>` を、お使いの7DTDサーバーのパスに変更してください：

```xml
<HintPath>C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server\...</HintPath>
```

### 2. ビルド

`build.bat` を実行するか、以下のコマンドを実行してください：

```
dotnet build Source\AntiCheatMod.csproj -c Release
```

### 3. インストール

`AntiCheatMod` フォルダ（`ModInfo.xml`・`AntiCheatMod.dll`・`Config\AntiCheatConfig.xml` 含む）を  
サーバーの `Mods` フォルダにコピーしてください。

```
7 Days to Die Dedicated Server/
  └── Mods/
      └── AntiCheatMod/
          ├── ModInfo.xml
          ├── AntiCheatMod.dll
          └── Config/
              └── AntiCheatConfig.xml
```

## 設定

`AntiCheatMod\Config\AntiCheatConfig.xml` で動作を調整できます：

```xml
<AntiCheatConfig>
    <!-- 管理者として扱うパーミッションレベル上限（この値以下は管理者として免除） -->
    <!-- 0=最高権限, 1=管理者, 1000=一般 -->
    <AdminPermissionLevel>1</AdminPermissionLevel>

    <Detection>
        <BlockConsoleCommands>true</BlockConsoleCommands>
        <DetectFlying>true</DetectFlying>
        <DetectTeleport>true</DetectTeleport>
        <DetectSpeedHack>true</DetectSpeedHack>
        <DetectGodMode>true</DetectGodMode>
    </Detection>

    <Thresholds>
        <TeleportDistance>80</TeleportDistance>      <!-- テレポート検出距離(m) -->
        <SpeedMultiplier>2.5</SpeedMultiplier>        <!-- 通常最大速度の何倍を超えたら検出 -->
        <FlyingDuration>5.0</FlyingDuration>          <!-- 空中滞在検出時間(秒) -->
        <MonitorInterval>2.0</MonitorInterval>        <!-- 監視間隔(秒) -->
    </Thresholds>

    <Violation>
        <WarnPlayer>true</WarnPlayer>                  <!-- プレイヤーに警告メッセージ -->
        <NotifyAdmins>true</NotifyAdmins>              <!-- 管理者に通知 -->
        <MaxWarnings>3</MaxWarnings>                   <!-- キックまでの違反回数 -->
        <KicksBeforeBan>0</KicksBeforeBan>             <!-- BANまでのキック回数 (0=BANしない) -->
        <ViolationResetMinutes>30</ViolationResetMinutes> <!-- 違反カウントリセット時間(分) -->
        <ResetOnKick>true</ResetOnKick>                <!-- キック後に違反カウントをリセット -->
    </Violation>

    <Logging>
        <EnableFileLog>true</EnableFileLog>
        <LogFileName>AntiCheat.log</LogFileName>
    </Logging>
</AntiCheatConfig>
```

### 主な設定項目

| 項目 | デフォルト | 説明 |
|------|-----------|------|
| `AdminPermissionLevel` | `1` | この値以下のパーミッションレベルを管理者として免除 |
| `TeleportDistance` | `80` | 1チェック間隔でこの距離(m)以上移動したらテレポート疑惑 |
| `SpeedMultiplier` | `2.5` | 通常最大速度の何倍を超えたらスピードハック疑惑 |
| `FlyingDuration` | `5.0` | 地面から離れたまま何秒経過したら飛行疑惑 |
| `MaxWarnings` | `3` | 何回違反でキック |
| `KicksBeforeBan` | `0` | 何回キックでBAN（0=BANしない、有効にする場合はResetOnKick=falseも推奨） |

## ログ確認

サーバーコンソールおよび `Mods/AntiCheatMod/AntiCheat.log` にログが出力されます：

```
[2026-06-13 12:00:00] [INFO] [AntiCheatMod] Mod初期化開始...
[2026-06-13 12:00:00] [INFO] [AntiCheatMod] プレイヤー参加: PlayerName (ID: ..., 権限: 1000, 管理者: False)
[2026-06-13 12:05:30] [CHEAT] プレイヤー: PlayerName (ID: ...) | 種類: Flying | 詳細: 空中滞在時間: 6.2秒
[2026-06-13 12:05:30] [INFO] [ViolationHandler] PlayerName に警告送信: [AntiCheat] 警告: 飛行チートが検出されました。 (残り2回でキックされます)
```
