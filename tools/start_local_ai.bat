@echo off
chcp 65001 >nul
echo ============================================
echo   W7-AI 本地 AI 伺服器
echo ============================================
echo.

set LLAMA_DIR=%~dp0..\llama
set MODEL=%LLAMA_DIR%\qwen2.5-1.5b-instruct-q4_k_m.gguf

if not exist "%LLAMA_DIR%\llama-server.exe" (
    echo [錯誤] 找不到 llama-server.exe
    echo 請從 https://github.com/ggml-org/llama.cpp/releases 下載
    echo 解壓到 llama\ 資料夾
    pause
    exit /b 1
)

if not exist "%MODEL%" (
    echo [錯誤] 找不到模型檔
    echo 請下載: https://huggingface.co/Qwen/Qwen2.5-1.5B-Instruct-GGUF
    echo 放到: %LLAMA_DIR%\
    pause
    exit /b 1
)

echo 啟動中... (CPU 模式, 約需 30 秒)
echo 模型: Qwen2.5-1.5B
echo 網址: http://localhost:8080
echo.
echo 啟動完成後可開啟 W7-AI 使用
echo 按 Ctrl+C 停止
echo.

"%LLAMA_DIR%\llama-server.exe" -m "%MODEL%" --port 8080 --host 127.0.0.1 -c 2048 -t 4

pause
