if "%DevEnvDir%"=="" goto Error;

msbuild BB74.Xwt.Controls.csproj /p:TargetFrameworkVersion=v4.0;Configuration=Release,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error
msbuild BB74.Xwt.Controls.csproj /p:TargetFrameworkVersion=v4.5;Configuration=Release,Platform=AnyCPU /p:OutputPath=.\package\lib\net45
IF ERRORLEVEL 1 GOTO Error

getversion package\lib\net40\BB74.Xwt.Controls.dll BB74.Xwt.Controls._nuspec BB74.Xwt.Controls.nuspec
IF ERRORLEVEL 1 GOTO Error

nuget pack BB74.Xwt.Controls.nuspec -BasePath .\package -properties configuration=Release
IF ERRORLEVEL 1 GOTO Error

goto exit

rem "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" ..\BB74.Controls.Xwt.sln /rebuild Release /project BB74.Xwt.Controls
rem IF ERRORLEVEL 1 GOTO Error
nuget pack BB74.Xwt.Controls.csproj -properties configuration=Release
IF ERRORLEVEL 1 GOTO Error
goto exit
:Error
echo "error"
pause
:exit