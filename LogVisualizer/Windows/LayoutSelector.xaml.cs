using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LogVisualizer;
/// <summary>
/// Interaction logic for LayoutSelector.xaml
/// </summary>
public partial class LayoutSelector : Window, INotifyPropertyChanged
{
    private System.Drawing.Point RowColumnCount = new();

    public int Row
    {
        get => RowColumnCount.X;
        set
        {
            this.RowColumnCount.X = value;
            this.Dispatcher.Invoke(UpdateGridView);
            this.OnPropertyChanged(nameof(Row));
        }
    }

    public int Column
    {
        get => RowColumnCount.Y;
        set
        {
            this.RowColumnCount.Y = value;
            this.Dispatcher.Invoke(UpdateGridView);
            this.OnPropertyChanged(nameof(Column));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public LayoutSelector(int rows, int cols)
    {
        this.InitializeComponent();
        this.DataContext = this;
        this.Row = rows;
        this.Column = cols;
    }

    private void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void UpdateGridView()
    {
        this.RowColVisualizer.ColumnDefinitions.Clear();
        this.RowColVisualizer.RowDefinitions.Clear();

        for (int i = 0; i < this.Row; i++)
            this.RowColVisualizer.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

        for (int i = 0; i < this.Column; i++)
            this.RowColVisualizer.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

        int count = this.Row * this.Column;
        for (int i = 0; i < count; i++)
        {
            var box = new Border()
            {
                Background = System.Windows.Media.Brushes.DimGray,
                BorderBrush = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
            };
            Grid.SetRow(box, i / this.Column);
            Grid.SetColumn(box, i % this.Column);
            this.RowColVisualizer.Children.Add(box);
        }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
