using LogVisualizer.Models;
using Microsoft.Win32;
using MotecLogSerializer.LdParser;
using System.Drawing;
using System.Windows;

namespace LogVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, LineGraph> Channels = [];
        string searchFilter = "";
        bool searchOpen = false;

        public MainWindow()
        {
            this.InitializeComponent();

            this.Graph.Plot.SetStyle(new ScottPlot.PlotStyle()
            {
                GridMajorLineColor = ScottPlot.Color.FromColor(Color.FromArgb(50, 255, 255, 255)),
                AxisColor = ScottPlot.Color.FromColor(Color.White),
                FigureBackgroundColor = ScottPlot.Color.FromColor(Color.Transparent),
            });
        }

        private void UncheckAllButtonClick(object sender, RoutedEventArgs e)
        {
            foreach (var (_, graph) in this.Channels)
                graph.Uncheck();
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
                // DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            };

            if (openFileDialog.ShowDialog() != true)
                return;

            this.Channels.Clear();
            this.Graph.Plot.Clear();

            var ldata = LdData.FromFile(openFileDialog.FileName);
            this.Channels = ldata.Channels.Where(c => c.Frequency > 0).Select(c => new LineGraph(c)).ToDictionary(lg => lg.Key);

            this.ListView.Items.Clear();

            foreach (var (key, graph) in this.Channels.OrderBy(_ => _.Key))
            {
                graph.GraphUpdated += this.Graph.Refresh;
                this.Graph.Plot.Add.Plottable(graph.ScatterLine);
                this.ListView.Items.Add(graph);
            }

            this.Graph.Plot.XLabel("Time (s)");
            foreach (var axes in Graph.Plot.Axes.GetAxes())
                axes.Min = 0;
            this.Graph.Refresh();
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
                foreach (var item in this.ListView.Items)
                    if (item is LineGraph graph)
                        graph.Visibility = graph.Key.Contains(this.searchFilter, StringComparison.InvariantCultureIgnoreCase)
                            ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ClearSearch(object sender, RoutedEventArgs? e)
        {
            this.searchOpen = false;
            this.searchFilter = "";
            this.SearchBar.Text = "Search";
            this.SearchBar.Foreground = System.Windows.Media.Brushes.Gray;

            foreach (var item in this.ListView.Items)
                if (item is LineGraph graph)
                    graph.Visibility = Visibility.Visible;
        }
    }
}