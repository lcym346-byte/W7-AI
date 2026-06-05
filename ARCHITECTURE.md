## 2. `ARCHITECTURE.md`（架構設計文件）

```markdown
# 🏗️ 架構設計文件

## 目錄
- [設計原則](#設計原則)
- [技術選型](#技術選型)
- [目錄結構](#目錄結構)
- [分層架構](#分層架構)
- [呼叫關係圖](#呼叫關係圖)
- [各檔案職責](#各檔案職責)
- [AI 三層 Fallback 架構](#ai-三層-fallback-架構)
- [程式碼生成流程](#程式碼生成流程)
- [背景任務機制](#背景任務機制)
- [安全機制](#安全機制)
- [維護指南](#維護指南)

---

## 設計原則

1. **單一職責**：每個 .cs 檔案只做一件事，不超過 300 行
2. **零外部依賴**：不用 NuGet 套件，所有功能用 .NET 4.0 內建 API 實現
3. **三層容錯**：AI 功能有 Gemini → DuckDuckGo → 本地模板 三層 fallback
4. **向下相容**：目標 .NET 4.0，確保 Windows 7 無 SP1 也能執行
5. **安全優先**：CMD 命令白名單/黑名單，生成程式碼執行前必須確認

---

## 技術選型

| 項目 | 選擇 | 原因 |
|------|------|------|
| 目標框架 | .NET Framework 4.0 | Win7 無 SP1 最高支援版本 |
| UI 框架 | WinForms | .NET 4.0 原生，不需額外安裝 |
| 非同步模型 | BackgroundWorker + Thread + 回呼 | .NET 4.0 不支援 async/await |
| HTTP 請求 | HttpWebRequest | .NET 4.0 不支援 HttpClient |
| JSON 解析 | 手寫正則表達式工具類 | 避免 NuGet 依賴 |
| 動態編譯 | CSharpCodeProvider | .NET 4.0 內建 |
| AI 主力 | Google Gemini API (Free Tier) | 免費 5000 次/月，REST API |
| AI 備用 | DuckDuckGo AI Chat | 完全免費，不需 API Key |
| AI 離線 | 本地程式碼模板庫 | 斷網時 fallback |
| 網路搜尋 | DuckDuckGo + Wikipedia | 免費，不需 API Key |

---

## 目錄結構

AIAgentTool/ │ ├── AIAgentTool.sln 方案檔 ├── README.md 專案說明 ├──
ARCHITECTURE.md 本文件 ├── CONTRIBUTING.md 貢獻指南 ├── LICENSE MIT 授權
├── CHANGELOG.md 版本紀錄 │ └── src/ 原始碼根目錄 ├── AIAgentTool.csproj 專案檔
 ├── app.config 應用程式設定 │ ├── Program.cs 程式進入點 ├── MainForm.cs 主視
窗邏輯 ├── MainForm.Designer.cs 主視窗 UI 佈局 ├── SettingsForm.cs 設定視窗 ├──
 SettingsForm.Designer.cs 設定視窗 UI 佈局 │ ├── Models/ 資料模型 │
├── SearchResult.cs 搜尋結果模型 │ ├── AgentTask.cs 任務模型 + 狀態列舉 + 類型列舉 │
 ├── KnowledgeItem.cs 知識快取項目模型 │ └── AppSettings.cs 應用程式設定模型 + 讀寫 │
 ├── Services/ 服務層 │ ├── AI/ AI 服務 │ │
├── GeminiApiService.cs Google Gemini API 呼叫 │ │
├── DuckDuckGoAiService.cs DuckDuckGo AI Chat 呼叫 │
│ └── AiRouter.cs AI 路由器 + Fallback 邏輯 │ │ │
 ├── Search/ 搜尋服務 │ │ ├── WebSearchService.cs DuckDuckGo 搜尋 (Instant + HTML) │
 │ ├── WikipediaService.cs Wikipedia 中英文查詢 │ │ └── WebScraperService.cs 網頁內容擷取 │
 │ │ ├── System/ 系統操作服務 │ │
├── ProcessManagerService.cs 程序啟動/關閉/列舉 + 視窗管理 │ │
├── SystemAutomationService.cs CMD 命令 + 截圖 + 剪貼簿 │
│ └── FileManagerService.cs 目錄瀏覽 + 檔案搜尋 │ │ │
├── CodeGen/ 程式碼生成服務 │ │ ├── CodeGeneratorService.cs AI 程式碼生成 + 意圖解析 │
 │ ├── CodeCompilerService.cs CSharpCodeProvider 動態編譯 │
 │ └── CodeTemplateLibrary.cs 程式碼模板庫 │ │ │ └── Core/ 核心引擎 │
 ├── AIReasoningEngine.cs 本地推理/摘要/關鍵詞/計算 │
├── TaskAutomationService.cs 總調度器 (意圖分析 → 分派任務)
│ └── BackgroundTaskRunner.cs 背景執行緒 + 任務佇列
│ └── Utils/ 工具類 ├── HtmlHelper.cs HTML 清理 + JSON 解析 └── ThreadSafeUI.cs 跨執行緒安全 UI 更新

## 分層架構

┌─────────────────────────────────────────────────────────┐ │ UI 層 (Presentation) │ │ MainForm.cs MainForm.Designer.cs SettingsForm.cs │ └────────────────────────┬────────────────────────────────┘ │ 透過 BackgroundTaskRunner 呼叫 │ (跨執行緒，不阻塞 UI) ┌────────────────────────┴────────────────────────────────┐ │ 核心引擎層 (Core) │ │ TaskAutomationService.cs (總調度 — 意圖分析→分派) │ │ BackgroundTaskRunner.cs (背景任務佇列) │ │ AIReasoningEngine.cs (本地推理/摘要/計算) │ └──────┬────────┬─────────┬──────────┬────────────────────┘ │ │ │ │ ┌──────┴──┐ ┌───┴───┐ ┌──┴────┐ ┌───┴──────┐ │ AI 層 │ │搜尋層 │ │系統層 │ │程式碼生成│ │ │ │ │ │ │ │ │ │AiRouter │ │WebSrch│ │Process│ │CodeGen │ │ ├Gemini │ │WikiSvc│ │SysAuto│ │Compiler │ │ ├DDG AI │ │Scraper│ │FileMgr│ │Templates │ │ └Local │ │ │ │ │ │ │ └─────────┘ └───────┘ └───────┘ └──────────┘ │ │ │ │ ┌──────┴────────┴─────────┴──────────┴────────────────────┐ │ 工具層 (Utils) │ │ HtmlHelper.cs ThreadSafeUI.cs │ └──────────────────────────────────────────────────────────┘ │ │ ┌──────┴────────┴─────────────────────────────────────────┐ │ 資料模型層 (Models) │ │ SearchResult AgentTask KnowledgeItem AppSettings │ └──────────────────────────────────────────────────────────┘


---

## 呼叫關係圖

MainForm │ ├─→ BackgroundTaskRunner ※ 所有任務都透過這裡在背景執行 │ │ │ └─→ TaskAutomationService ※ 總調度器，分析意圖後分派 │ │ │ ├─→ AIReasoningEngine 本地推理/摘要/關鍵詞/計算 │ │ │ ├─→ AiRouter AI 呼叫路由 │ │ ├─→ GeminiApiService Google Gemini REST API │ │ ├─→ DuckDuckGoAiService DuckDuckGo AI Chat │ │ └─→ (離線 fallback) 回傳提示改用本地模板 │ │ │ ├─→ WebSearchService DuckDuckGo 搜尋 │ ├─→ WikipediaService Wikipedia API │ ├─→ WebScraperService 網頁內容擷取 │ │ │ ├─→ ProcessManagerService 程序啟動/關閉/視窗管理 │ ├─→ SystemAutomationService CMD/截圖/剪貼簿 │ ├─→ FileManagerService 檔案瀏覽/搜尋 │ │ │ └─→ CodeGeneratorService 程式碼生成 │ ├─→ AiRouter 呼叫 AI 寫程式碼 │ ├─→ CodeTemplateLibrary 本地模板庫 │ └─→ CodeCompilerService 編譯 .cs → .exe │ ├─→ SettingsForm 設定畫面 │ └─→ AppSettings 讀寫設定檔 (JSON) │ └─→ ThreadSafeUI 跨執行緒安全更新 UI 控件


---

## 各檔案職責

### 程式入口

| 檔案 | 行數 | 職責 |
|------|------|------|
| `Program.cs` | ~30 | 應用程式進入點，設定 TLS，啟動 MainForm |

### UI 層

| 檔案 | 行數 | 職責 |
|------|------|------|
| `MainForm.Designer.cs` | ~350 | 純 UI 佈局定義，所有控件的位置/大小/顏色/字型 |
| `MainForm.cs` | ~300 | UI 事件處理：按鈕點擊、Enter 鍵、歷史回顧、結果顯示、托盤管理 |
| `SettingsForm.Designer.cs` | ~150 | 設定視窗 UI 佈局 |
| `SettingsForm.cs` | ~200 | API Key 輸入、AI 來源選擇、安全等級調整、儲存設定 |

### 資料模型

| 檔案 | 行數 | 職責 |
|------|------|------|
| `SearchResult.cs` | ~30 | 搜尋結果：Title, Url, Snippet, Source, RelevanceScore |
| `AgentTask.cs` | ~60 | 任務：Id, Query, Type(列舉), Status(列舉), Result, Steps, Progress |
| `KnowledgeItem.cs` | ~20 | 知識快取：Topic, Content, Source, CachedAt, UseCount |
| `AppSettings.cs` | ~40 | 設定：GeminiApiKey, AiSource, SafetyLevel, SavePath + JSON 讀寫 |

### AI 服務

| 檔案 | 行數 | 職責 |
|------|------|------|
| `GeminiApiService.cs` | ~200 | 呼叫 Gemini REST API (POST JSON)，處理回應，錯誤處理 |
| `DuckDuckGoAiService.cs` | ~200 | 模擬 DuckDuckGo AI Chat HTTP 請求，取得 AI 回答 |
| `AiRouter.cs` | ~150 | 根據設定和連線狀態選擇 AI 來源：Gemini → DDG → 離線提示 |

### 搜尋服務

| 檔案 | 行數 | 職責 |
|------|------|------|
| `WebSearchService.cs` | ~250 | DuckDuckGo Instant API + HTML 搜尋，並行多源 |
| `WikipediaService.cs` | ~150 | Wikipedia 中英文 API，取得條目摘要/全文 |
| `WebScraperService.cs` | ~150 | 擷取任意網頁主要內容，HTML→純文字 |

### 系統操作服務

| 檔案 | 行數 | 職責 |
|------|------|------|
| `ProcessManagerService.cs` | ~300 | 啟動程式(中文別名)、關閉程式(正常→強制)、列舉程序、視窗管理(P/Invoke) |
| `SystemAutomationService.cs` | ~300 | CMD 安全執行(白名單/黑名單)、快速命令別名、螢幕截圖、剪貼簿讀寫 |
| `FileManagerService.cs` | ~200 | 瀏覽目錄(中文別名)、搜尋檔案(遞迴)、開啟資料夾 |

### 程式碼生成服務

| 檔案 | 行數 | 職責 |
|------|------|------|
| `CodeGeneratorService.cs` | ~250 | 接收自然語言需求 → 判斷用 AI 或模板 → 取得程式碼 → 回傳 |
| `CodeCompilerService.cs` | ~150 | 用 CSharpCodeProvider 編譯 .cs 原始碼為 .exe，回傳編譯結果/錯誤 |
| `CodeTemplateLibrary.cs` | ~300 | 所有內建模板：Console/WinForms/檔案處理/網路工具/系統工具/文字處理 |

### 核心引擎

| 檔案 | 行數 | 職責 |
|------|------|------|
| `AIReasoningEngine.cs` | ~250 | 本地推理：意圖分析、關鍵詞提取、文字摘要、綜合分析、數學計算、系統資訊 |
| `TaskAutomationService.cs` | ~300 | 總調度器：分析意圖(擴充版) → 分派到對應服務 → 回傳結果 |
| `BackgroundTaskRunner.cs` | ~150 | 背景任務：任務佇列、BackgroundWorker 執行、進度/完成事件 |

### 工具類

| 檔案 | 行數 | 職責 |
|------|------|------|
| `HtmlHelper.cs` | ~200 | HTML 標籤清除、JSON 值擷取、JSON 陣列解析、文字截斷、連結擷取 |
| `ThreadSafeUI.cs` | ~80 | 跨執行緒安全地更新 WinForms 控件 (InvokeRequired 封裝) |

---

## AI 三層 Fallback 架構

使用者提問 │ ▼ AiRouter.SendAsync(prompt) │ ├─ 1. 檢查 Gemini API Key 是否存在且有效 │ ├─ 有 → 呼叫 GeminiApiService │ │ ├─ 成功 → 回傳結果 ✓ │ │ └─ 失敗 (網路錯誤/額度用盡/逾時) │ │ │ │ │ ▼ │ └─ 無 Key ──────┤ │ │ ├─ 2. 嘗試 DuckDuckGo AI Chat │ ├─ 成功 → 回傳結果 ✓ │ └─ 失敗 (網路斷線/被封鎖) │ │ │ ▼ └─ 3. 離線模式 ├─ 程式碼生成 → 使用本地 CodeTemplateLibrary ├─ 搜尋/問答 → 回傳「目前離線，建議檢查網路連線」 └─ 計算/系統資訊/檔案操作 → 正常執行（不需網路）


### 各 AI 來源比較

| | Gemini API | DuckDuckGo AI | 本地模板 |
|---|---|---|---|
| 需要 API Key | ✅ 需要（免費申請） | ❌ 不需要 | ❌ 不需要 |
| 需要網路 | ✅ | ✅ | ❌ |
| 免費額度 | 5000 次/月 | 無上限（有速率限制） | 無限 |
| 程式碼品質 | ⭐⭐⭐⭐⭐ 最佳 | ⭐⭐⭐⭐ 良好 | ⭐⭐⭐ 固定模板 |
| 回應速度 | 1-3 秒 | 2-5 秒 | 即時 |
| 穩定性 | ⭐⭐⭐⭐ 高 | ⭐⭐⭐ 中（非官方） | ⭐⭐⭐⭐⭐ 最穩 |

---

## 程式碼生成流程

使用者: 「寫程式 批次把jpg檔名加上日期前綴」 │ ▼ TaskAutomationService.AnalyzeIntent() → 識別為 TaskType.CodeGeneration │ ▼ CodeGeneratorService.GenerateAsync(description) │ ├─ 1. 建構 Prompt │ 「請用 C# 寫一個 .NET 4.0 Console 程式，功能是： │ 批次把jpg檔名加上日期前綴。 │ 只回傳完整的 .cs 原始碼，不要解釋。」 │ ├─ 2. 呼叫 AiRouter.SendAsync(prompt) │ Gemini → DDG → 離線 │ ├─ 3. 如果 AI 回傳成功 │ → 擷取 csharp ... 區塊 │ → 清理多餘文字 │ ├─ 4. 如果 AI 全部失敗 │ → CodeTemplateLibrary.FindBestTemplate(description) │ → 回傳最接近的本地模板 │ └─ 5. 回傳原始碼 │ ▼ MainForm 顯示在「程式碼」分頁 使用者可以： │ ├─ [編輯] 手動修改原始碼 ├─ [編譯] → CodeCompilerService.Compile(source) │ ├─ 成功 → 產生 .exe │ └─ 失敗 → 顯示錯誤 + AI 自動修正建議 ├─ [執行] → 彈出確認對話框 → Process.Start(.exe) ├─ [存 .cs] → SaveFileDialog └─ [存 .exe] → SaveFileDialog


---

## 背景任務機制

MainForm (UI Thread) │ │ 使用者點 [執行] 或按 Enter │ ▼ BackgroundTaskRunner.EnqueueTask(query) │ │ 任務加入佇列 Queue │ │ BackgroundWorker (背景執行緒) │ ┌─────────────────────────────┐ │ │ while (queue.Count > 0) │ │ │ { │ │ │ var task = queue.Dequeue();│ │ │ TaskAutomation.Execute(); │ ← 在背景執行 │ │ ReportProgress(); │ ← 觸發 UI 更新 │ │ } │ │ └─────────────────────────────┘ │ │ ProgressChanged 事件 (自動回到 UI Thread) ▼ ThreadSafeUI.UpdateControl() │ ├─ 更新進度條 ├─ 更新狀態列 ├─ 更新步驟日誌 └─ 任務完成 → 如果最小化中 → 托盤氣泡通知


### 托盤行為

| 操作 | 行為 |
|------|------|
| 點 X 關閉按鈕 | 最小化到托盤（不是真的關閉） |
| 托盤圖示雙擊 | 還原視窗 |
| 托盤右鍵 → 顯示視窗 | 還原視窗 |
| 托盤右鍵 → 退出程式 | 真正關閉 |
| 任務完成時 | 托盤氣泡通知「任務完成：XXX」 |
| 托盤 Tooltip | 「AI Agent - 執行中: 2 個任務」 |

---

## 安全機制

### CMD 命令控制

允許 (白名單)： 禁止 (黑名單)： ipconfig, ping, tracert, del, erase, rmdir, format, netstat, nslookup, tasklist, shutdown, restart, reg, systeminfo, hostname, whoami, net, netsh, sc, bcdedit, dir, tree, type, find, ver, diskpart, cipher, takeown, driverquery, getmac, echo, powershell, wscript, cscript wmic (查詢類)


### 管道/重導向檢查
- 禁止 `| del`、`> format` 等危險組合
- 允許安全的管道如 `| find`

### 關閉程式安全策略
CloseMainWindow() → 等待 3 秒 → 如果還在 → Kill()


### 程式碼執行確認
編譯的 .exe 執行前一律彈出確認對話框： 「即將執行 AI 生成的程式 XXX.exe，是否確認？」 [確認執行] [取消]


### 安全等級設定
| 等級 | CMD | 程式碼執行 | 關閉程式 |
|------|-----|----------|---------|
| 嚴格 | 僅白名單 | 每次確認 | 每次確認 |
| 中等 | 白名單 + 安全管道 | 每次確認 | 不確認 |
| 寬鬆 | 除黑名單外都允許 | 首次確認 | 不確認 |

---

## 維護指南

### 我想改 UI 長相
→ 只改 `MainForm.Designer.cs`

### 我想改按鈕行為
→ 只改 `MainForm.cs`

### Gemini API 改版了
→ 只改 `GeminiApiService.cs`

### DuckDuckGo AI Chat 壞了
→ 只改 `DuckDuckGoAiService.cs`

### 想加新的 AI 來源
→ 新增一個 `xxxAiService.cs` + 改 `AiRouter.cs` 加入 fallback

### 搜尋結果解析有問題
→ 只改 `WebSearchService.cs` 或 `WikipediaService.cs`

### 想加新的程式碼模板
→ 只改 `CodeTemplateLibrary.cs`

### 想加新的中文程式別名
→ 只改 `ProcessManagerService.cs` 的 `AppAliases` 字典

### 想加新的 CMD 快速命令
→ 只改 `SystemAutomationService.cs` 的 `QuickCommand` 字典

### 想加新的任務類型
→ 1. `AgentTask.cs` 加 TaskType 列舉值
→ 2. `TaskAutomationService.cs` 加意圖識別關鍵字 + 分派方法

### 編譯出錯要除錯
→ 只看 `CodeCompilerService.cs`

### 背景執行有 bug
→ 只看 `BackgroundTaskRunner.cs`
3. CONTRIBUTING.md（貢獻指南）
# 🤝 貢獻指南

感謝你有興趣貢獻！以下是參與本專案的規範。

---

## 開發環境

- **IDE**：Visual Studio 2010 以上 或 任何文字編輯器 + 命令列編譯
- **框架**：.NET Framework 4.0（不可使用更高版本的 API）
- **語言**：C# 5.0 以下語法（不可使用 async/await、string interpolation $""）
- **套件**：不使用任何 NuGet 套件

## ⚠️ .NET 4.0 語法限制

因為目標是 Windows 7 無 SP1，以下語法**不能用**：

```csharp
// ❌ 不能用 async/await (.NET 4.5+)
async Task DoSomething() { await Task.Delay(100); }

// ❌ 不能用 string interpolation (C# 6.0+)
string s = $"Hello {name}";

// ❌ 不能用 null conditional (C# 6.0+)
string s = obj?.ToString();

// ❌ 不能用 HttpClient (.NET 4.5+)
var client = new HttpClient();

// ✅ 正確寫法
string s = string.Format("Hello {0}", name);
string s = obj != null ? obj.ToString() : null;
HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

分支策略
main：穩定版本
dev：開發分支
feature/xxx：新功能分支
fix/xxx：修復分支
提交格式
[類型] 簡短描述

類型：
  feat:     新功能
  fix:      修復 bug
  refactor: 重構（不改功能）
  docs:     文件
  style:    格式（不影響程式碼執行）
  test:     測試

範例：
  feat: 新增 Ollama 本地 AI 支援
  fix: 修復 DuckDuckGo HTML 搜尋結果解析失敗
  docs: 更新 README 安裝步驟
新增功能流程
在 Issues 中提出或認領功能
從 dev 建立 feature/xxx 分支
只修改相關的檔案（遵守單一職責）
確保編譯通過（.NET 4.0）
提交 Pull Request 到 dev
新增 AI 來源步驟
在 src/Services/AI/ 新增 XxxAiService.cs
實作 string SendMessage(string prompt) 方法
在 AiRouter.cs 加入 fallback 邏輯
在 AppSettings.cs 加入設定欄位
在 SettingsForm.cs 加入 UI 設定
新增任務類型步驟
在 AgentTask.cs 的 TaskType 列舉加入新值
在 TaskAutomationService.cs 的 AnalyzeIntentExtended() 加入關鍵字
在 TaskAutomationService.cs 加入 ExecuteXxxTask() 方法
如需新服務，在對應的 Services/ 子目錄新增 .cs 檔
