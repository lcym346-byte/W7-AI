# 🤖 AI 智慧代理工具 (AI Agent Tool)

> Windows 7 相容的本機 AI 代理工具，具備圖形介面，能用自然語言下指令，
> 自動搜尋網路資訊、操作電腦程式、生成並編譯 C# 程式碼。

![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.0-blue)
![Windows](https://img.shields.io/badge/Windows-7%2F8%2F10%2F11-green)
![License](https://img.shields.io/badge/License-MIT-yellow)
![Language](https://img.shields.io/badge/Language-C%23-purple)

---

## ✨ 特色

- **零門檻部署**：.NET Framework 4.0（Windows 7 原版內建），不需安裝額外套件
- **自然語言操控**：用中文或英文口語化指令，AI 自動判斷該做什麼
- **三層 AI 架構**：Google Gemini API → DuckDuckGo AI Chat → 本地模板引擎
- **程式碼生成**：描述需求 → AI 寫 C# 程式碼 → 即時編譯為 .exe → 直接執行
- **電腦操作代理**：啟動/關閉程式、視窗管理、檔案管理、CMD 命令
- **背景執行**：最小化到系統托盤，任務在背景持續執行
- **完全免費**：所有 API 均使用免費額度，不需付費

---

## 📸 介面預覽

---

## 🔧 系統需求

| 項目 | 最低需求 |
|------|---------|
| 作業系統 | Windows 7（不需 SP1）/ 8 / 8.1 / 10 / 11 |
| .NET Framework | 4.0（Windows 7 原版已內建） |
| 記憶體 | 512 MB 以上 |
| 磁碟空間 | 10 MB |
| 網路 | 需要（AI 及搜尋功能）；離線時可用本地模板及電腦操作功能 |

---

## 🚀 安裝與使用

### 方法 A：下載預編譯版本

1. 到 [Releases](../../releases) 頁面下載最新的 `AIAgentTool.zip`
2. 解壓縮到任意資料夾
3. 雙擊 `AIAgentTool.exe` 執行

### 方法 B：自行編譯

```bat
git clone https://github.com/你的帳號/AIAgentTool.git
cd AIAgentTool

:: 使用 .NET Framework 4.0 內建編譯器
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe ^
  /target:winexe ^
  /out:bin\AIAgentTool.exe ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Web.dll ^
  /reference:System.Data.dll ^
  /recurse:src\*.cs
方法 C：用 Visual Studio 開啟
開啟 AIAgentTool.sln
確認目標框架為 .NET Framework 4.0
按 F5 編譯執行

⚙️ 首次設定
啟動程式後，點擊 [設定] 按鈕
Google Gemini API Key（建議設定，免費）：
前往 https://aistudio.google.com/apikey
用 Google 帳號登入
點「Create API Key」取得 Key
貼入設定畫面
不設定 Gemini Key 也能用（自動使用 DuckDuckGo AI Chat 或本地模板）

📖 使用範例
什麼是量子計算
研究 人工智慧的發展歷史
比較 Python vs Java
摘要 https://en.wikipedia.org/wiki/AI

電腦操作
開啟 記事本
關閉 chrome
列出程序
已安裝程式
最大化 notepad
瀏覽 桌面
找檔案 report.docx
截圖
cmd ipconfig

程式碼生成
寫程式 批次把jpg檔名加上日期前綴
寫程式 一個可以倒數計時的小視窗
寫程式 讀取CSV檔案並計算平均值

批次操作
開啟 記事本; 開啟 計算機; 開啟 小畫家
開啟 chrome 然後 截圖

🏗️ 技術架構
詳見 ARCHITECTURE.md

🤝 貢獻指南
詳見 CONTRIBUTING.md

📄 授權
MIT License — 詳見 LICENSE
