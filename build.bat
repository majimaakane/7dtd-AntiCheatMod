@echo off
echo ==========================================
echo  AntiCheatMod ビルドスクリプト
echo ==========================================
echo.

REM ビルド実行（csprojのHintPathに合わせてサーバーパスを設定してください）
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
