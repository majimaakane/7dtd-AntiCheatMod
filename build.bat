@echo off
echo ==========================================
echo  AntiCheatMod ビルドスクリプト
echo ==========================================
echo.

REM --- バージョンインクリメント ---
echo バージョンを更新中...
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$xml = [xml](Get-Content 'AntiCheatMod\ModInfo.xml' -Encoding UTF8);" ^
  "$ver = $xml.xml.Version.value;" ^
  "$parts = $ver -split '\.';" ^
  "$parts[2] = [int]$parts[2] + 1;" ^
  "$newVer = $parts -join '.';" ^
  "$xml.xml.Version.SetAttribute('value', $newVer);" ^
  "$xml.Save((Resolve-Path 'AntiCheatMod\ModInfo.xml'));" ^
  "Write-Host ('  ' + $ver + ' -> ' + $newVer)"

if %ERRORLEVEL% NEQ 0 (
    echo ★ バージョン更新に失敗しました。
    pause
    exit /b 1
)

echo.

REM --- ビルド実行 ---
echo ビルド中...
dotnet build Source\AntiCheatMod.csproj -c Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ★ ビルドに失敗しました。
    echo   Source\AntiCheatMod.csproj のHintPathが正しいか確認してください。
    pause
    exit /b 1
)

echo.
echo ==========================================
echo  ビルド完了！
echo  AntiCheatMod フォルダを
echo  サーバーの Mods フォルダにコピーしてください。
echo.
echo  設定変更: AntiCheatMod\Config\AntiCheatConfig.xml
echo ==========================================
pause
