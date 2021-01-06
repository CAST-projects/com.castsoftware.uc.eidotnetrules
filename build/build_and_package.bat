::========================================================================================
::========================================================================================
::
:: This tool builds eidotnetrules
::
::========================================================================================
::========================================================================================

@if not defined LOGDEBUG set LOGDEBUG=off
@echo %LOGDEBUG%
SetLocal EnableDelayedExpansion
for /f "delims=/" %%a in ('cd') do set CURRENTPWD=%%a
for %%a in (%0) do set CMDDIR=%%~dpa
for %%a in (%0) do set TOOLNAME=%%~na
set CMDPATH=%0
set RETCODE=1

set NOPUB=false
for %%a in (WKSP SRCDIR RESDIR BUILDNO) do set %%a=
:LOOP_ARG
    set option=%1
    if not defined option goto CHECK_ARGS
    shift
    set value=%1
    if defined value set value=%value:"=%
    call set %option%=%%value%%
    shift
goto LOOP_ARG

:CHECK_ARGS
for %%a in (WKSP SRCDIR RESDIR BUILDDIR BUILDNO) do (
    if not defined %%a (
        @echo.
        @echo ERROR: parameter "%%a" has not been defined.
        @echo usage:
        @echo    %0 wksp= joburl= consdir=outdir= admindir= extenddir= css_data=  caip_mainvers= console_version= extend_version=
        goto endclean
    )
)

for %%a in (%WKSP% %SRCDIR% %BUILDDIR%) do (
    if not exist %%a (
        @echo.
        @echo ERROR: folder "%%a" does not exist.
        goto endclean
    )
)

set FILESRV=\\productfs01
if not defined ENGTOOLS set ENGTOOLS=%FILESRV%\EngTools
set SIGNDIR=%ENGTOOLS%\certificates
set PATH=%PATH%;C:\CAST-Caches\Win64
set NUNITDIR=%WKSP%\nunit
set XSLTDIR=%WKSP%\xslt
set RELEASE64=%WKSP%\Release
set VERSION=1.0.0

for /f "delims=. tokens=1,2" %%a in ('echo %VERSION%') do set SHORT_VERSION=%%a.%%b
echo.
echo Current version is %VERSION%
echo Current short version is %SHORT_VERSION%

cd %WKSP%
if errorlevel 1 (
	echo.
	echo ERROR: cannot find folder %WKSP%
	goto endclean
)
if not exist %SRCDIR% (
	echo.
	echo ERROR: cannot find folder %SRCDIR%
	goto endclean
)

echo.
echo ====================================
echo Get externals tools
echo ====================================
robocopy /mir /nc /nfl /ndl /np %ENGTOOLS%\external_tools\nunit\NUnit-2.6.3 %NUNITDIR%
if errorlevel 8 exit /b 1
robocopy /mir /nc /nfl /ndl /np %ENGTOOLS%\external_tools\xsltproc %XSLTDIR%
if errorlevel 8 exit /b 1

echo.
echo ==============================================
echo Cleaning ...
echo ==============================================
for %%a in (%RELEASE64% %RESDIR%) do (
    if exist %%a (
        echo Cleaning %%a
        rmdir /q /s %%a
        sleep.exe 3
    )
    mkdir %%a
    if errorlevel 1 goto endclean
)
pushd %RESDIR%
for /f "delims=/" %%a in ('cd') do set RESDIR=%%a
popd

pushd %SRCDIR%
echo.
echo ==============================================
echo Compiling main and tests ...
echo ==============================================
if errorlevel 1 goto endclean
call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\VC\vcvarsall.bat" x64
@echo %LOGDEBUG%
if errorlevel 1 goto endclean

msbuild sources\QRs.sln /p:Configuration="Release",OutputPath=%RELEASE64%
if errorlevel 1 (
	echo.
	echo ERROR: Main compilation failed
	goto endclean
)

pushd %WKSP%
echo.
echo ==============================================
echo Running tests ...
echo ==============================================
%NUNITDIR%\bin\nunit-console.exe /labels /noshadow %RELEASE64%\UnitTests.dll
if errorlevel 1 goto endclean
%XSLTDIR%\xsltproc.exe -v -o TestResult.junit.xml %NUNITDIR%\bin\nunit-to-junit.xsl TestResult.xml
if errorlevel 1 goto endclean

pushd %RELEASE64%
echo.
echo ==============================================
echo sign executables
echo ==============================================
call %SIGNDIR%\signtool.bat CastDotNetExtension.dll SHA256
if errorlevel 1 goto endclean
call %SIGNDIR%\signtool.bat EICastQualityRules.dll SHA256
if errorlevel 1 goto endclean
call %SIGNDIR%\signtool.bat UnitTests.dll SHA256
if errorlevel 1 goto endclean
call %SIGNDIR%\signtool.bat CastDotNetExtensionTestTools.dll SHA256
if errorlevel 1 goto endclean
popd

echo.
echo ==============================================
echo Preparing extension folder
echo ==============================================
robocopy /mir %SRCDIR%\nuget\package_files %RESDIR%
if errorlevel 8 goto endclean
robocopy /s %RELEASE64% %RESDIR% *.* /xf *.pdb
if errorlevel 8 goto endclean

pushd %WKSP%
echo.
echo ==============================================
echo Nuget packaging ...
echo ==============================================
sed -i 's/_THE_VERSION_/%VERSION%/' %RESDIR%/plugin.nuspec
if errorlevel 1 goto endclean
sed -i 's/_THE_SHORT_VERSION_/%SHORT_VERSION%/' %RESDIR%/plugin.nuspec
if errorlevel 1 goto endclean

set CMD=%BUILDDIR%\nuget_package_basics.bat outdir=%RESDIR% pkgdir=%RESDIR% buildno=%BUILDNO% nopub=%NOPUB% is_component=false
echo Executing command:
echo %CMD%
call %CMD%
if errorlevel 1 goto endclean

for /f "tokens=*" %%a in ('dir /b %RESDIR%\com.castsoftware.*.nupkg') do set PACKPATH=%RESDIR%\%%a
if not defined PACKPATH (
	echo .
	echo ERROR: No package was created : file not found %RESDIR%\com.castsoftware.*.nupkg ...
	goto endclean
)
if not exist %PACKPATH% (
	echo .
	echo ERROR: File not found %PACKPATH% ...
	goto endclean
)

set GROOVYEXE=groovy
%GROOVYEXE% --version 2>nul
if errorlevel 1 set GROOVYEXE="%GROOVY_HOME%\bin\groovy"
%GROOVYEXE% --version 2>nul
if errorlevel 1 (
	echo ERROR: no groovy executable available, need one!
	goto endclean
)

:: ========================================================================================
:: Nuget checking
:: ========================================================================================
set CMD=%GROOVYEXE% %BUILDDIR%\nuget_package_verification.groovy --packpath=%PACKPATH%
echo Executing command:
echo %CMD%
call %CMD%
if errorlevel 1 goto endclean

echo End of build with success.
set RETCODE=0

:endclean
cd /d %CURRENTPWD%
exit /b %RETCODE%


:Usage
    echo usage:
    echo %0 wksp=^<path^> srcdir=^<path^> builddir=^<path^> RESDIR=^<path^> buildno=^<number^>
    echo.
    echo wksp: full path to the WKSP dir
    echo srcdir: sources directory full path
    echo builddir: extension build directory full path
    echo resdir: output directory full path
    echo buildno: build number: build number for this package
    echo.
    goto endclean
goto:eof
