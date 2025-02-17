﻿using Serenity.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Serenity.CodeGenerator
{
    public class GeneratorConfig
    {
        public List<Connection> Connections { get; set; }
        public string KDiff3Path { get; set; }
        public string WebProjectFile { get; set; }
        public string ScriptProjectFile { get; set; }
        public string RootNamespace { get; set; }
        public List<BaseRowClass> BaseRowClasses { get; set; }
        public List<string> RemoveForeignFields { get; set; }
        public bool GenerateSSImports { get; set; }
        public bool GenerateTSTypings { get; set; }
        public bool GenerateTSCode { get; set; }

        public GeneratorConfig()
        {
            Connections = new List<Connection>();
            KDiff3Path = Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ProgramFilesX86), @"KDiff3\kdiff3.exe");
            BaseRowClasses = new List<BaseRowClass>();
            GenerateTSTypings = true;
            GenerateSSImports = true;
            GenerateTSCode = false;
            SetDefaults();
        }

        public static string GetConfigurationFilePath()
        {
            var configPath = AppDomain.CurrentDomain.BaseDirectory;

            if (IsNugetPackage())
                configPath = Path.GetFullPath(Path.Combine(configPath, @"..\..\..\"));

            return Path.Combine(configPath, "Serenity.CodeGenerator.config");
        }

        private void SetDefaults()
        {
            RootNamespace = "MyProject";

            if (IsNugetPackage())
            {
                var configPath = Path.GetDirectoryName(GetConfigurationFilePath());
                var solutions = Directory.GetFiles(configPath, "*.sln");
                if (solutions.Length == 1)
                {
                    RootNamespace = Path.GetFileNameWithoutExtension(solutions[0]);
                    var subPath = Path.Combine(configPath, RootNamespace);

                    var webProjectFile = Path.Combine(subPath, RootNamespace + ".Web");
                    webProjectFile = Path.Combine(webProjectFile, RootNamespace + ".Web.csproj");
                    if (File.Exists(webProjectFile))
                        WebProjectFile = GetRelativePath(webProjectFile, AppDomain.CurrentDomain.BaseDirectory);

                    var scriptProjectFile = Path.Combine(subPath, RootNamespace + ".Script");
                    scriptProjectFile = Path.Combine(scriptProjectFile, RootNamespace + ".Script.csproj");
                    if (File.Exists(scriptProjectFile))
                        ScriptProjectFile = GetRelativePath(scriptProjectFile, AppDomain.CurrentDomain.BaseDirectory);
                }
            }
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool IsNugetPackage()
        {
            return
                AppDomain.CurrentDomain.BaseDirectory.EndsWith(@"\tools\",
                    StringComparison.OrdinalIgnoreCase) &&
                AppDomain.CurrentDomain.BaseDirectory.IndexOf(@"\packages\Serenity.CodeGenerator.",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public void Save()
        {
            Connections.Sort((x, y) => x.Key.CompareTo(y.Key));
            File.WriteAllText(GeneratorConfig.GetConfigurationFilePath(), JSON.StringifyIndented(this));
        }

        public static GeneratorConfig Load()
        {
            var configFilePath = GetConfigurationFilePath();
            var config = JsonConfigHelper.LoadConfig<GeneratorConfig>(configFilePath);
            config.Connections = config.Connections ?? new List<GeneratorConfig.Connection>();
            config.RemoveForeignFields = config.RemoveForeignFields ?? new List<string>();
            return config;
        }

        public void UpdateConnectionsFrom(string configFilePath,
            Action<Connection> added)
        {
            if (!string.IsNullOrEmpty(configFilePath) &&
                File.Exists(configFilePath))
            {
                try
                {
                    var xml = new XmlDocument();
                    xml.LoadXml(File.ReadAllText(configFilePath));
                    var nodes = xml.SelectNodes("//configuration/connectionStrings/add");
                    foreach (XmlElement node in nodes)
                    {
                        var name = node.Attributes["name"];
                        var conn = node.Attributes["connectionString"];
                        var prov = node.Attributes["providerName"];
                        if (name != null &&
                            !string.IsNullOrWhiteSpace(name.Value) &&
                            conn != null &&
                            !string.IsNullOrWhiteSpace(conn.Value) &&
                            prov != null &&
                            !string.IsNullOrWhiteSpace(prov.Value))
                        {
                            var connection = Connections.FirstOrDefault(x => String.Compare(x.Key, name.Value, StringComparison.OrdinalIgnoreCase) == 0);
                            if (connection == null)
                            {
                                connection = new GeneratorConfig.Connection();
                                connection.Key = name.Value;
                                Connections.Add(connection);
                                connection.ConnectionString = conn.Value;
                                connection.ProviderName = prov.Value;
                                if (added != null)
                                    added(connection);
                            }
                            else
                            {
                                connection.ConnectionString = conn.Value;
                                connection.ProviderName = prov.Value;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }
        }

        public class Connection
        {
            public string Key { get; set; }
            public string ConnectionString { get; set; }
            public string ProviderName { get; set; }
            public List<Table> Tables { get; set; }

            public Connection()
            {
                Tables = new List<Table>();
            }

            public override string ToString()
            {
                return Key + " [" + ConnectionString + "], " +  ProviderName;
            }
        }

        public class Table
        {
            public string Tablename { get; set; }
            public string Identifier { get; set; }
            public string Module { get; set; }
            public string ConnectionKey { get; set; }
            public string PermissionKey { get; set; }
        }

        public class BaseRowClass
        {
            public string ClassName { get; set; }
            public List<string> Fields { get; set; }
        }
    }
}