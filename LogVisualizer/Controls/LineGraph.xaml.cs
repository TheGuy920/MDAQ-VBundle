using MotecLogSerializer.LdParser;
using ScottPlot;
using ScottPlot.DataSources;
using ScottPlot.Plottables;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;

namespace LogVisualizer.Models;

/// <summary>
/// Interaction logic for LineGraph.xaml
/// </summary>
public partial class LineGraph : UserControl
{
    public readonly string Key;
    public readonly uint MetaPtr;

    public Signal Line { get; private set; }
    public event Action? GraphUpdated;

    private bool IsChecked => this.VisibilityCB.IsChecked == true;

    public LineGraph(LdChan channel)
    {
        InitializeComponent();
        this.Key = channel.Name;

        var rand = new Random();
        byte colorR = (byte)rand.Next(255);
        byte colorG = (byte)rand.Next(255);
        byte colorB = (byte)rand.Next(255);

        this.ColorPickerCtrl.SelectedColor = Color.FromRgb(colorR, colorG, colorB);
        this.MetaPtr = channel.MetaPtr;
        this.TitleTB.Text = this.Key;

        float[] dt = channel.Data;
        var dataPoints = new SignalSourceDouble([.. dt.Select((d, i) => (double)d)], 1d / channel.Frequency);
        this.Line = new Signal(dataPoints)
        {
            MarkerStyle = MarkerStyle.None,
            IsVisible = false,
            LineColor = new ScottPlot.Color(colorR, colorG, colorB),
            LineWidth = 3,
        };

        this.Line.LineColor = this.Line.LineColor.Lighten(0.4);
    }
    
    private void IsCheckedChanged(object? _, System.Windows.RoutedEventArgs? __)
    {
        this.Line.IsVisible = this.IsChecked;
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
        if (e.NewValue == null || this.Line == null)
            return;

        this.Line.LineColor = new ScottPlot.Color(e.NewValue.Value.R, e.NewValue.Value.G, e.NewValue.Value.B);
        this.GraphUpdated?.Invoke();
    }

    private static readonly double divisor = Math.Pow(Math.E, 5) - 1;

    private void ScaleDataValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (this.Line == null)
            return;

        //this.Line.ScaleY = 50 * (Math.Pow(Math.E, e.NewValue/20) - 1) / divisor;
        //this.ResetButton.IsEnabled = Math.Abs(this.Line.ScaleY - 1) >= 0.01;

        this.GraphUpdated?.Invoke();
    }

    private void ResetScale()
    {
        this.ScaleData.Value = 27.46551563;
        //this.Line.ScaleY = 1;
        this.GraphUpdated?.Invoke();
    }

    private void ResetButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.ResetScale();
    }

    public void TmpEnable()
    {
        this.VisibilityCB.IsChecked = true;
    }
}
