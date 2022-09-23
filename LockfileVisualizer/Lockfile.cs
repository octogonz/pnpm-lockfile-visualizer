using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LockfileVisualizer
{
    public class LockfileNode
    {
        public YamlNode YamlNode;
        public int LineNumber;

        public LockfileNode(YamlNode yamlNode)
        {
            this.YamlNode = yamlNode;
            this.LineNumber = yamlNode.Start.Line;
        }
    }

    [DebuggerDisplay("{EntryId}")]
    public class LockfileDependency : LockfileNode
    {
        public readonly LockfileEntry ContainingEntry;
        public readonly string Name;
        public readonly string VersionSpec;
        public readonly bool DevDependency;
        public readonly string EntryId;

        public LockfileEntry? ResolvedEntry;

        public LockfileDependency(string name, string versionSpec, bool devDependency, LockfileEntry containingEntry, YamlNode yamlNode) : base(yamlNode)
        {
            this.Name = name;
            this.VersionSpec = versionSpec;
            this.DevDependency = devDependency;
            this.ContainingEntry = containingEntry;

            if (this.VersionSpec.StartsWith("link:"))
            {
                string relativePath = this.VersionSpec.Substring("link:".Length);
                string rootRelativePath = Path.GetRelativePath(".", Path.Combine(containingEntry.PackageJsonFolderPath, relativePath));
                this.EntryId = "project:./" + rootRelativePath.Replace("\\", "/");
            }
            else if (this.VersionSpec.StartsWith("/"))
            {
                this.EntryId = this.VersionSpec;
            }
            else
            {
                this.EntryId = "/" + this.Name + "/" + this.VersionSpec;
            }
        }

        public static void ParseDependencies(List<LockfileDependency> dependencies, LockfileEntry containingEntry, YamlMappingNode yamlNode)
        {
            YamlMappingNode? dependenciesNode = Utils.GetYamlChild<YamlMappingNode>(yamlNode, "dependencies");
            if (dependenciesNode != null)
            {
                foreach (var keyValuePair in dependenciesNode)
                {
                    string packageName = keyValuePair.Key.ToString();
                    string versionSpec = keyValuePair.Value.ToString();

                    dependencies.Add(new LockfileDependency(packageName, versionSpec, false, containingEntry, keyValuePair.Key));
                }
            }

            YamlMappingNode? devDependenciesNode = Utils.GetYamlChild<YamlMappingNode>(yamlNode, "devDependencies");
            if (devDependenciesNode != null)
            {
                foreach (var keyValuePair in devDependenciesNode)
                {
                    string packageName = keyValuePair.Key.ToString();
                    string versionSpec = keyValuePair.Value.ToString();

                    dependencies.Add(new LockfileDependency(packageName, versionSpec, true, containingEntry, keyValuePair.Key));
                }
            }
        }
    }

    public enum LockfileEntryKind
    {
        Project,
        Package
    }

    [DebuggerDisplay("{EntryId}")]
    public class LockfileEntry : LockfileNode
    {
        // Example:
        //   ../../plugins/remark-canonical-link-plugin
        private static Regex removeDotsRegex = new Regex(@"^.*/([^/]+)$");

        // Example:
        //   /@rushstack/eslint-config/3.0.1_eslint@8.21.0+typescript@4.7.4
        private static Regex packageEntryIdRegex = new Regex(@"^/(.*)/([^/]+)$");

        public readonly LockfileEntryKind Kind;

        public readonly string EntryId;
        public readonly string RawEntryId;
        public readonly string PackageJsonFolderPath;

        public readonly List<LockfileDependency> Dependencies = new List<LockfileDependency>();

        public readonly List<LockfileDependency> Referencers = new List<LockfileDependency>();

        public readonly string DisplayText;

        public readonly string EntryPackageName;
        public readonly string EntryPackageVersion;
        public readonly string EntrySuffix;

        public LockfileEntry(string rawEntryId, LockfileEntryKind kind, string rootPackageJsonPath, YamlMappingNode yamlNode) : base(yamlNode)
        {
            this.EntryId = rawEntryId;
            this.RawEntryId = rawEntryId;
            this.PackageJsonFolderPath = "";

            this.Kind = kind;

            this.EntryPackageName = "";
            this.EntryPackageVersion = "";
            this.EntrySuffix = "";

            if (kind == LockfileEntryKind.Project)
            {
                // If    rootPackageJsonPath    = "common/temp/package.json"
                // and   rawEntryId             = "../../libraries/example"
                // then  entryId                = "project:./libraries/example"
                string rootPackageJsonFolderPath = Path.GetDirectoryName(rootPackageJsonPath);
                this.PackageJsonFolderPath = Path.GetRelativePath(".", Path.Combine(rootPackageJsonFolderPath, rawEntryId)).Replace("\\", "/");
                this.EntryId = "project:./" + this.PackageJsonFolderPath;
                this.EntryPackageName = Path.GetFileName(this.RawEntryId);
                this.DisplayText = "Project: " + this.EntryPackageName;
            }
            else
            {
                this.DisplayText = rawEntryId;

                var match = LockfileEntry.packageEntryIdRegex.Match(rawEntryId);
                if (match.Success)
                {
                    string packageName = match.Groups[1].Value;
                    this.EntryPackageName = packageName;

                    string versionPart = match.Groups[2].Value;

                    int underscoreIndex = versionPart.IndexOf('_');
                    if (underscoreIndex >= 0)
                    {
                        string version = versionPart.Substring(0, underscoreIndex);
                        string suffix = versionPart.Substring(underscoreIndex + 1);

                        this.EntryPackageVersion = version;
                        this.EntrySuffix = suffix;

                        //       /@rushstack/eslint-config/3.0.1_eslint@8.21.0+typescript@4.7.4
                        // -->   @rushstack/eslint-config 3.0.1 (eslint@8.21.0+typescript@4.7.4)
                        this.DisplayText = packageName + " " + version + " (" + suffix + ")";
                    }
                    else
                    {
                        this.EntryPackageVersion = versionPart;

                        //       /@rushstack/eslint-config/3.0.1
                        // -->   @rushstack/eslint-config 3.0.1
                        this.DisplayText = packageName + " " + versionPart;
                    }
                }

                // Example:
                //   common/temp/node_modules/.pnpm
                //     /@babel+register@7.17.7_@babel+core@7.17.12
                //     /node_modules/@babel/register
                this.PackageJsonFolderPath = "common/temp/node_modules/.pnpm/"
                    + this.EntryPackageName.Replace("/", "+") + "@" + this.EntryPackageVersion
                    + "/node_modules/" + this.EntryPackageName;
            }

            LockfileDependency.ParseDependencies(this.Dependencies, this, yamlNode);
        }
    }

    public class Lockfile
    {
        public readonly string YamlContent;
        public readonly string RootPackageJsonPath;

        public readonly List<LockfileEntry> Importers = new List<LockfileEntry>();
        public readonly List<LockfileEntry> Packages = new List<LockfileEntry>();

        public readonly List<LockfileEntry> AllEntries = new List<LockfileEntry>();
        public readonly Dictionary<string, LockfileEntry> AllEntriesById = new Dictionary<string, LockfileEntry>();

        public Lockfile(string yamlContent, string rootPackageJsonPath)
        {
            this.YamlContent = yamlContent;
            this.RootPackageJsonPath = rootPackageJsonPath;
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
                    var importer = new LockfileEntry(keyValue.Key.ToString(), LockfileEntryKind.Project, rootPackageJsonPath, mapping);
                    this.Importers.Add(importer);
                    this.AllEntries.Add(importer);
                    this.AllEntriesById.Add(importer.EntryId, importer);
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
                    var package = new LockfileEntry(keyValue.Key.ToString(), LockfileEntryKind.Package, rootPackageJsonPath, mapping);
                    this.Packages.Add(package);
                    this.AllEntries.Add(package);
                    this.AllEntriesById.Add(package.EntryId, package);
                }
            }

            foreach (var entry in this.AllEntries)
            {
                foreach (var dependency in entry.Dependencies)
                {
                    LockfileEntry match;
                    if (this.AllEntriesById.TryGetValue(dependency.EntryId, out match))
                    {
                        dependency.ResolvedEntry = match;
                        match.Referencers.Add(dependency);
                    }
                    else
                    {
                        throw new Exception("Unable to resolve dependency entryId=" + dependency.EntryId);
                    }
                }
            }
        }
    }
}