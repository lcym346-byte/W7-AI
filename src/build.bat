@echo off
cd /d "%~dp0"
echo 開始編譯 AI 智慧代理工具...
echo.

C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe ^
  /target:winexe ^
  /out:AIAgentTool.exe ^
  /reference:System.dll ^
  /reference:System.Core.dll ^
  /reference:System.Data.dll ^
  /reference:System.Drawing.dll ^
  /reference:System.Management.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Xml.dll ^
  /reference:System.Runtime.Serialization.dll ^
  /reference:Microsoft.CSharp.dll ^
  Program.cs ^
  Models\SearchResult.cs ^
  Models\AgentTask.cs ^
  Models\KnowledgeItem.cs ^
  Models\AppSettings.cs ^
  Utils\HtmlHelper.cs ^
  Utils\ThreadSafeUI.cs ^
  Services\AI\GeminiApiService.cs ^
  Services\AI\DuckDuckGoAiService.cs ^
  Services\AI\AiRouter.cs ^
  Services\Search\WebSearchService.cs ^
  Services\Search\WikipediaService.cs ^
  Services\Search\WebScraperService.cs ^
  Services\System\ProcessManagerService.cs ^
  Services\System\SystemAutomationService.cs ^
  Services\System\FileManagerService.cs ^
  Services\CodeGen\CodeGeneratorService.cs ^
  Services\CodeGen\CodeCompilerService.cs ^
  Services\CodeGen\CodeTemplateLibrary.cs ^
  Services\Core\AIReasoningEngine.cs ^
  Services\Core\TaskAutomationService.cs ^
  Services\Core\BackgroundTaskRunner.cs ^
  UI\MainForm.cs ^
  UI\MainForm.Designer.cs ^
  UI\SettingsForm.cs ^
  UI\SettingsForm.Designer.cs

echo.
if %ERRORLEVEL%==0 (
    echo 編譯成功！輸出：%~dp0AIAgentTool.exe
) else (
    echo 編譯失敗，請檢查上方錯誤訊息
)
echo.
pause
