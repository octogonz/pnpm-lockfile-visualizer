using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace LockfileVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Lockfile? _Lockfile = null;
        private readonly DispatcherTimer _updateTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            Utils.RegisterAvalonSchema("YAML", ".yml", ".yaml");

            IHighlightingDefinition highlighter = HighlightingManager.Instance.GetDefinitionByExtension(".yaml");

            ctlYamlEditor.SyntaxHighlighting = highlighter;
            ctlYamlEditor.Options.HighlightCurrentLine = true;

            btnReload_Click(this, null);

            txtImportersSearch.Text = "";
            txtPackagesSearch.Text = "";

            this._updateTimer.Interval = TimeSpan.FromMilliseconds(500);
            this._updateTimer.Tick += _updateTimer_Tick;
            this._updateTimer.Start();
        }

        private void btnReload_Click(object sender, RoutedEventArgs? e)
        {
            string yamlFilePath = Path.Join(Utils.DataFolder, "pnpm-lock.yaml");

            string yamlContent = File.ReadAllText(yamlFilePath);
            this._Lockfile = new Lockfile(yamlContent);

            ctlYamlEditor.Text = this._Lockfile.YamlContent;

            this._refreshFile();
        }

        private void _refreshFile()
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

        private void _requestUpdateUI()
        {
            this._updateTimer.Start();
        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            this._updateTimer.Stop();
            this._updateUI();
        }

        private void _updateUI()
        {
            ctlImportersListView.Items.Filter = null;
            ctlPackagesListView.Items.Filter = null;

            ctlImportersListView.Items.Filter = ctlImportersListView_ItemsFilter;
            ctlPackagesListView.Items.Filter = ctlPackagesListView_ItemsFilter;
        }

        private bool ctlImportersListView_ItemsFilter(object itemObject)
        {
            var item = itemObject as ListViewItem;
            if (item != null)
            {
                string text = txtImportersSearch.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    LockfileImporter? importer = item.Tag as LockfileImporter;
                    if (importer != null)
                    {
                        if (importer.WorkspacePath.IndexOf(text, System.StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool ctlPackagesListView_ItemsFilter(object itemObject)
        {
            var item = itemObject as ListViewItem;
            if (item != null)
            {
                string text = txtPackagesSearch.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    LockfilePackage? package = item.Tag as LockfilePackage;
                    if (package != null)
                    {
                        if (package.PackagePath.IndexOf(text, System.StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private void txtPackagesSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            this._requestUpdateUI();
        }

        private void txtImportersSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            this._requestUpdateUI();
        }

        private void ctlPackagesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ctlPackagesListView.SelectedItem as ListViewItem;
            if (item != null)
            {
                LockfilePackage? package = item.Tag as LockfilePackage;
                if (package != null)
                {
                    ctlYamlEditor.ScrollToLine(package.LineNumber - 1);
                    ctlYamlEditor.TextArea.Caret.Line = package.LineNumber - 1;
                    ctlYamlEditor.TextArea.Caret.BringCaretToView();
                }
            }
        }

        private void ctlImportersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ctlImportersListView.SelectedItem as ListViewItem;
            if (item != null)
            {
                LockfileImporter? importer = item.Tag as LockfileImporter;
                if (importer != null)
                {
                    ctlYamlEditor.ScrollToLine(importer.LineNumber - 1);
                    ctlYamlEditor.TextArea.Caret.Line = importer.LineNumber - 1;
                    ctlYamlEditor.TextArea.Caret.BringCaretToView();
                }
            }
        }
    }
}