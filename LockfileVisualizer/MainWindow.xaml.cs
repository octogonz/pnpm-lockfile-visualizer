using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using System.Windows;
using Path = System.IO.Path;

namespace LockfileVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Utils.RegisterAvalonSchema("YAML", ".yml", ".yaml");

            IHighlightingDefinition highlighter = HighlightingManager.Instance.GetDefinitionByExtension(".yaml");

            ctlYamlEditor.SyntaxHighlighting = highlighter;
        }

        private void btnReload_Click(object sender, RoutedEventArgs e)
        {
            string yamlFilePath = Path.Join(Utils.DataFolder, "pnpm-lock.yaml");
            string yamlContent = File.ReadAllText(yamlFilePath);
            ctlYamlEditor.Text = yamlContent;
        }
    }
}