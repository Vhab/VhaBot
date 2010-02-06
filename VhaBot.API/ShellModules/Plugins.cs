using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;

namespace VhaBot.ShellModules
{
    public class Plugins
    {
        private BotShell Parent;
        private Config Config;
        private SortedDictionary<string, PluginLoader> PluginLoaders;
        private Dictionary<string, PluginBase> PluginsList;
        private string PluginsPath;
        private bool SafeMode = false;

        public Plugins(BotShell parent, string path)
        {
            this.PluginsPath = path;
            this.Parent = parent;
            this.PluginsList = new Dictionary<string, PluginBase>();
            this.Config = new Config(this.Parent.ToString(), "plugins");
            this.Config.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS CORE_Plugins (InternalName VARCHAR(255) UNIQUE, State VARCHAR(255), Version INTEGER)");
            this.Scan();
        }

        private void Scan()
        {
            try
            {
                BotShell.Output("[Plugins] Starting Plugin Scan...");
                if (this.PluginLoaders == null)
                    this.PluginLoaders = new SortedDictionary<string, PluginLoader>();

                lock (this.PluginLoaders)
                    this.PluginLoaders.Clear();

                List<ScannedAssembly> assemblies = new List<ScannedAssembly>();

                // Add this Assemlby to the list
                assemblies.Add(new ScannedAssembly(Assembly.LoadFrom("VhaBot.Plugins.dll"), AssemlbyType.Buildin, "VhaBot.Plugins.dll"));

                // Scan for DLLs
                string dllPath = this.PluginsPath.Contains(";") ? this.PluginsPath.Substring(0, this.PluginsPath.IndexOf(';')) : this.PluginsPath;
                string[] dlls = new string[0];
                try { dlls = Directory.GetFiles("." + Path.DirectorySeparatorChar + dllPath + Path.DirectorySeparatorChar, "*.dll"); }
                catch { }
                foreach (string dll in dlls)
                {
                    string shortDll = Path.GetFileName(dll);
                    if (shortDll.Length < 1 || shortDll.ToCharArray()[0] == '_')
                    {
                        BotShell.Output("[Plugins] Skipped DLL: " + shortDll, true);
                        continue;
                    }
                    BotShell.Output("[Plugins] Found DLL: " + shortDll, true);
                    try
                    {
                        assemblies.Add(new ScannedAssembly(Assembly.LoadFrom(dll), AssemlbyType.Binary, shortDll));
                    }
                    catch (Exception ex)
                    {
                        BotShell.Output("[Plugins] Failed Loading DLL " + shortDll + ": " + ex.ToString());
                    }
                }
               
                // Scan for plugin files
                string[] paths = this.PluginsPath.Split(new char[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string path in paths)
                {
                    if (path == null || path == string.Empty) continue;
                    if (!Directory.Exists(path)) continue;
                    List<string> filesSharp = new List<string>();
                    foreach (string file in Directory.GetFiles(path + Path.DirectorySeparatorChar, "*.cs"))
                        filesSharp.Add(file);

                    List<string> filesBasic = new List<string>();
                    foreach (string file in Directory.GetFiles(path + Path.DirectorySeparatorChar, "*.vb"))
                        filesBasic.Add(file);

                    if (filesSharp.Count > 0 || filesBasic.Count > 0)
                    {
                        CodeDomProvider csCompiler = new Microsoft.CSharp.CSharpCodeProvider();
                        CodeDomProvider vbCompiler = new Microsoft.VisualBasic.VBCodeProvider();
                        CompilerParameters options = new CompilerParameters();
                        options.CompilerOptions = "/target:library /optimize";
                        options.GenerateExecutable = false;
                        options.GenerateInMemory = true;
                        options.IncludeDebugInformation = false;

                        // .NET related assemblies
                        options.ReferencedAssemblies.Add("mscorlib.dll");
                        options.ReferencedAssemblies.Add("System.dll");
                        options.ReferencedAssemblies.Add("System.Data.dll");
                        options.ReferencedAssemblies.Add("System.Xml.dll");
                        options.ReferencedAssemblies.Add("System.Web.dll");

                        // VhaBot related assemblies
                        options.ReferencedAssemblies.Add("AoLib.dll");
                        options.ReferencedAssemblies.Add("VhaBot.API.dll");
                        options.ReferencedAssemblies.Add("VhaBot.Communication.dll");

                        // Add reference DLL's
                        foreach (ScannedAssembly assembly in assemblies)
                            if (assembly.Type == AssemlbyType.Binary)
                                options.ReferencedAssemblies.Add(dllPath + Path.DirectorySeparatorChar + assembly.File);

                        for (int i = 0; i < 2; i++)
                        {
                            try
                            {
                                CompilerResults results = null;
                                if (i == 0)
                                {
                                    if (filesSharp.Count < 1) continue;
                                    results = csCompiler.CompileAssemblyFromFile(options, filesSharp.ToArray());
                                }
                                else
                                {
                                    if (filesBasic.Count < 1) continue;
                                    results = csCompiler.CompileAssemblyFromFile(options, filesBasic.ToArray());
                                }

                                List<string> shortFiles = new List<string>();
                                if (i == 0)
                                    foreach (string file in filesSharp)
                                        shortFiles.Add(Path.GetFileName(file));
                                else
                                    foreach (string file in filesBasic)
                                        shortFiles.Add(Path.GetFileName(file));
                                string shortFile = string.Join(", ", shortFiles.ToArray());

                                bool errors = false;
                                if (results.Errors.Count > 0)
                                {
                                    foreach (CompilerError error in results.Errors)
                                    {
                                        if (error.IsWarning) continue;
                                        BotShell.Output("[Plugins] Compile Error: " + error.ErrorText + " (" + error.FileName + ":" + error.Line + ")");
                                        this.SafeMode = true;
                                        errors = true;
                                    }
                                }
                                if (!errors)
                                {
                                    BotShell.Output("[Plugins] Compiled: " + shortFile);
                                    assemblies.Add(new ScannedAssembly(results.CompiledAssembly, AssemlbyType.Source, path));
                                }
                            }
                            catch (Exception ex)
                            {
                                BotShell.Output("[Plugins] Error while compiling: " + ex.ToString());
                                this.SafeMode = true;
                            }
                        }
                    }
                }
                
                // Scan for plugins
                foreach (ScannedAssembly assembly in assemblies)
                {
                    Type[] types = assembly.Assembly.GetExportedTypes();
                    foreach (Type _type in types)
                    {
                        string type = _type.FullName;
                        try
                        {
                            PluginLoader loader = new PluginLoader(assembly.File, type, assembly.Type, assembly.Assembly);
                            lock (this.PluginLoaders)
                            {
                                if (this.PluginLoaders.ContainsKey(loader.InternalName.ToLower()))
                                {
                                    BotShell.Output("[Error] Internal Name (" + loader.InternalName + ") Already in Use!", true);
                                    BotShell.Output("[Error] Plugin (" + loader.ToString() + ") Conflicts With Plugin (" + this.PluginLoaders[loader.InternalName.ToLower()].ToString() + ")");
                                    BotShell.Output("[Error] Skipped Object " + loader.Type + " in " + loader.File + "!", true);
                                }
                                else
                                {
                                    if (loader.DefaultState != PluginState.Core && this.SafeMode) continue; // When using SafeMode, load only core plugins
                                    if (loader.Dependencies.Length > 0 && loader.DefaultState == PluginState.Loaded)
                                    {
                                        BotShell.Output("[Error] Plugins with dependencies can't have DefaultState = Loaded (" + loader.InternalName + ")! Plugin skipped!");
                                        continue;
                                    }
                                    if (loader.Dependencies.Length > 0 && loader.DefaultState == PluginState.Core)
                                    {
                                        BotShell.Output("[Error] Core plugins can't have dependencies (" + loader.InternalName + ")! Plugin skipped!");
                                        continue;
                                    }
                                    if (loader.AssemblyType != AssemlbyType.Buildin && loader.DefaultState == PluginState.Core)
                                    {
                                        BotShell.Output("[Error] Only buildin plugins can have DefaultState = Core (" + loader.InternalName + ")! Plugin skipped!");
                                        continue;
                                    }
                                    this.PluginLoaders.Add(loader.InternalName.ToLower(), loader);
                                    BotShell.Output("[Plugins] Detected Plugin: " + loader.ToString(), true);
                                }
                            }
                        }
                        catch { }
                    }
                }
                BotShell.Output("[Plugins] Found " + this.PluginLoaders.Count + " Plugins");
                return;
            }
            catch (Exception ex)
            {
                BotShell.Output("[Plugins] Unknown error during plugin scanning! " + ex.ToString());
                return;
            }
        }

        internal void LoadPlugins()
        {
            lock (this.PluginLoaders)
                foreach (KeyValuePair<string, PluginLoader> kvp in this.PluginLoaders)
                    if (kvp.Value.DefaultState == PluginState.Core)
                        this.Load(kvp.Key);

            if (this.SafeMode)
            {
                BotShell.Output("[Plugins] Loaded core plugins in SafeMode");
                return;
            }

            List<string> ignore = new List<string>();
            List<string> load = new List<string>();
            List<string> remove = new List<string>();
            using (IDbCommand command = this.Config.Connection.CreateCommand())
            {
                command.CommandText = "SELECT InternalName, State FROM CORE_Plugins";
                IDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string internalName = reader.GetString(0);
                    PluginState state = (PluginState)Enum.Parse(typeof(PluginState), reader.GetString(1));
                    if (this.GetDefaultState(internalName) == PluginState.Core || !this.Exists(internalName))
                    {
                        remove.Add(internalName);
                        continue;
                    }
                    if (state == PluginState.Loaded)
                        load.Add(internalName);
                    ignore.Add(internalName.ToLower());
                }
                reader.Close();
            }

            foreach (string internalName in remove)
                this.Config.ExecuteNonQuery("DELETE FROM CORE_Plugins WHERE InternalName = '" + internalName.ToLower() + "'");

            foreach (string internalName in load)
                this.Load(internalName);

            lock (this.PluginLoaders)
                foreach (KeyValuePair<string, PluginLoader> kvp in this.PluginLoaders)
                    if (kvp.Value.DefaultState == PluginState.Loaded && !ignore.Contains(kvp.Key.ToLower()))
                        this.Load(kvp.Key);
        }

        public bool IsLoaded(string internalName)
        {
            lock (this.PluginsList)
                return this.PluginsList.ContainsKey(internalName.ToLower());
        }

        public PluginState GetState(string internalName)
        {
            PluginState state = this.GetConfiguredState(internalName);
            if (state == PluginState.Core)
                return state;

            if (this.IsLoaded(internalName))
                state = PluginState.Loaded;
            else
                if (state == PluginState.Loaded)
                    state = PluginState.Installed;
            return state;
        }

        public PluginState GetConfiguredState(string internalName)
        {
            internalName = internalName.ToLower();
            PluginState state = this.GetDefaultState(internalName);
            if (state == PluginState.Core)
                return state;

            try
            {
                using (IDbCommand command = this.Config.Connection.CreateCommand())
                {
                    command.CommandText = "SELECT State FROM CORE_Plugins WHERE InternalName = '" + internalName + "'";
                    IDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                        state = (PluginState)Enum.Parse(typeof(PluginState), reader.GetString(0));
                    reader.Close();
                }
            }
            catch { }
            if (state == PluginState.Core)
                state = PluginState.Disabled;
            return state;
        }

        public PluginState GetDefaultState(string internalName)
        {
            internalName = internalName.ToLower();
            lock (this.PluginLoaders)
                if (this.PluginLoaders.ContainsKey(internalName))
                    return this.PluginLoaders[internalName].DefaultState;
            return PluginState.Disabled;
        }

        public string GetName(string internalName)
        {
            if (!this.Exists(internalName))
                return string.Empty;
            return this.GetLoader(internalName).Name + " v" + this.GetLoader(internalName).Version;
        }

        public PluginLoader GetLoader(string internalName)
        {
            internalName = internalName.ToLower();
            lock (this.PluginLoaders)
                if (this.PluginLoaders.ContainsKey(internalName))
                    return this.PluginLoaders[internalName];
                else
                    return null;
        }

        public PluginBase GetPlugin(string internalName)
        {
            internalName = internalName.ToLower();
            lock (this.PluginsList)
                if (this.PluginsList.ContainsKey(internalName))
                    return this.PluginsList[internalName];
                else
                    return null;
        }

        public string[] GetPlugins()
        {
            List<string> list = new List<string>();
            lock (this.PluginLoaders)
            {
                foreach (string key in this.PluginLoaders.Keys)
                    list.Add(key.ToLower());
            }
            list.Sort();
            return list.ToArray();
        }

        public string[] GetLoadedPlugins()
        {
            List<string> list = new List<string>();
            lock (this.PluginsList)
            {
                foreach (string key in this.PluginsList.Keys)
                    list.Add(key.ToLower());
            }
            return list.ToArray();
        }

        public bool Exists(string internalName)
        {
            lock (this.PluginLoaders)
                return this.PluginLoaders.ContainsKey(internalName.ToLower());
        }

        public PluginLoadResult Load(string internalName)
        {
            PluginState state = this.GetState(internalName);
            if (state == PluginState.Loaded)
            {
                BotShell.Output("[Error] Plugin (" + this.GetLoader(internalName).ToString() + ") is Already Loaded!");
                return PluginLoadResult.AlreadyLoaded;
            }
            if (state != PluginState.Installed && state != PluginState.Core)
            {
                BotShell.Output("[Error] Plugin (" + this.GetLoader(internalName).ToString() + ") is Not Installed!");
                return PluginLoadResult.NotInstalled;
            }

            internalName = internalName.ToLower();
            PluginLoader loader = this.GetLoader(internalName);
            if (loader == null)
            {
                BotShell.Output("[Error] Unable to Find Plugin (" + internalName + ")!");
                return PluginLoadResult.NotFound;
            }
            BotShell.Output("[Plugins] Loading Plugin (" + loader.ToString() + ")");

            foreach (Command command in loader.Commands)
            {
                if (this.Parent.Commands.Exists(command.CommandName))
                {
                    BotShell.Output("[Error] Command (" + command + ") Already in Use!");
                    BotShell.Output("[Error] Plugin (" + loader.ToString() + ") Conflicts With Plugin (" + this.GetLoader(this.Parent.Commands.GetInternalName(command.CommandName)).ToString() + ")");
                    return PluginLoadResult.CommandConflict;
                }
            }
            foreach (string dependency in loader.Dependencies)
            {
                if (!this.Exists(dependency))
                {
                    BotShell.Output("[Error] Required Dependency not Found! No Plugin Known With the Internal Name: " + dependency);
                    return PluginLoadResult.DepencencyNotFound;
                }
                if (!this.IsLoaded(dependency))
                {
                    BotShell.Output("[Error] Required Dependency (" + this.GetLoader(dependency).ToString() + ") not Loaded!");
                    return PluginLoadResult.DepencencyNotLoaded;
                }
            }
            PluginBase plugin = loader.CreatePlugin();
            if (plugin.InternalName.ToLower() != internalName)
                return PluginLoadResult.NotFound;

            this.PluginsList.Add(internalName, plugin);
            try
            {
                plugin.OnLoad(this.Parent);
                foreach (Command command in plugin.Commands)
                {
                    if (!command.IsAlias)
                        this.Parent.Commands.Register(internalName, command);
                    else
                        this.Parent.Commands.RegisterAlias(command.Alias, command.CommandName);
                }
                if (this.GetDefaultState(internalName) != PluginState.Core)
                {
                    if (this.GetConfiguredState(internalName) == PluginState.Installed)
                        this.Config.ExecuteNonQuery(String.Format("DELETE FROM CORE_Plugins WHERE InternalName = '{0}'", internalName));
                    this.Config.ExecuteNonQuery(String.Format("REPLACE INTO CORE_Plugins VALUES('{0}', '{1}', '{2}')", internalName, PluginState.Loaded, plugin.Version));
                    //this.Config.ExecuteNonQuery(String.Format("UPDATE CORE_Plugins SET State = '{1}', Version = '{2}' WHERE InternalName = '{0}'", internalName, PluginState.Loaded, plugin.Version));
                }
            }
            catch (Exception ex)
            {
                BotShell.Output("[Error] Unable to Load Plugin (" + loader.ToString() + "). Exception: " + ex.ToString());
                this.Unload(internalName);
                return PluginLoadResult.OnLoadError;
            }
            return PluginLoadResult.Ok;
        }

        public bool Unload(string internalName) { return this.Unload(internalName, false); }
        public bool Unload(string internalName, bool noconfig)
        {
            if (!this.IsLoaded(internalName))
                return false;

            internalName = internalName.ToLower();
            lock (this.PluginsList)
            {
                foreach (PluginBase plugin in this.PluginsList.Values)
                {
                    foreach (string dependency in plugin.Dependencies)
                    {
                        if (dependency.ToLower() == internalName)
                        {
                            BotShell.Output("[Error] Unable to Unload Plugin (" + this.GetPlugin(internalName).ToString() + ")");
                            BotShell.Output("[Error] Loaded Plugin (" + plugin.ToString() + ") depends on this plugin!");
                            return false;
                        }
                    }
                }
            }
            this.Parent.Commands.UnregisterAll(internalName);
            this.Parent.Configuration.UnregisterAll(internalName);
            int version;
            lock (this.PluginsList)
            {
                version = this.PluginsList[internalName].Version;
                this.PluginsList[internalName].OnUnload(this.Parent);
                BotShell.Output("[Plugins] Unloaded Plugin (" + this.PluginsList[internalName].ToString() + ")");
                this.PluginsList.Remove(internalName);
            }
            if (!noconfig)
            {
                this.Config.ExecuteNonQuery(String.Format("REPLACE INTO CORE_Plugins VALUES('{0}', '{1}', '{2}')", internalName, PluginState.Installed, version));
            }
            return true;
        }

        public bool Install(string internalName)
        {
            if (this.GetConfiguredState(internalName) != PluginState.Disabled)
                return false;

            internalName = internalName.ToLower();
            PluginBase plugin = this.GetLoader(internalName).CreatePlugin();
            int version = plugin.Version;
            try
            {
                plugin.OnInstall(this.Parent);
                BotShell.Output("[Plugins] Installed Plugin (" + plugin.ToString() + ")");
            }
            catch { }
            this.Config.ExecuteNonQuery(String.Format("INSERT INTO CORE_Plugins VALUES('{0}', '{1}', '{2}')", internalName, PluginState.Installed, version));
            this.Config.ExecuteNonQuery(String.Format("UPDATE CORE_Plugins SET State = '{1}' WHERE InternalName = '{0}'", internalName, PluginState.Installed));
            return true;
        }

        public bool Uninstall(string internalName)
        {
            if (this.GetConfiguredState(internalName) != PluginState.Installed)
                return false;

            internalName = internalName.ToLower();
            PluginBase plugin = this.GetLoader(internalName).CreatePlugin();
            int version = plugin.Version;
            try
            {
                plugin.OnUninstall(this.Parent);
                BotShell.Output("[Plugins] Uninstalled Plugin (" + plugin.ToString() + ")");
            }
            catch { }
            this.Config.ExecuteNonQuery(String.Format("INSERT INTO CORE_Plugins VALUES('{0}', '{1}', '{2}')", internalName, PluginState.Disabled, version));
            this.Config.ExecuteNonQuery(String.Format("UPDATE CORE_Plugins SET State = '{1}' WHERE InternalName = '{0}'", internalName, PluginState.Disabled));
            return true;
        }
    }
}
