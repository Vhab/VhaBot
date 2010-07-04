@echo off
cd /d %1

mkdir VhaBot.Test\bin\%2\Plugins

copy /Y Plugins\*.cs VhaBot.Test\bin\%2\Plugins\
copy /Y Plugins\*.dll VhaBot.Test\bin\%2\Plugins\
IF EXIST config.xml copy /Y config.xml VhaBot.Test\bin\%2\