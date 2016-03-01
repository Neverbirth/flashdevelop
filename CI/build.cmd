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

:: Build and run the tests
msbuild FlashDevelop.sln /p:Configuration=Release+Tests /p:Platform="x86" /t:Rebuild %MSBuildLogger%

powershell.exe -file tests.ps1

if %errorlevel% neq 0 goto :error

if "%APPVEYOR_PULL_REQUEST_NUMBER%" neq "" exit

del "FlashDevelop\Bin/Debug\*.Tests.*" /Q
del "FlashDevelop\Bin/Debug\NSubstitute.*" /Q
del "FlashDevelop\Bin/Debug\nunit.framework.*" /Q

msbuild FlashDevelop.sln /p:Configuration=Release /p:Platform="AnyCPU" /t:Rebuild %MSBuildLogger%

:: Check for build errors
if %errorlevel% neq 0 goto :error

:: Create the installer
makensis FlashDevelop\Installer\Installer.nsi

:: Check for nsis errors
if %errorlevel% neq 0 goto :error

:: Create the archive
7z a -tzip FlashDevelop\Installer\Binary\FlashDevelop.zip .\FlashDevelop\Bin\Debug\* -xr!.empty

:: Check for 7zip errors
if %errorlevel% neq 0 goto :error

:: Done
exit

:error
exit -1
