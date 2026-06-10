@echo off
chcp 65001 >nul
echo ============================================
echo   W7-AI 本地 LLM 啟動器 (KoboldCpp)
echo ============================================
echo.

set LLAMA_DIR=%~dp0..\llama
set MODEL=%LLAMA_DIR%\qwen2.5-1.5b-instruct-q4_k_m.gguf

if not exist "%LLAMA_DIR%\koboldcpp-oldpc.exe" (
    echo [錯誤] 找不到 koboldcpp-oldpc.exe
    echo 請確認檔案在: %LLAMA_DIR%\
    pause
    exit /b 1
)

if not exist "%MODEL%" (
    echo [錯誤] 找不到模型檔案
    echo 請確認: %MODEL%
    pause
    exit /b 1
)

echo 啟動 KoboldCpp...
echo 模型: qwen2.5-1.5b-instruct-q4_k_m.gguf
echo 端口: 5001
echo.
echo 啟動後請勿關閉此視窗！
echo W7-AI 將自動連線到 http://127.0.0.1:5001
echo ============================================
echo.

"%LLAMA_DIR%\koboldcpp-oldpc.exe" --model "%MODEL%" --port 5001 --host 127.0.0.1 --contextsize 512 --threads 2

pause
