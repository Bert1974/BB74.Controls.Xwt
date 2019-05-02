"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\devenv.com" ..\BB74.Controls.Xwt.sln /rebuild Release /project BB74.Xwt.Controls
IF ERRORLEVEL 1 GOTO Error
nuget pack BB74.Xwt.Controls.csproj -properties configuration=Release
IF ERRORLEVEL 1 GOTO Error
goto exit
:Error
echo "error"
pause
:exit