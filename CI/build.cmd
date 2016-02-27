:: Builds the binary on the server for CI

:: Set paths
set PATH=%PATH%;C:\Windows\Microsoft.NET\Framework\v4.0.30319\
set PATH=%PATH%;C:\Program Files (x86)\Git\bin\
set PATH=%PATH%;C:\Program Files (x86)\NSIS
set PATH=%PATH%;C:\Program Files\7-Zip\

:: MSBuild logging

:flashdevelop

:: Check for build errors
if %errorlevel% neq 0 goto :error

:: Build the solutions
msbuild FlashDevelop.sln /p:Configuration=Release+Tests /p:Platform="Any CPU" /t:Rebuild %MSBuildLogger%

:: Done
exit

:error

exit -1
