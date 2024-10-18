using MotecLogSerializer.LdParser;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DColor = System.Drawing.Color;

namespace LogVisualizer.Models;
/// <summary>
/// Interaction logic for PlotGraph.xaml
/// </summary>
public partial class PlotGraph : UserControl
{
    public event Action<PlotGraph>? OnSelected;
    public event Action<PlotGraph>? OnRemove;
    public readonly Dictionary<string, LineGraph> Channels = [];
    public readonly IEnumerable<LineGraph> OrderedChannels;

    private static readonly SolidColorBrush SelectedBrush = new(Color.FromArgb(0xFF, 0x00, 0x80, 0xFF));
    private static readonly SolidColorBrush SelectedBrushBG = new(Color.FromArgb(0x10, 0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush UnselectedBrush = new(Color.FromArgb(0x01, 0xFF, 0xFF, 0xFF));

    public PlotGraph(string FullFile, IEnumerable<LdChan> Channels)
    {
        InitializeComponent();

        this.Graph.Plot.Title(Path.GetFileName(FullFile));
        this.Graph.Plot.SetStyle(new ScottPlot.PlotStyle()
        {
            GridMajorLineColor = ScottPlot.Color.FromColor(DColor.FromArgb(50, 255, 255, 255)),
            AxisColor = ScottPlot.Color.FromColor(DColor.White),
            FigureBackgroundColor = ScottPlot.Color.FromColor(DColor.Transparent),
        });

        IEnumerable<LdChan> active = Channels.Where(c => c.Frequency > 0);
        IEnumerable<string> names = active.Select(c => c.Name);
        string fixName(LineGraph lg) => names.Count(n => n == lg.Key) > 1 ? $"{lg.Key}.{lg.MetaPtr}" : lg.Key;

        this.Channels = active.Select(c => new LineGraph(c, 0.250)).ToDictionary(fixName);
        this.OrderedChannels = this.Channels.Values.OrderBy(g => g.Key);

        foreach ((string key, LineGraph graph) in this.Channels.OrderBy(_ => _.Key))
        {
            graph.GraphUpdated += this.Graph.Refresh;
            this.Graph.Plot.Add.Plottable(graph.ScatterLine);
        }

        this.Graph.Plot.XLabel("Time (s)");
        foreach (ScottPlot.IAxis axes in Graph.Plot.Axes.GetAxes())
            axes.Min = 0;

        this.Graph.Refresh();
    }

    public void UncheckAll()
    {
        foreach (LineGraph graph in this.Channels.Values)
            graph.Uncheck();
    }

    public void Unselected()
    {
        this.BorderSelector.BorderBrush = Brushes.Transparent;
        this.BorderSelector.Background = UnselectedBrush;
    }

    private void Selected(object sender, MouseButtonEventArgs e)
    {
        this.OnSelected?.Invoke(this);
        this.BorderSelector.BorderBrush = SelectedBrush;
        this.BorderSelector.Background = SelectedBrushBG;
        e.Handled = false;
    }

    private void RemoveSelf(object sender, System.Windows.RoutedEventArgs e)
    {
        this.OnRemove?.Invoke(this);
    }
}
