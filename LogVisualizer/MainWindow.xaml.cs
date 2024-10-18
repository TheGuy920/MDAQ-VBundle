using LogVisualizer.Models;
using Microsoft.Win32;
using MotecLogSerializer.LdParser;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Timers;
using Timer = System.Timers.Timer;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace LogVisualizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{  
    public bool CanUnselectAll => this.ActiveGraph is not null && this.ActiveGraph.Channels.Values.Any(g => g.ScatterLine.IsVisible);
    public List<MenuItem> FileGraphItems => this.Files.Select(ToMenuItem).ToList();
    public bool CanUnselectGraph => this.ActiveGraph is not null;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool CanCloseAll => this.Files.Count > 0;

    readonly Dictionary<string, LdData> Files = [];
    readonly List<PlotGraph> Graphs = [];
    private Point? GrapplePoint = null;
    private Point? ScalePoint = null;
    PlotGraph? _activeGraph = null;
    private readonly string _title;
    string searchFilter = "";
    bool searchOpen = false;

    PlotGraph? ActiveGraph
    {
        get => _activeGraph;
        set
        {
            _activeGraph = value;
            OnPropertyChanged(nameof(CanUnselectGraph));
            OnPropertyChanged(nameof(CanUnselectAll));
        }
    }
    
    private readonly Timer ScaleCheck = new()
    {
        Interval = 100,
        AutoReset = true,
        Enabled = false,
    };

    public MainWindow()
    {
        this.InitializeComponent();
        this._title = this.Title;
        this.ScaleCheck.Elapsed += (s, e) => this.Dispatcher.Invoke(this.CheckScale);
        this.DataContext = this;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => this.WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        this.MainGridView.Margin = this.WindowState == WindowState.Maximized ? new Thickness(5) : new Thickness(0);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => this.Close();

    private void DockPanel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        this.GrapplePoint = e.GetPosition(this);
        this.ScalePoint = null;
        this.ScaleCheck.Stop();
        e.Handled = true;
    }

    private void WindowMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        this.GrapplePoint = null;
        this.ScalePoint = null;
        this.ScaleCheck.Stop();
    }

    private void WindowMouseMove(object sender, MouseEventArgs e)
    {
        if (this.GrapplePoint is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            Point p = e.GetPosition(this);
            this.Left += p.X - this.GrapplePoint.Value.X;
            this.Top += p.Y - this.GrapplePoint.Value.Y;
        }

        this.CheckScale();
    }

    private void CheckScale()
    {
        if (this.ScalePoint is not null && Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed)
        {
            Point p = Mouse.PrimaryDevice.GetPosition(this);
            Debug.WriteLine(p);
            if (this.ScalePoint.Value.X > 0)
                this.Width = p.X + 15;

            if (this.ScalePoint.Value.Y > 0)
                this.Height = p.Y + 15;
        }
    }

    private void UncheckAllButtonClick(object sender, RoutedEventArgs e)
    {
        this.ActiveGraph?.UncheckAll();
        OnPropertyChanged(nameof(CanUnselectAll));
    }
    
    private void UnselectAllButtonClick(object sender, RoutedEventArgs e)
    {
        this.GraphOnSelected(null);
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

        if (this.Files.ContainsKey(openFileDialog.FileName))
            return;

        this.Files.Add(openFileDialog.FileName, LdData.FromFile(openFileDialog.FileName));
        OnPropertyChanged(nameof(FileGraphItems));
        OnPropertyChanged(nameof(CanCloseAll));
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

    private void TextBoxLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (this.searchOpen && this.SearchBar.Text.Length <= 0)
            this.ClearSearch(sender, null);
    }

    private void TextBoxTextChanged(object sender, TextChangedEventArgs? e)
    {
        if (this.searchOpen && this.ListView is not null)
        {
            this.searchFilter = this.SearchBar.Text;
            foreach (LineGraph graph in this.ListView.Items.OfType<LineGraph>())
                graph.Visibility = graph.Key.Contains(this.searchFilter, StringComparison.InvariantCultureIgnoreCase) ? Visibility.Visible : Visibility.Hidden;
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

    private void GraphOnSelected(PlotGraph? obj)
    {
        foreach (PlotGraph graph in this.Graphs)
        {
            if (graph == obj)
                continue;
            graph.Unselected();
        }

        this.ActiveGraph = obj;
        this.ListView.ItemsSource = obj?.OrderedChannels;
    }

    private void ScaleOneMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement elem)
        {
            Point cursorPos = e.GetPosition(this);
            if (elem.Tag == null)
            {
                cursorPos.Y = 0;
                this.ScalePoint = cursorPos;
            }
            else
            {
                cursorPos.X = 0;
                this.ScalePoint = cursorPos;
            }

            this.GrapplePoint = null;
            this.ScaleCheck.Start();
        }
    }

    private void ScaleTwoMouseDown(object sender, MouseButtonEventArgs e)
    {
        Point cursorPos = e.GetPosition(this);
        this.ScalePoint = cursorPos;
        this.GrapplePoint = null;
        this.ScaleCheck.Start();
    }

    protected void OnPropertyChanged(string name)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private void CloseAllFiles(object sender, RoutedEventArgs e)
    {
        this.Title = this._title;
        this.GraphViewer.Children.Clear();
        this.ListView.ItemsSource = null;
        this.Files.Clear();

        OnPropertyChanged(nameof(FileGraphItems));
        OnPropertyChanged(nameof(CanCloseAll));
    }

    private MenuItem ToMenuItem(KeyValuePair<string, LdData> item)
    {
        MenuItem menuItem = new() { Header = item.Key, Tag = item, HorizontalContentAlignment=HorizontalAlignment.Center, VerticalContentAlignment=VerticalAlignment.Center };
        menuItem.Click += this.OpenGraph;
        return menuItem;
    }

    private void OpenGraph(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem mitem)
        {
            var data = (KeyValuePair<string, LdData>)mitem.Tag;
            PlotGraph graph = new(data.Key, data.Value.Channels);
            graph.Graph.LayoutUpdated += (s, _) => OnPropertyChanged(nameof(CanUnselectAll));
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
    }
}