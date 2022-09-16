using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LockfileVisualizer
{
    public class LockfileNode<T> where T : YamlNode
    {
        public T YamlNode;
        public int LineNumber;

        public LockfileNode(T yamlNode)
        {
            this.YamlNode = yamlNode;
            this.LineNumber = yamlNode.Start.Line;
        }
    }

    public class LockfileDependency : LockfileNode<YamlNode>
    {
        public string Name;
        public string VersionSpec;
        public bool DevDependency;

        public LockfileDependency(string name, string versionSpec, bool devDependency, YamlNode yamlNode) : base(yamlNode)
        {
            this.Name = name;
            this.VersionSpec = versionSpec;
            this.DevDependency = devDependency;
        }

        public static void ParseDependencies(List<LockfileDependency> dependencies, YamlMappingNode yamlNode)
        {
            YamlMappingNode? dependenciesNode = Utils.GetYamlChild<YamlMappingNode>(yamlNode, "dependencies");
            if (dependenciesNode != null)
            {
                foreach (var keyValuePair in dependenciesNode)
                {
                    string packageName = keyValuePair.Key.ToString();
                    string versionSpec = keyValuePair.Value.ToString();

                    dependencies.Add(new LockfileDependency(packageName, versionSpec, false, keyValuePair.Key));
                }
            }

            YamlMappingNode? devDependenciesNode = Utils.GetYamlChild<YamlMappingNode>(yamlNode, "devDependencies");
            if (devDependenciesNode != null)
            {
                foreach (var keyValuePair in devDependenciesNode)
                {
                    string packageName = keyValuePair.Key.ToString();
                    string versionSpec = keyValuePair.Value.ToString();

                    dependencies.Add(new LockfileDependency(packageName, versionSpec, true, keyValuePair.Key));
                }
            }
        }
    }

    public class LockfileImporter : LockfileNode<YamlMappingNode>
    {
        public string WorkspacePath;
        public readonly List<LockfileDependency> Dependencies = new List<LockfileDependency>();

        public LockfileImporter(string workspacePath, YamlMappingNode yamlNode) : base(yamlNode)
        {
            this.WorkspacePath = workspacePath;

            LockfileDependency.ParseDependencies(this.Dependencies, yamlNode);
        }
    }

    public class LockfilePackage : LockfileNode<YamlMappingNode>
    {
        public string PackagePath;
        public readonly List<LockfileDependency> Dependencies = new List<LockfileDependency>();

        public LockfilePackage(string packagePath, YamlMappingNode yamlNode) : base(yamlNode)
        {
            this.PackagePath = packagePath;

            LockfileDependency.ParseDependencies(this.Dependencies, yamlNode);
        }
    }

    public class Lockfile
    {
        public readonly string YamlContent;

        public readonly List<LockfileImporter> Importers = new List<LockfileImporter>();
        public readonly List<LockfilePackage> Packages = new List<LockfilePackage>();

        public Lockfile(string yamlContent)
        {
            this.YamlContent = yamlContent;
            var input = new StringReader(yamlContent);

            var yaml = new YamlStream();
            yaml.Load(input);

            YamlMappingNode? root = (YamlMappingNode)yaml.Documents[0].RootNode;
            if (root == null)
            {
                throw new Exception("Missing YAML root");
            }

            YamlMappingNode? importers = Utils.GetYamlChild<YamlMappingNode>(root, "importers");
            if (importers != null)
            {
                foreach (var keyValue in importers.Children)
                {
                    YamlMappingNode? mapping = keyValue.Value as YamlMappingNode;
                    if (mapping == null)
                    {
                        throw new Exception("Expecting mapping");
                    }
                    var importer = new LockfileImporter(keyValue.Key.ToString(), mapping);
                    this.Importers.Add(importer);
                }
            }

            YamlMappingNode? packages = Utils.GetYamlChild<YamlMappingNode>(root, "packages");
            if (packages != null)
            {
                foreach (var keyValue in packages.Children)
                {
                    YamlMappingNode? mapping = keyValue.Value as YamlMappingNode;
                    if (mapping == null)
                    {
                        throw new Exception("Expecting mapping");
                    }
                    var package = new LockfilePackage(keyValue.Key.ToString(), mapping);
                    this.Packages.Add(package);
                }
            }
        }
    }
}