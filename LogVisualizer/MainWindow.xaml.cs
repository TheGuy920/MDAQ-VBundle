using LogVisualizer.Models;
using Microsoft.Win32;
using MotecLogSerializer.LdParser;
using System.Windows;

namespace LogVisualizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    List<LdChan> Channels = [];
    string searchFilter = "";
    bool searchOpen = false;

    readonly List<PlotGraph> Graphs = [];
    PlotGraph? ActiveGraph;

    public MainWindow()
    {
        this.InitializeComponent();
    }

    private void UncheckAllButtonClick(object sender, RoutedEventArgs e)
    {
        this.ActiveGraph?.UncheckAll();
    }

    private void OpenFile(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new()
        {
            CheckFileExists = true,
            CheckPathExists = true,
            Filter = "Ld files (*.ld)|*.ld",
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        this.GraphViewer.Children.Clear();
        this.ListView.ItemsSource = null;
        this.Channels.Clear();

        var ldata = LdData.FromFile(openFileDialog.FileName);
        this.Channels = ldata.Channels;

        this.Title = openFileDialog.FileName;
    }

    private void TextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (!this.searchOpen)
        {
            this.searchOpen = true;
            this.searchFilter = "";
            this.SearchBar.Text = "";
            this.SearchBar.Foreground = System.Windows.Media.Brushes.Black;
        }
    }

    private void TextBox_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (this.searchOpen && this.SearchBar.Text.Length <= 0)
            this.ClearSearch(sender, null);
    }

    private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs? e)
    {
        if (this.searchOpen && this.ListView is not null)
        {
            this.searchFilter = this.SearchBar.Text;
            foreach (LineGraph graph in this.ListView.Items.OfType<LineGraph>())
                graph.Visibility = graph.Key.Contains(this.searchFilter, StringComparison.InvariantCultureIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void ClearSearch(object sender, RoutedEventArgs? e)
    {
        this.searchOpen = false;
        this.searchFilter = "";
        this.SearchBar.Text = "Search";
        this.SearchBar.Foreground = System.Windows.Media.Brushes.Gray;

        foreach (LineGraph graph in this.ListView.Items.OfType<LineGraph>())
            graph.Visibility = Visibility.Visible;
    }

    private void AddEmptyGraph(object sender, RoutedEventArgs e)
    {
        PlotGraph graph = new(this.Channels);
        graph.OnSelected += this.GraphOnSelected;
        graph.OnRemove += g =>
        {
            this.Graphs.Remove(g);
            this.GraphViewer.Children.Remove(g);

            if (this.ActiveGraph == g)
            {
                this.ActiveGraph = null;
                this.ListView.ItemsSource = null;
            }
        };

        this.Graphs.Add(graph);
        this.GraphViewer.Children.Add(graph);
    }

    private void GraphOnSelected(PlotGraph obj)
    {
        foreach (PlotGraph graph in this.Graphs)
        {
            if (graph == obj)
                continue;
            graph.Unselected();
        }

        this.ActiveGraph = obj;
        this.ListView.ItemsSource = obj.OrderedChannels;
    }
}