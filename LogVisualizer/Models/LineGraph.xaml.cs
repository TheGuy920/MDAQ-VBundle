using MotecLogSerializer.LdParser;
using ScottPlot;
using ScottPlot.DataSources;
using ScottPlot.Plottables;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace LogVisualizer.Models
{
    /// <summary>
    /// Interaction logic for LineGraph.xaml
    /// </summary>
    public partial class LineGraph : UserControl
    {
        public string Key { get; init; }
        public Scatter ScatterLine { get; private set; }
        public event Action? GraphUpdated;

        private bool IsChecked => this.VisibilityCB.IsChecked == true;
        
        public LineGraph(LdChan channel)
        {
            InitializeComponent();

            var rand = new Random();
            var colorR = (byte)rand.Next(255);
            var colorG = (byte)rand.Next(255);
            var colorB = (byte)rand.Next(255);

            this.ColorPickerCtrl.SelectedColor = Color.FromRgb(colorR, colorG, colorB);
            this.TitleTB.Text = channel.Name + '.' + channel.MetaPtr.ToString();
            this.Key = this.TitleTB.Text;

            Coordinates[] coords = channel.Data.Select((d, i) => new Coordinates(i / (double)channel.Frequency, d)).ToArray();
            ScatterSourceCoordinatesArray data = new(coords);
            this.ScatterLine = new Scatter(data)
            {
                MarkerStyle = MarkerStyle.None,
                IsVisible = false,
                LineColor = new ScottPlot.Color(colorR, colorG, colorB),
                LineWidth = 3,
                ScaleY = 1.1,
            };

            this.ScatterLine.LineColor = this.ScatterLine.LineColor.Lighten(0.4);
        }

        private void IsCheckedChanged(object? _, System.Windows.RoutedEventArgs? __)
        {
            this.ScatterLine.IsVisible = this.IsChecked;
            this.GraphUpdated?.Invoke();
        }

        public void Uncheck()
        {
            this.VisibilityCB.IsChecked = false;
            this.ResetScale();
        }

        private void OnMouseDown(object _, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.VisibilityCB.IsChecked = !this.IsChecked;
        }

        private void SelectedColorChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue == null || this.ScatterLine == null)
                return;

            this.ScatterLine.LineColor = new ScottPlot.Color(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B);
            this.GraphUpdated?.Invoke();
        }

        private static readonly double divisor = Math.Pow(Math.E, 5) - 1;

        private void ScaleData_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.ScatterLine == null)
                return;

            this.ScatterLine.ScaleY = (50 * (Math.Pow(Math.E, e.NewValue/20) - 1)) / divisor;
            if (Math.Abs(this.ScatterLine.ScaleY - 1) < 0.01)
                this.ResetButton.IsEnabled = false;
            else
                this.ResetButton.IsEnabled = true;

            this.GraphUpdated?.Invoke();
        }

        private void ResetScale()
        {
            this.ScaleData.Value = 27.46551563;
            this.ScatterLine.ScaleY = 1;
            this.GraphUpdated?.Invoke();
        }

        private void ResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.ResetScale();
        }
    }
}
