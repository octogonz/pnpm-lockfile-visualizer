using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Path = System.IO.Path;

namespace LockfileVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Lockfile? _Lockfile = null;

        public MainWindow()
        {
            InitializeComponent();

            Utils.RegisterAvalonSchema("YAML", ".yml", ".yaml");

            IHighlightingDefinition highlighter = HighlightingManager.Instance.GetDefinitionByExtension(".yaml");

            ctlYamlEditor.SyntaxHighlighting = highlighter;
            ctlYamlEditor.Options.HighlightCurrentLine = true;

            btnReload_Click(this, null);
        }

        private void btnReload_Click(object sender, RoutedEventArgs? e)
        {
            string yamlFilePath = Path.Join(Utils.DataFolder, "pnpm-lock.yaml");

            string yamlContent = File.ReadAllText(yamlFilePath);
            this._Lockfile = new Lockfile(yamlContent);

            ctlYamlEditor.Text = this._Lockfile.YamlContent;

            this.RefreshUI();
        }

        private void RefreshUI()
        {
            if (this._Lockfile == null)
            {
                return;
            }

            this.ctlPackagesListView.Items.Clear();
            foreach (var x in this._Lockfile.Packages)
            {
                var item = new ListViewItem();
                item.Tag = x;
                item.Content = x.PackagePath;
                ctlPackagesListView.Items.Add(item);
            }

            this.ctlImportersListView.Items.Clear();
            foreach (var x in this._Lockfile.Importers)
            {
                var item = new ListViewItem();
                item.Tag = x;
                item.Content = x.WorkspacePath;
                ctlImportersListView.Items.Add(item);
            }
        }
    }
}