:: ===================================================
:: Tests tool for eidotnetrules
:: ===================================================
@if not defined LOGDEBUG set LOGDEBUG=off
@echo %LOGDEBUG%
SetLocal EnableDelayedExpansion

for /f "delims=/" %%a in ('cd') do set CURRENTDIR=%%a
for %%a in (%0) do set CMDDIR=%%~dpa
for %%a in (%0) do set CMDNAME=%%~na

set CMDPATH=%0
set RETCODE=1

:: Checking arguments
for %%a in (SRC_DIR WKSP) do set %%a=
:LOOP_ARG
    set option=%1
    echo option %1
    if not defined option goto CHECK_ARGS
    shift
    set value=%1
    if defined value set value=%value:"=%
    call set %option%=%%value%%
    shift
goto LOOP_ARG

:CHECK_ARGS
for %%a in (SRC_DIR WKSP) do (
    if not defined %%a (
        @echo.
        @echo No "%%a" defined !
        call :Usage
        goto endclean
    )
)

if not defined MODE set MODE=unit
if not defined ENGTOOLS set ENGTOOLS=\\productfs01\EngTools
set EXTERNAL_TOOLS=%ENGTOOLS%\external_Tools
set CACHEWIN64=c:\CAST-Caches\Win64
set EXTWIN64=%EXTERNAL_TOOLS%\win64
if not exist %CACHEWIN64% set PATH=%EXTWIN64%;%PATH%
if exist %CACHEWIN64%     set PATH=%CACHEWIN64%;%PATH%

set WORK_DIR=%WKSP%\work

for %%a in (%SRC_DIR% %WKSP%) do (
    if not exist %%a (
        @echo.
        @echo ERROR: Folder %%a does not exist
        goto endclean
    )
)

set DLLNUNIT=UnitTests.dll

pushd %WKSP%
if errorlevel 1 goto endclean

@echo.
@echo =======================================================
@echo Cleaning
@echo =======================================================
@echo.
for %%a in (%WORK_DIR%) do (
    if exist %%a (
        rmdir /s /q %%a
        sleep.exe 3
    )
    mkdir %%a
    if errorlevel 1 goto endclean
)

@echo.
@echo ================================================================
@echo === Extracting build package
@echo ================================================================
for /f %%a in ('dir /b /s %WKSP%\upload\eidotnetrules_test_pack.7z') do set TESTPACKPATH=%%a
if not exist %TESTPACKPATH% (
    @echo.
    @echo ERROR: package %TESTPACKPATH% does not exist from build job
    goto endclean
)
set CMD=7z.exe x -aoa %TESTPACKPATH% -o%WORK_DIR%
@echo.
@echo Executing:
@echo %CMD%
call %CMD%
if errorlevel 1 goto endclean

@echo.
@echo ================================================================
echo Logging the list of installed dotnet runtimes
@echo ================================================================
dotnet --list-runtimes

@echo.
@echo ================================================================
echo Logging the list of installed dotnet runtimes
@echo ================================================================
dotnet --list-sdks

@echo.
@echo ================================================================
@echo === Running tests with dotnet test (and nunit + NunitXml.TestLogger)
@echo ================================================================
pushd %WORK_DIR%
for %%a in (%DLLNUNIT%) do (
    @echo.
    set CMD=dotnet test %WORK_DIR%\%%a --logger:"nunit;LogFilePath=%WORK_DIR%\nunit.unit.%%a.nunit"
    @echo executing:
    @echo !CMD!
    !CMD! >%WKSP%\test_%MODE%_%%~na.log 2>&1
    @echo return code is: !ERRORLEVEL!
    if !ERRORLEVEL! LSS 0 (
        if exist %WORK_DIR%\nunit.unit.%%a.nunit (
            @echo.
            @echo Some errors appeared while Executing dll: %%a
            set RETCODE=2
        ) else (
            @echo.
            @echo ERROR: crash while Executing dll: %%a
            @echo return code is: !ERRORLEVEL!
            goto endclean
        )
    )
    @echo.
    @echo ================================================================
    @echo === XSLT transformation
    @echo ================================================================
    call msxsl.exe %WORK_DIR%\nunit.unit.%%a.nunit %SRC_DIR%\nunit3-to-junit.xsl -o %WKSP%\results_%MODE%_%%~na_junit.xml
    if errorlevel 1 goto endclean
    @echo %MODE% logs for %%~na are in %WKSP%\test_%MODE%_%%~na.log
)
popd
@echo retcode is: %RETCODE%

:endSuccess
if %RETCODE% neq 2 set RETCODE=0

:endclean
cd /d %CURRENTDIR%
exit /b %RETCODE%

:Usage
    @echo Usage:
    @echo %CMDPATH% wksp=... src_dir=...
    @echo.
    @echo wksp: full path to workspace
    @echo src_dir: path for dotnet sources folder.
    @echo.
    goto endclean
goto:eof
