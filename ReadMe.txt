This instruction file will explain how to run and debug VhaBot from source.

First of all, to run and debug VhaBot you need an Anarchy Online account and character to run the bot on.
In order to automatically debug VhaBot with the character there is some manual setup required.

1. Copy and rename '/Extra/config.xml.example' to '/config.xml'.
   Don't worry, SVN is setup to automatically ignore this file.
   Your account details will not be committed to SVN.
2. Edit '/config.xml' with your account and character details
3. Open 'VhaBot2008.sln'
4. Inside Visual Studio: Right click 'VhaBot.Test' and select 'Set as StartUp Project'
5a. Select 'Debug' as target when working on Default and Raid plugins
5b. Select 'Incubator' as target when working on Incubator plugins
6. Hit F5 to compile and debug the application as you would normally