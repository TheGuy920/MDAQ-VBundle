using LogVisualizer.Models;
using Microsoft.Win32;
using MotecLogSerializer.LdParser;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Timer = System.Timers.Timer;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace LogVisualizer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    public bool CanUnselectAll => this.ActiveGraph is not null && this.ActiveGraph.Channels.Values.Any(g => g.Line.IsVisible);
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

    private UniformGrid ActiveView => (UniformGrid)this.TabController.SelectedContent;

    public MainWindow()
    {
        this.InitializeComponent();
        this._title = this.Title;
        this.ScaleCheck.Elapsed += (s, e) => this.Dispatcher.Invoke(this.CheckScale);
        this.DataContext = this;

        // Set the initial view to the first tab
        this.TabController.Items.Insert(this.TabController.Items.Count-1, this.NewTab(false));
    }

    private int counter = 0;
    private TabItem NewTab(bool canClose = true)
    {
        DockPanel dockPanel = new();
        TabItem tbi = new() { Content = new UniformGrid() { Background = System.Windows.Media.Brushes.Transparent, }, Header = dockPanel, Background = Brushes.Gray };
        var tb = new TextBox() 
        { 
            Background = Brushes.Transparent, Text = "Tab " + ++counter, IsReadOnly = true, BorderBrush = Brushes.Transparent,
            AcceptsReturn = true, IsReadOnlyCaretVisible = false, IsHitTestVisible = false, MinWidth = 10
        };
        void lostFocus(object _, object __)
        {
            tb.IsReadOnly = true;
            tb.IsHitTestVisible = false;
            tb.BorderBrush = Brushes.Transparent;
            tb.Background = tb.Text.Length == 0 ? new SolidColorBrush(Color.FromArgb(0xF0, 0xFF, 0x00, 0x00)) : (Brush)Brushes.Transparent;
            tb.Focusable = false;
        }
        tbi.PreviewMouseDoubleClick += (s, e) =>
        {
            tb.IsHitTestVisible = true;
            tb.IsReadOnly = false;
            tb.BorderBrush = Brushes.Cyan;
            tb.Focusable = true;
            Task.Run(() => this.Dispatcher.Invoke(tb.Focus));
        };
        tb.LostFocus += lostFocus;
        tb.PreviewLostKeyboardFocus += lostFocus;
        tb.TextChanged += (s, e) =>
        {
            if (tb.Text.Contains('\n'))
            {
                tb.Text = tb.Text.Replace("\r", "").Replace("\n", "");
                lostFocus(s, e);
            }
        };
        dockPanel.Children.Add(tb);

        Button closeButton = new() { 
            Content = "x", Background = Brushes.Transparent, Foreground = Brushes.Red,
            HorizontalAlignment = HorizontalAlignment.Right, BorderBrush=Brushes.Transparent,
            Padding=new(-5), Margin=new(10,0,0,0), Width=15,
            FontFamily = new("Cascadia Mono"), VerticalAlignment = VerticalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center
        };
        if (canClose)
            dockPanel.Children.Add(closeButton);

        closeButton.Click += (s, e) =>
        {
            this.TabController.SelectedIndex -= 1;
            this.TabController.Items.Remove(tbi);
        };

        return tbi;
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

    private void DockPanelMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        this.TDock.Focus();
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

        if (sender is not ToggleButton)
            this.ToggleButtonClick(sender, null);
    }

    private void GraphOnSelected(PlotGraph? obj)
    {
        if (this.ActiveGraph == obj)
            return;

        foreach (PlotGraph graph in this.Graphs)
        {
            if (graph == obj)
                continue;
            graph.Unselected();
        }

        this.ActiveGraph = obj;
        if (obj is null) {
            this.ListView.ItemsSource = null;
            return;
        }

        ObservableCollection<UIElement> tmpCollection = [];
        this.ListView.ItemsSource = tmpCollection;

        Task.Run(async () =>
        {
            foreach(LineGraph item in obj.OrderedChannels)
            {
                this.Dispatcher.BeginInvoke(tmpCollection.Add, item);
                await Task.Delay(1);
            }
        });
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
        this.ActiveView?.Children.Clear();
        this.ListView.ItemsSource = null;
        this.Files.Clear();

        OnPropertyChanged(nameof(FileGraphItems));
        OnPropertyChanged(nameof(CanCloseAll));
    }

    private MenuItem ToMenuItem(KeyValuePair<string, LdData> item)
    {
        MenuItem menuItem = new() { Header = item.Key, Tag = item, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
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
                this.ActiveView.Children.Remove(g);

                if (this.ActiveGraph == g)
                {
                    this.ActiveGraph = null;
                    this.ListView.ItemsSource = null;
                }
            };

            this.Graphs.Add(graph);
            this.ActiveView.Children.Add(graph);
        }
    }

    private void ChangeLayout(object sender, RoutedEventArgs e)
    {
        var ls = new LayoutSelector(this.ActiveView.Rows, this.ActiveView.Columns);
        ls.ShowDialog();

        this.ActiveView.Rows = ls.Row;
        this.ActiveView.Columns = ls.Column;
    }

    private void ToggleButtonClick(object sender, RoutedEventArgs? e)
    {
        if (OnlyViewEnabled.IsChecked == true)
        {
            OnlyViewEnabled.Foreground = Brushes.Red;
            foreach (LineGraph graph in this.ListView.Items.OfType<LineGraph>())
                graph.Visibility = graph.VisibilityCB.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
        else if (sender is ToggleButton)
        {
            OnlyViewEnabled.Foreground = Brushes.Black;
            if (this.searchOpen)
                this.TextBoxTextChanged(sender, null);
            else
                this.ClearSearch(sender, null);
        }
    }

    private void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is TabControl tabControl && tabControl.SelectedItem is TabItem tbi)
        {
            if (tbi == this.NewTabControl)
            {
                Task.Run(() => this.Dispatcher.Invoke(() =>
                {
                    int cnt = this.TabController.Items.Count - 1;
                    TabItem ntb = this.NewTab();
                    this.TabController.Items.Insert(cnt, ntb);
                    this.TabController.SelectedItem = ntb;
                }));
            }
            else
            {
                this.GraphOnSelected(null);
            }
        }
    }

    private void EnableAll(object sender, RoutedEventArgs e)
    {
        foreach (LineGraph graph in this.ListView.Items.OfType<LineGraph>())
            graph.TmpEnable();
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {

    }
}