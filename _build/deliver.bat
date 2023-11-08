:: ====================================================================================================================
:: basic operations for nuget plugin generation

@if not defined LOGDEBUG set LOGDEBUG=off
@echo %LOGDEBUG%
SetLocal EnableDelayedExpansion

set RETCODE=1
for /f "delims=/" %%a in ('cd') do set WORKSPACE=%%a
for %%a in (%0) do set CMDDIR=%%~dpa
set CMDPATH=%0

cd /d %WORKSPACE%

:: Checking arguments
set SRC_DIR=
set TOOLS_DIR=
set PACK_DIR=
set BUILDNO=

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
if not defined SRC_DIR (
	echo.
	echo No "src_dir" defined !
	goto endclean
)
if not defined TOOLS_DIR (
	echo.
	echo No "tools_dir" defined !
	goto endclean
)
if not defined PACK_DIR (
	echo.
	echo No "pack_dir" defined !
	goto endclean
)
if not defined BUILDNO (
	echo.
	echo No "buildno" defined !
	goto endclean
)

for %%a in (%SRC_DIR% %TOOLS_DIR%) do (
    if not exist %%a (
        echo.
        echo ERROR: Folder %%a does not exist
        goto endclean
    )
)

set THEDELIVDIR=upload
for %%a in (%PACK_DIR%) do (
    if exist %%a rmdir /s /q %%a
    mkdir %%a
    if errorlevel 1 goto endclean
)

set ROBOPT=/ndl /njh /njs /np
robocopy /mir %ROBOPT% %SRC_DIR%\nuget\package_files %PACK_DIR%
if errorlevel 8 goto endclean

echo.
echo Get additional files
xcopy /y /s %SRC_DIR%\nuget\package_files\InstallScripts\*.* %PACK_DIR%\InstallScripts\
if errorlevel 1 goto endclean
xcopy /y /s %SRC_DIR%\nuget\package_files\MasterFiles\*.* %PACK_DIR%\MasterFiles\
if errorlevel 1 goto endclean
xcopy /y /s %SRC_DIR%\nuget\package_files\Configuration\*.* %PACK_DIR%\Configuration\
if errorlevel 1 goto endclean

echo generation of adg
call \\PRODUCTFS01\EngBuild\Releases\AssessmentModel\MetricsCompiler.bat -encodeUA -inputdir %PACK_DIR%\MasterFiles\ -outputdir %PACK_DIR% -pck com.castsoftware.uc.eidotnetrules
if errorlevel 1 (
	echo.
	echo ERROR during MetricCompiler
	goto endclean
)

echo.
echo Extract Security Analyzer binaries
call \\productfs01\EngTools\external_tools\win64\7z.exe x upload\eidotnetrules_main_pack.7z -o%PACK_DIR%
if errorlevel 1 exit /b 1

set CMD=%TOOLS_DIR%\nuget_package_basics.bat outdir=%THEDELIVDIR% pkgdir=%PACK_DIR% buildno=%BUILDNO%
echo Executing command:
echo %CMD%
call %CMD%
if errorlevel 1 goto endclean

for /f "tokens=*" %%a in ('dir /b %THEDELIVDIR%\com.castsoftware.*.nupkg') do set PACKPATH=%THEDELIVDIR%\%%a
if not defined PACKPATH (
	echo .
	echo ERROR: No package was created : file not found %THEDELIVDIR%\com.castsoftware.*.nupkg ...
	goto endclean
)
if not exist %PACKPATH% (
	echo .
	echo ERROR: File not found %PACKPATH% ...
	goto endclean
)

set GROOVYEXE=groovy
call %GROOVYEXE% --version 2>nul
if errorlevel 1 set GROOVYEXE="%GROOVY_HOME%\bin\groovy"
call %GROOVYEXE% --version 2>nul
if errorlevel 1 (
	echo ERROR: no groovy executable available, need one!
	goto endclean
)

set CMD=%GROOVYEXE% %TOOLS_DIR%\nuget_package_verification.groovy --packpath=%PACKPATH%
echo Executing command:
echo %CMD%
call %CMD%
if errorlevel 1 goto endclean

echo.
echo Extension creation in SUCCESS
set RETCODE=0

:endclean
cd /d %WORKSPACE%
exit /b %RETCODE%
