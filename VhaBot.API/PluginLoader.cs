using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace VhaBot
{
    public class PluginLoader
    {
        public readonly string Type;
        public readonly string File;
        private readonly Assembly Assembly;
        public readonly AssemlbyType AssemblyType;

        public readonly string Name;
        public readonly string InternalName;
        public readonly int Version;
        public readonly string Author;
        public readonly string Description;
        public readonly PluginState DefaultState;
        public readonly string[] Dependencies;
        public readonly Command[] Commands;

        public PluginLoader(string file, string type, AssemlbyType assemblyType, Assembly assembly)
        {
            this.Assembly = assembly;
            this.Type = type;
            this.File = file;
            this.AssemblyType = assemblyType;
            
            PluginBase plugin = this.CreatePlugin();
            plugin.Init();

            if (plugin.Name == null ||
                plugin.Name == string.Empty ||
                plugin.InternalName == null ||
                plugin.InternalName == string.Empty ||
                plugin.InternalName.Contains(" ") ||
                plugin.Version < 0)
            { throw new Exception("invalid plugin"); }

            this.Name = plugin.Name;
            this.InternalName = plugin.InternalName;
            this.Version = plugin.Version;
            this.Author = plugin.Author;
            if (plugin.Description != null)
                this.Description = plugin.Description;
            else
                this.Description = string.Empty;
            this.DefaultState = plugin.DefaultState;
            if (plugin.Dependencies != null)
                this.Dependencies = plugin.Dependencies;
            else
                this.Dependencies = new string[0];
            if (plugin.Commands != null)
                this.Commands = plugin.Commands;
            else
                this.Commands = new Command[0];
        }

        internal PluginBase CreatePlugin()
        {
            try
            {
                PluginBase plugin = (PluginBase)this.Assembly.CreateInstance(this.Type);
                return plugin;
            }
            catch
            {
                throw new Exception("Unable to create plugin!");
            }
        }

        public override string ToString()
        {
            return this.Name + " v" + this.Version;
        }
    }
}
