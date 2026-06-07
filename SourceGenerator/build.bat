@echo off
chcp 65001 >nul
echo ==========================================
echo  YGZFrameWork CfgTool Source Generator 编译
echo ==========================================
echo.

set "PROJECT_DIR=%~dp0"
cd /d "%PROJECT_DIR%"

where dotnet >nul 2>nul
if %errorlevel% neq 0 (
    echo [错误] 未找到 dotnet CLI，请先安装 .NET SDK 5.0+
    echo 下载地址: https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/3] 还原 NuGet 包...
dotnet restore "YGZFrameWork.CfgTool.SourceGenerator.csproj"
if %errorlevel% neq 0 (
    echo [错误] 还原失败
    pause
    exit /b 1
)

echo [2/3] 编译 Source Generator...
dotnet build "YGZFrameWork.CfgTool.SourceGenerator.csproj" -c Release
if %errorlevel% neq 0 (
    echo [错误] 编译失败
    pause
    exit /b 1
)

echo [3/3] 复制到 Unity 项目...
set "DLL_SOURCE=bin\Release\netstandard2.0\YGZFrameWork.CfgTool.SourceGenerator.dll"
set "DLL_TARGET=..\Assets\2_Scripts\Framework\ConfigTool\Editor\YGZFrameWork.CfgTool.SourceGenerator.dll"

if not exist "..\Assets\2_Scripts\Framework\ConfigTool\Editor" (
    mkdir "..\Assets\2_Scripts\Framework\ConfigTool\Editor"
)

copy /Y "%DLL_SOURCE%" "%DLL_TARGET%"
if %errorlevel% neq 0 (
    echo [警告] 复制 dll 失败，请手动复制:
    echo   从: %DLL_SOURCE%
    echo   到: %DLL_TARGET%
) else (
    echo [成功] Source Generator 已复制到 Unity 项目
)

echo.
echo ==========================================
echo  编译完成！
echo ==========================================
echo.
echo 下一步：
echo   1. 确保 dll 已复制到 Assets/2_Scripts/Framework/ConfigTool/Editor/
echo   2. 在 Unity 中创建/更新 asmdef 引用（详见 README_SourceGenerator.md）
echo   3. 返回 Unity，等待编译完成
echo.
pause
