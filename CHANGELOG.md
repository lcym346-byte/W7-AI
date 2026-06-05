## 4. `CHANGELOG.md`（版本紀錄）

```markdown
# 📋 版本紀錄

## [2.0.0] - 開發中

### 新增
- 三層 AI 架構：Gemini API + DuckDuckGo AI Chat + 本地模板
- 程式碼生成：自然語言 → C# 原始碼 → 即時編譯 → 執行
- 電腦操作：啟動/關閉程式、視窗管理、檔案管理
- 安全 CMD 執行：白名單/黑名單機制
- 系統托盤：最小化背景執行 + 氣泡通知
- 背景任務佇列：不阻塞 UI
- 設定畫面：API Key、AI 來源、安全等級
- 螢幕截圖、剪貼簿讀寫
- 批次操作（分號/然後）
- 任務歷史回顧
- 結果匯出 .txt

### 技術
- 目標框架 .NET Framework 4.0（Windows 7 無 SP1 相容）
- 零 NuGet 依賴
- 非同步使用 BackgroundWorker + Thread（不用 async/await）
- 28 個 .cs 檔案，單一職責架構
5. LICENSE
CopyMIT License

Copyright (c) 2026

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
6. .gitignore
Copy# Build results
bin/
obj/
*.exe
*.dll
*.pdb

# Visual Studio
.vs/
*.suo
*.user
*.sln.docstates

# User settings
settings.json

# OS files
Thumbs.db
Desktop.ini
.DS_Store

# Generated code output
output/
generated/
7. src/AIAgentTool.csproj（專案檔）
Copy<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="4.0" DefaultTargets="Build"
         xmlns="http://schemas.microsoft.com/developer/msbuild/2003">

  <PropertyGroup>
    <Configuration Condition=" '$(Configuration)' == '' ">Debug</Configuration>
    <Platform Condition=" '$(Platform)' == '' ">AnyCPU</Platform>
    <ProjectGuid>{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}</ProjectGuid>
    <OutputType>WinExe</OutputType>
    <RootNamespace>AIAgentTool</RootNamespace>
    <AssemblyName>AIAgentTool</AssemblyName>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)' == 'Debug' ">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>..\bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
  </PropertyGroup>

  <PropertyGroup Condition=" '$(Configuration)' == 'Release' ">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>..\bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="System" />
    <Reference Include="System.Core" />
    <Reference Include="System.Drawing" />
    <Reference Include="System.Windows.Forms" />
    <Reference Include="System.Web" />
    <Reference Include="System.Data" />
    <Reference Include="System.Xml" />
    <Reference Include="Microsoft.CSharp" />
  </ItemGroup>

  <ItemGroup>
    <!-- 程式入口 -->
    <Compile Include="Program.cs" />

    <!-- UI -->
    <Compile Include="MainForm.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="MainForm.Designer.cs">
      <DependentUpon>MainForm.cs</DependentUpon>
    </Compile>
    <Compile Include="SettingsForm.cs">
      <SubType>Form</SubType>
    </Compile>
    <Compile Include="SettingsForm.Designer.cs">
      <DependentUpon>SettingsForm.cs</DependentUpon>
    </Compile>

    <!-- Models -->
    <Compile Include="Models\SearchResult.cs" />
    <Compile Include="Models\AgentTask.cs" />
    <Compile Include="Models\KnowledgeItem.cs" />
    <Compile Include="Models\AppSettings.cs" />

    <!-- Services/AI -->
    <Compile Include="Services\AI\GeminiApiService.cs" />
    <Compile Include="Services\AI\DuckDuckGoAiService.cs" />
    <Compile Include="Services\AI\AiRouter.cs" />

    <!-- Services/Search -->
    <Compile Include="Services\Search\WebSearchService.cs" />
    <Compile Include="Services\Search\WikipediaService.cs" />
    <Compile Include="Services\Search\WebScraperService.cs" />

    <!-- Services/System -->
    <Compile Include="Services\System\ProcessManagerService.cs" />
    <Compile Include="Services\System\SystemAutomationService.cs" />
    <Compile Include="Services\System\FileManagerService.cs" />

    <!-- Services/CodeGen -->
    <Compile Include="Services\CodeGen\CodeGeneratorService.cs" />
    <Compile Include="Services\CodeGen\CodeCompilerService.cs" />
    <Compile Include="Services\CodeGen\CodeTemplateLibrary.cs" />

    <!-- Services/Core -->
    <Compile Include="Services\Core\AIReasoningEngine.cs" />
    <Compile Include="Services\Core\TaskAutomationService.cs" />
    <Compile Include="Services\Core\BackgroundTaskRunner.cs" />

    <!-- Utils -->
    <Compile Include="Utils\HtmlHelper.cs" />
    <Compile Include="Utils\ThreadSafeUI.cs" />
  </ItemGroup>

  <ItemGroup>
    <None Include="app.config" />
  </ItemGroup>

  <Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />

</Project>
Copy
8. src/app.config
Copy<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <startup>
    <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.0" />
  </startup>
  <system.net>
    <settings>
      <httpWebRequest useUnsafeHeaderParsing="true" />
    </settings>
  </system.net>
</configuration>
9. AIAgentTool.sln（方案檔）
CopyMicrosoft Visual Studio Solution File, Format Version 11.00
# Visual Studio 2010
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "AIAgentTool", "src\AIAgentTool.csproj", "{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{B1A2C3D4-E5F6-7890-ABCD-EF1234567890}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
GitHub 上的完整檔案樹
CopyAIAgentTool/                          ← Repository 根目錄
├── README.md                         ✅ 已完成
├── ARCHITECTURE.md                   ✅ 已完成
├── CONTRIBUTING.md                   ✅ 待撰寫
├── CHANGELOG.md                      ✅ 已完成
├── LICENSE                           ✅ 待撰寫
├── .gitignore                        ✅ 待撰寫
├── AIAgentTool.sln                   ✅ 待撰寫
│
└── src/
    ├── AIAgentTool.csproj            ✅ 待撰寫
    ├── app.config                    ✅ 待撰寫
    │
    ├── Program.cs                    ⬜ 待撰寫
    ├── MainForm.cs                   ⬜ 待撰寫
    ├── MainForm.Designer.cs          ⬜ 待撰寫
    ├── SettingsForm.cs               ⬜ 待撰寫
    ├── SettingsForm.Designer.cs      ⬜ 待撰寫
    │
    ├── Models/
    │   ├── SearchResult.cs           ⬜ 待撰寫
    │   ├── AgentTask.cs              ⬜ 待撰寫
    │   ├── KnowledgeItem.cs          ⬜ 待撰寫
    │   └── AppSettings.cs            ⬜ 待撰寫
    │
    ├── Services/
    │   ├── AI/
    │   │   ├── GeminiApiService.cs       ⬜ 待撰寫
    │   │   ├── DuckDuckGoAiService.cs    ⬜ 待撰寫
    │   │   └── AiRouter.cs              ⬜ 待撰寫
    │   ├── Search/
    │   │   ├── WebSearchService.cs       ⬜ 待撰寫
    │   │   ├── WikipediaService.cs       ⬜ 待撰寫
    │   │   └── WebScraperService.cs      ⬜ 待撰寫
    │   ├── System/
    │   │   ├── ProcessManagerService.cs  ⬜ 待撰寫
    │   │   ├── SystemAutomationService.cs⬜ 待撰寫
    │   │   └── FileManagerService.cs     ⬜ 待撰寫
    │   ├── CodeGen/
    │   │   ├── CodeGeneratorService.cs   ⬜ 待撰寫
    │   │   ├── CodeCompilerService.cs    ⬜ 待撰寫
    │   │   └── CodeTemplateLibrary.cs    ⬜ 待撰寫
    │   └── Core/
    │       ├── AIReasoningEngine.cs      ⬜ 待撰寫
    │       ├── TaskAutomationService.cs  ⬜ 待撰寫
    │       └── BackgroundTaskRunner.cs   ⬜ 待撰寫
    │
    └── Utils/
        ├── HtmlHelper.cs                 ⬜ 待撰寫
        └── ThreadSafeUI.cs               ⬜ 待撰寫
