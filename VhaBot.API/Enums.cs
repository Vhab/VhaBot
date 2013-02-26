using System;
using System.Collections;
using AoLib.Net;

namespace VhaBot
{
    public enum BotState
    {
        Connected,
        Connecting,
        Disconnected
    }

    public enum CommandType
    {
        PrivateChannel,
        Tell,
        Organization
    }

    public enum PluginState
    {
        Core,
        Disabled,
        Installed,
        Loaded
    }

    public enum ChannelType
    {
        Announcements,
        General,
        Organization,
        Japanese,
        Shopping,
        Towers,
        Unknown
    }

    public enum UserLevel
    {
        Disabled = 1024,
        SuperAdmin = 16,
        Admin = 8,
        Leader = 4,
        Member = 2,
        Guest = 1
    }

    public enum OnlineState
    {
        Online,
        Offline,
        Timeout,
        Unknown
    }

    public enum PluginLoadResult
    {
        Ok,
        DepencencyNotFound,
        DepencencyNotLoaded,
        CommandConflict,
        AlreadyLoaded,
        NotFound,
        NotInstalled,
        OnLoadError
    }

    public enum ConfigType
    {
        String,     //.*
        Password,   //.*
        Integer,    //[0-9]+
        Boolean,    //(true|false)
        Username,   //([a-z]+[0-9]+){4,13}
        Date,       //([0-2][0-9]|3[0-1])/([0]?[0-9]|1[0-2])/[0-9]{4}
        Time,       //([0-1]?[0-9]|2[0-4]):([0-5][0-9]|60):([0-5][0-9]|60)
        Dimension,  //(Test|RubiKa)
        Color,      //[#]?[0-9a-f]{6}
        Custom      //.*
    }

    public enum AssemblyType
    {
        Binary,
        Source,
        Buildin
    }

    public enum IntraBotMessageState
    {
        Success,
        InvalidBot,
        InvalidPlugin,
        Error
    }
}
