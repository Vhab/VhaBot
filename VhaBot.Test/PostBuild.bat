@echo off

rem ========================
rem === Check build mode ===
rem ========================

IF NOT %3==Release GOTO :OK1
wscript "%~2\MessageBox.js" VhaBot.Test is not available in Release mode
exit /b 1
:OK1

rem ==================================
rem === Check if config.xml exists ===
rem ==================================

IF EXIST "%~1\Config.xml" GOTO OK2
wscript "%~2\MessageBox.js" Please create Config.xml as outlined in ReadMe.txt
exit /b 1
:OK2

rem ======================================
rem === Start building our debug setup ===
rem ======================================
cd /d %1
mkdir VhaBot.Test\bin\%3\Plugins
del /Q VhaBot.Test\bin\%3\Plugins\*

mkdir VhaBot.Test\bin\%3\data
copy /Y Extra\data\* VhaBot.Test\bin\%3\data

copy /Y Plugins.Default\*.cs VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins.Default\*.dll VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins.Raid\*.cs VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins.Raid\*.dll VhaBot.Test\bin\%3\Plugins\

IF NOT %3==Incubator GOTO :NO_INCUBATOR
copy /Y Plugins.Incubator\*.cs VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins.Incubator\*.dll VhaBot.Test\bin\%3\Plugins\
:NO_INCUBATOR

IF NOT %3==Helpbot GOTO :NO_HELPBOT
copy /Y Plugins.Helpbot\*.cs VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins.Helpbot\*.dll VhaBot.Test\bin\%3\Plugins\
:NO_HELPBOT

IF NOT %3==Vhanet GOTO :NO_VHANET
copy /Y Plugins.Vhanet\*.cs VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins.Vhanet\*.dll VhaBot.Test\bin\%3\Plugins\
:NO_VHANET

copy /Y config.xml VhaBot.Test\bin\%3\