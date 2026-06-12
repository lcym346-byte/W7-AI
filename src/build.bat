@echo off
cd /d "%~dp0"
echo Building AIAgentTool...
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
  Services\AI\OpenAiCompatibleService.cs ^
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
  Services\Core\SmartTaskPlanner.cs ^
  Services\Core\SmartTaskExecutor.cs ^
  Services\Core\TaskAutomationService.cs ^
  Services\Core\BackgroundTaskRunner.cs ^
  UI\MainForm.cs ^
  UI\MainForm.Designer.cs ^
  UI\SettingsForm.cs ^
  UI\SettingsForm.Designer.cs
src\Services\CodeGen\LessonMemory.cs

copy /Y AIAgentTool.exe.config AIAgentTool.exe.config >nul 2>&1

echo.
if %ERRORLEVEL%==0 (
    echo BUILD OK
) else (
    echo BUILD FAILED
)
echo.
pause
