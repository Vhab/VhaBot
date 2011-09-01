@echo off
cd %1

echo Creating directories
rmdir /s /q Package
mkdir Package
mkdir Package\Plugins
mkdir Package\data

echo Copying Assemblies
copy /Y AoLib\bin\Release\AoLib.dll Package\
copy /Y VhaBot.API\bin\Release\VhaBot.API.dll Package\
copy /Y VhaBot.API\bin\Release\Mono.Data.SqliteClient.dll Package\
copy /Y VhaBot.API\bin\Release\sqlite3.dll Package\
copy /Y VhaBot.Communication\bin\Release\VhaBot.Communication.dll Package\
copy /Y VhaBot.Configuration\bin\Release\VhaBot.Configuration.dll Package\
copy /Y VhaBot.ConfigurationTool\bin\Release\Configure.exe Package\
copy /Y VhaBot.Core\bin\Release\VhaBot.Core.dll Package\
copy /Y VhaBot.CorePlugins\bin\Release\VhaBot.CorePlugins.dll Package\
copy /Y VhaBot\bin\Release\VhaBot.exe Package\
copy /Y VhaBot.Shell\bin\Release\VhaBot.Shell.exe Package\
copy /Y VhaBot.Shell\bin\Release\VhaBot.Shell.exe.config Package\
copy /Y VhaBot.Lite\bin\Release\VhaBot.Lite.exe Package\
copy /Y VhaBot.Lite\bin\Release\VhaBot.Lite.exe.config Package\
copy /Y Plugins.Default\*.cs Package\Plugins\
copy /Y Plugins.Default\*.dll Package\Plugins\
copy /Y Plugins.Raid\*.cs Package\Plugins\
copy /Y Plugins.Raid\*.dll Package\Plugins\
copy /Y Extra\* Package\
copy /Y Extra\data\* Package\data\