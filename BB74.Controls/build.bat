set BB74_CONFIG=Release
set BB74_PUBLISH="default"
set BB74_VERSIONONLY=FALSE
:parse
IF "%~1"=="" GOTO endparse
IF "%~1"=="debug" set BB74_CONFIG=Debug
IF "%~1"=="release" set BB74_CONFIG=Release
IF "%~1"=="versiononly" set BB74_VERSIONONLY=true
IF "%~1"=="bert" set BB74_PUBLISH=bert
SHIFT
GOTO parse
:endparse

if "%BB74_VERSIONONLY%"=="TRUE" goto versiononly

IF "%BB74_CONFIG%"=="Release" goto release_build
set BB74_VERSION=-debug
goto _release_build
:release_build
set BB74_VERSION=
:_release_build

if NOT "%DevEnvDir%"=="" goto devenvok
call "c:\Program Files (x86)\Microsoft Visual Studio\2017\Community\VC\Auxiliary\Build\vcvarsx86_amd64.bat"
if "%DevEnvDir%"=="" goto Error
:devenvok

msbuild BB74.Xwt.Controls.csproj /t:Clean,Build /p:TargetFrameworkVersion=v4.0;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net40
IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Xwt.Controls.csproj /p:TargetFrameworkVersion=v4.5;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net45
rem IF ERRORLEVEL 1 GOTO Error
rem msbuild BB74.Xwt.Controls.csproj /p:TargetFrameworkVersion=v4.7.2;Configuration=%BB74_CONFIG%,Platform=AnyCPU /p:OutputPath=.\package\lib\net472
rem IF ERRORLEVEL 1 GOTO Error

:versiononly
getversion -version_ext "%BB74_VERSION%"  package\lib\net40\BB74.Xwt.Controls.dll BB74.Xwt.Controls._nuspec _tmp\BB74.Xwt.Controls.nuspec
IF ERRORLEVEL 1 GOTO Error

if "%BB74_VERSIONONLY%"=="TRUE" goto exit

nuget pack _tmp\BB74.Xwt.Controls.nuspec -BasePath .\package -properties configuration=%BB74_CONFIG% -OutputDirectory packages\
IF ERRORLEVEL 1 GOTO Error

if NOT "%BB74_PUBLISH%"=="bert" goto exit

getversion -version_ext "%BB74_VERSION%" package\lib\net40\BB74.Xwt.Controls.dll copy._bat _tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error

call _tmp\copy.bat
IF ERRORLEVEL 1 GOTO Error
goto exit

goto exit

:Error
echo "error"
pause
:exit

set BB74_CONFIG=
set BB74_VERSION=
set BB74_PUBLISH=
set BB74_VERSIONONLY=