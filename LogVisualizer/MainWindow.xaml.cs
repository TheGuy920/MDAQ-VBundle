using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using MotecLogSerializer.LdParser;

namespace LogVisualizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string PATH = "C:\\Users\\theguy920\\Documents\\Motec\\S1_#5264_20240921_135809.ld";

        readonly Dictionary<string, (LdChan Channel, bool isShown, float[] xData)> Channels;

        public MainWindow()
        {
            InitializeComponent();

            var ldata = LdData.FromFile(PATH);
            this.Channels = ldata.Channels
                .Where(c => c.Frequency > 0)
                .DistinctBy(c => c.Name)
                .Select(c => (c, false, Enumerable.Range(0, c.Data.Length).Select(i => i / (float)c.Frequency).ToArray()))
                .ToDictionary(pack => pack.c.Name);

            this.SelectionBox.ItemsSource = Channels.Keys.Order();
            this.SelectionBox.SelectionChanged += SelectionBox_SelectionChanged;

            this.Graph.Plot.SetStyle(new ScottPlot.PlotStyle()
            {
                GridMajorLineColor = ScottPlot.Color.FromColor(Color.FromArgb(50, 255, 255, 255)),
                AxisColor = ScottPlot.Color.FromColor(Color.White),
                FigureBackgroundColor = ScottPlot.Color.FromColor(Color.Transparent),
            });
        }

        private void SelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                string key = e.AddedItems[0]!.ToString()!;
                var (channel, isShown, xData) = Channels[key];
                if (channel != null && !isShown)
                {
                    this.Graph.Plot.Axes.Bottom.Min = -1;
                    this.Graph.Plot.Axes.Bottom.Max = channel.Data.Length / channel.Frequency;
                    this.Graph.Plot.Axes.Left.Min = -1;
                    this.Graph.Plot.Axes.Left.Max = channel.Data.Max() + 1 ;
                    var line = this.Graph.Plot.Add.ScatterLine(xData, channel.Data);
                    line.LineWidth = 3;
                    line.Color = line.Color.Lighten(0.4);

                    this.Channels[key] = (channel, true, xData);
                }
            }

            this.Graph.Refresh();
        }
    }
}