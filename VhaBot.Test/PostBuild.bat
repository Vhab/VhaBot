@echo off
rem Check if config.xml exists
IF EXIST "%~1\Config.xml" GOTO OK

rem Display error
wscript "%~2\MessageBox.js" Please create Config.xml as outlined in ReadMe.txt
exit /b 1

:OK
cd /d %1

mkdir VhaBot.Test\bin\%3\Plugins
del /Q VhaBot.Test\bin\%3\Plugins\*

copy /Y Plugins\*.cs VhaBot.Test\bin\%3\Plugins\
copy /Y Plugins\*.dll VhaBot.Test\bin\%3\Plugins\
IF EXIST config.xml copy /Y config.xml VhaBot.Test\bin\%3\