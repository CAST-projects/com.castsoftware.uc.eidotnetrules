::========================================================================================
::========================================================================================
::
:: This tool builds eidotnetrules (v2)
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

for %%a in (WKSP SRCDIR RUNTIME) do set %%a=
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
for %%a in (WKSP SRCDIR RUNTIME) do (
    if not defined %%a (
        @echo.
        @echo ERROR: parameter "%%a" has not been defined.
        goto endclean
    )
)

for %%a in (%WKSP% %SRCDIR%) do (
    if not exist %%a (
        @echo.
        @echo ERROR: folder "%%a" does not exist.
        goto endclean
    )
)

if not defined ENGTOOLS set ENGTOOLS=\\productfs01\EngTools
set CASTCACHES=C:\CAST-Caches
set PATH=%PATH%;%CASTCACHES%\Win64
set TEMP=%WORKSPACE%\temp
set TARGET=Release

set BINDIR=%WKSP%\Build\x64\Release
touch.exe %WKSP%\WorkspaceRootMarker
if errorlevel 1 goto endclean

@echo loading 64bits VC environment
set FRAMEWORK=net6.0
set VCVARSALL="C:\Program Files\Microsoft Visual Studio\2022\Professional\VC\Auxiliary\Build\vcvarsall.bat"
if not exist %VCVARSALL% (
    @echo.
    @echo ERROR: file %VCVARSALL% does not exist
    goto endclean
)
set CMD=%VCVARSALL% x86_amd64
@echo Executing %CMD%
call %CMD%
@echo %LOGDEBUG%
if errorlevel 1 goto endclean

:: For Linux, we wait for 9 seconds in order to avoid nuget concurrence on the same file
if not %RUNTIME%==win-x64 (TIMEOUT 9)

set CMD=dotnet.exe publish %SRCDIR%\Sources\EICastQualityRules.csproj --framework %FRAMEWORK% -c %TARGET% --runtime %RUNTIME% /p:PublishReadyToRun=true /p:Platform=x64
@echo Executing %CMD%
call %CMD%
if errorlevel 1 goto endclean

if errorlevel 1 goto endclean
if %BUILDTYPE%.==noc. (
	if %RUNTIME%==win-x64 (
		set CMD=%ENGTOOLS%\certificates\sign_exe_dll.bat bindir=%BINDIR%
		@echo Executing !CMD!
		call !CMD!
		if errorlevel 1 goto endclean
	)
)

if not defined BUILDNO (set SUFFIX=) else (SET SUFFIX=_%BUILDNO%)

@echo SUFFIX=%SUFFIX%

:: ===================================================
:: Main packages
:: ===================================================

pushd %BINDIR%
if not %RUNTIME%==win-x64 (
    @echo.
    @echo ==========================================
    @echo Creating Linux package
    @echo ==========================================
    set ARCHIVE=%WKSP%\upload\eidotnetrules_main_pack.zip
    @echo ARCHIVE=!ARCHIVE!
    IF EXIST !ARCHIVE! del !ARCHIVE!
    @echo on
    call 7z.exe -tzip a !ARCHIVE! ^
		ref\ ^
		runtimes\ ^
        *.dll ^
        *.exe ^
        *.dll.config ^
        *.deps.json ^
        *.so ^
        *.a ^
        *.runtimeconfig.dev.json ^
        *.runtimeconfig.json ^
        createdump ^
        -xr^^!Microsoft.TestPlatform.*.dll ^
        -xr^^!Microsoft.VisualStudio.CodeCoverage.Shim.dll ^
        -xr^^!Microsoft.VisualStudio.TestPlatform*.dll ^
        -xr^^!NuGet*.dll ^
        -xr^^!nunit*.dll ^
        -xr^^!nunit*.dll ^
        -xr^^!testcentric.*.dll ^
        -xr^^!testhost*.dll ^
        -xr^^!testhost*.exe
    @echo %LOGDEBUG%
    if errorlevel 1 goto endclean
) else (
    @echo.
    @echo ==========================================
    @echo Creating Windows package
    @echo ==========================================
    set ARCHIVE=%WKSP%\upload\eidotnetrules_main_pack.7z
    IF EXIST !ARCHIVE! del !ARCHIVE!
    @echo on
    call 7z.exe a !ARCHIVE! ^
		ref\ ^
		runtimes\ ^
        *.dll ^
        *.exe ^
        *.dll.config ^
        *.deps.json ^
        *.runtimeconfig.dev.json ^
        *.runtimeconfig.json ^
        -xr^^!Microsoft.TestPlatform.*.dll ^
        -xr^^!Microsoft.VisualStudio.CodeCoverage.Shim.dll ^
        -xr^^!Microsoft.VisualStudio.TestPlatform*.dll ^
        -xr^^!NuGet*.dll ^
        -xr^^!nunit*.dll ^
        -xr^^!nunit*.dll ^
		-xr^^!testcentric.*.dll ^
        -xr^^!testhost*.dll ^
        -xr^^!testhost*.exe
    @echo %LOGDEBUG%
    if errorlevel 1 goto endclean
)
popd

:: ===================================================
:: Build Tests packages
:: ===================================================
set CMD=dotnet.exe build %SRCDIR%\UnitTests\UnitTests.csproj -c %TARGET% --runtime %RUNTIME%  /p:PublishReadyToRun=true /p:Platform=x64
@echo Executing %CMD%
call %CMD%
if errorlevel 1 goto endclean

@echo Add XSLT translator tool
XCOPY "%SRCDIR%\build\nunit3-to-junit.xsl" "%BINDIR%\" /Y

:: ===================================================
:: Tests packages
:: ===================================================

pushd %BINDIR%
if not %RUNTIME%==win-x64 (
    @echo.
    @echo ==========================================
    @echo Creating Linux package
    @echo ==========================================
    set ARCHIVE=%WKSP%\upload\eidotnetrules_test_pack.zip
    IF EXIST !ARCHIVE! del !ARCHIVE!
    @echo on
    call 7z.exe -tzip a !ARCHIVE! ^
		ref\ ^
		runtimes\ ^
        *.dll ^
        *.exe ^
        *.xsl ^
        *.dll.config ^
        *.deps.json ^
        *.so ^
        *.a ^
        *.runtimeconfig.dev.json ^
        *.runtimeconfig.json
    @echo %LOGDEBUG%
    if errorlevel 1 goto endclean
) else (
    @echo.
    @echo ==========================================
    @echo Creating Windows package
    @echo ==========================================
    set ARCHIVE=%WKSP%\upload\eidotnetrules_test_pack.7z
    IF EXIST !ARCHIVE! del !ARCHIVE!
    @echo on
    call 7z.exe a !ARCHIVE! ^
		ref\ ^
		runtimes\ ^
        *.dll ^
        *.exe ^
        *.xsl ^
        *.dll.config ^
        *.deps.json ^
        *.runtimeconfig.dev.json ^
        *.runtimeconfig.json
    @echo %LOGDEBUG%
    if errorlevel 1 goto endclean
)
popd

@echo.
@echo Build in SUCCESS
set RETCODE=0

:endclean
exit /b %RETCODE%

:Usage
    @echo.
    @echo Usage: %CMDPATH% srcdir=^<path to carl sources folder^> target=^<build target: %TARGET% or Debug^> runtime=^<win-x64 or linux-x64^> buildtype=^<inc or noc^>
    @echo.
    goto endclean
goto:eof
