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
        private LockfileEntry? _selectedEntry = null;
        private bool _selectedEntryChanged = false;

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

            ctlEntryDepsListView.Items.Clear();
            ctlEntryRefsListView.Items.Clear();

            this._updateTimer.Interval = TimeSpan.FromMilliseconds(500);
            this._updateTimer.Tick += this._updateTimer_Tick;
            this._updateTimer.Start();
        }

        private void btnReload_Click(object sender, RoutedEventArgs? e)
        {
            string yamlFilePath = Path.Join(Utils.DataFolder, "pnpm-lock.yaml");

            string yamlContent = File.ReadAllText(yamlFilePath);
            this._Lockfile = new Lockfile(yamlContent, "common/temp/package.json");

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
                item.Content = x.DisplayText;
                ctlPackagesListView.Items.Add(item);
            }

            this.ctlImportersListView.Items.Clear();
            foreach (var x in this._Lockfile.Importers)
            {
                var item = new ListViewItem();
                item.Tag = x;
                item.Content = x.DisplayText;
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

            if (this._selectedEntry != null)
            {
                if (this._selectedEntryChanged)
                {
                    ctlYamlEditor.ScrollToLine(this._selectedEntry.LineNumber - 1);
                    ctlYamlEditor.TextArea.Caret.Line = this._selectedEntry.LineNumber - 1;
                    ctlYamlEditor.TextArea.Caret.BringCaretToView();
                }

                this.txtEntryName.Content = this._selectedEntry.DisplayText;

                ctlEntryDepsListView.Items.Clear();
                foreach (var dependency in this._selectedEntry.Dependencies)
                {
                    var item = new ListViewItem();
                    item.Tag = dependency;
                    item.Content = dependency.ResolvedEntry.DisplayText;

                    ctlEntryDepsListView.Items.Add(item);
                }

                ctlEntryRefsListView.Items.Clear();
                foreach (var referencer in this._selectedEntry.Referencers)
                {
                    var item = new ListViewItem();
                    item.Tag = referencer;
                    item.Content = referencer.ContainingEntry.DisplayText;

                    ctlEntryRefsListView.Items.Add(item);
                }

                this.txtFolderPath.Content = this._selectedEntry.PackageJsonFolderPath;
            }
        }

        private bool ctlImportersListView_ItemsFilter(object itemObject)
        {
            var item = itemObject as ListViewItem;
            if (item != null)
            {
                string text = txtImportersSearch.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    LockfileEntry? entry = item.Tag as LockfileEntry;
                    if (entry != null)
                    {
                        if (entry.EntryId.IndexOf(text, System.StringComparison.InvariantCultureIgnoreCase) >= 0)
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
                    LockfileEntry? entry = item.Tag as LockfileEntry;
                    if (entry != null)
                    {
                        if (entry.EntryId.IndexOf(text, System.StringComparison.InvariantCultureIgnoreCase) >= 0)
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
                LockfileEntry? entry = item.Tag as LockfileEntry;
                if (entry != null)
                {
                    this._selectItem(entry);
                }
            }
        }

        private void ctlImportersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ctlImportersListView.SelectedItem as ListViewItem;
            if (item != null)
            {
                LockfileEntry? entry = item.Tag as LockfileEntry;
                if (entry != null)
                {
                    this._selectItem(entry);
                }
            }
        }

        private void ctlEntryRefsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ctlEntryRefsListView.SelectedItem as ListViewItem;
            if (item != null)
            {
                LockfileDependency? dependency = item.Tag as LockfileDependency;
                if (dependency != null)
                {
                    this._selectItem(dependency.ContainingEntry);
                }
            }
        }

        private void ctlEntryDepsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ctlEntryDepsListView.SelectedItem as ListViewItem;
            if (item != null)
            {
                LockfileDependency? dependency = item.Tag as LockfileDependency;
                if (dependency != null)
                {
                    this._selectItem(dependency.ResolvedEntry);
                }
            }
        }

        private void _selectItem(LockfileEntry? entry)
        {
            this._selectedEntry = entry;
            this._selectedEntryChanged = true;
            this._requestUpdateUI();
        }
    }
}