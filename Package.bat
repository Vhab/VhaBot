@echo off
cd %1

echo Creating directories
mkdir Package
mkdir Package\Plugins

echo Copying Assemblies
copy /Y AoLib\bin\Release\AoLib.dll Package\
copy /Y VhaBot.API\bin\Release\VhaBot.API.dll Package\
copy /Y VhaBot.API\bin\Release\Mono.Data.SqliteClient.dll Package\
copy /Y VhaBot.API\bin\Release\sqlite3.dll Package\
copy /Y VhaBot.Communication\bin\Release\VhaBot.Communication.dll Package\
copy /Y VhaBot.Configuration\bin\Release\VhaBot.Configuration.dll Package\
copy /Y VhaBot.ConfigurationTool\bin\Release\Configure.exe Package\
copy /Y VhaBot.Core\bin\Release\VhaBot.Core.dll Package\
copy /Y VhaBot.Plugins\bin\Release\VhaBot.Plugins.dll Package\
copy /Y VhaBot\bin\Release\VhaBot.exe Package\
copy /Y VhaBot.Shell\bin\Release\VhaBot.Shell.exe Package\
copy /Y VhaBot.Shell\bin\Release\VhaBot.Shell.exe.config Package\
copy /Y VhaBot.Lite\bin\Release\VhaBot.Lite.exe Package\
copy /Y VhaBot.Lite\bin\Release\VhaBot.Lite.exe.config Package\
copy /Y Plugins\*.cs Package\Plugins\
copy /Y Plugins\*.dll Package\Plugins\
copy /Y Extra\* Package\