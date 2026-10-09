using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfApplication = System.Windows.Application;

namespace PersonalNavigator;

public partial class MainWindow : Window
{
    private readonly string _personalRoot;
    private readonly SearchEngine _engine;
    private NavigatorSettings _settings;
    private readonly DispatcherTimer _searchTimer;
    private readonly Stack<string> _history = new();
    private List<SearchResult> _visibleResults = [];
    private SearchResult? _selected;
    private string _currentPath;
    private bool _deepSearch;
    private bool _indexing;
    private bool _allowExit;
    private double _mapZoom = 1;
    private FileSystemWatcher? _watcher;
    private System.Threading.Timer? _refreshTimer;
    private CancellationTokenSource? _indexCancellation;
    private System.Windows.Forms.NotifyIcon? _trayIcon;
    private string? _screenshotPath;
    private string? _screenshotQuery;
    private bool _screenshotCaptured;

    private static readonly string[] Palette =
    [
        "#6EE7D8", "#82B1FF", "#C4A7E7", "#F6C177", "#EB8F8F", "#8BD5CA", "#A6DA95"
    ];

    public MainWindow()
    {
        InitializeComponent();
        _personalRoot = ResolvePersonalRoot();
        _currentPath = _personalRoot;
        _settings = NavigatorSettings.Load();
        _deepSearch = _settings.DefaultDeepSearch;
        _engine = new SearchEngine(_personalRoot);
        _engine.IndexUpdated += count => Dispatcher.Invoke(() =>
        {
            _indexing = false;
            LoadingPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = $"{count:N0} items indexed locally";
            RenderCurrentView();
            ScheduleScreenshotIfNeeded();
        });

        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); RenderCurrentView(); };

        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    private static string ResolvePersonalRoot()
    {
        string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Personal");
        if (Directory.Exists(expected)) return expected;
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (directory.Name.Equals("Personal", StringComparison.OrdinalIgnoreCase)) return directory.FullName;
            directory = directory.Parent;
        }
        return expected;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        WindowsIntegration.ApplyDarkWindow(this);
        bool registered = WindowsIntegration.RegisterGlobalHotKey(this, ToggleVisibility);
        if (!registered) StatusText.Text = "Ctrl+Alt+Space is already used by another app";
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Topmost = _settings.AlwaysOnTop;
        ApplySettingsToControls();
        if (!string.IsNullOrWhiteSpace(_screenshotQuery)) SearchBox.Text = _screenshotQuery;
        InitializeTrayIcon();
        bool cached = _engine.TryLoadCache();
        if (cached)
        {
            StatusText.Text = $"{_engine.EntryCount:N0} cached items • refreshing quietly";
            RenderCurrentView();
        }
        else
        {
            LoadingPanel.Visibility = Visibility.Visible;
            _indexing = true;
        }

        StartWatcher();
        await RebuildIndexAsync(showLoading: !cached);
    }

    private void ScheduleScreenshotIfNeeded()
    {
        if (_screenshotCaptured || string.IsNullOrWhiteSpace(_screenshotPath)) return;
        _screenshotCaptured = true;
        Dispatcher.BeginInvoke(async () =>
        {
            await Task.Delay(450);
            try
            {
                UpdateLayout();
                int pixelWidth = Math.Max(1, (int)Math.Round(ActualWidth));
                int pixelHeight = Math.Max(1, (int)Math.Round(ActualHeight));
                var bitmap = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(this);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                string? parent = Path.GetDirectoryName(_screenshotPath);
                if (!string.IsNullOrWhiteSpace(parent)) Directory.CreateDirectory(parent);
                using var stream = File.Create(_screenshotPath);
                encoder.Save(stream);
            }
            finally
            {
                _allowExit = true;
                WpfApplication.Current.Shutdown();
            }
        }, DispatcherPriority.ApplicationIdle);
    }

    public void InitializeHidden()
    {
        ShowInTaskbar = false;
        Show();
        Hide();
        ShowInTaskbar = true;
    }

    public void EnableScreenshotMode(string outputPath, string? query)
    {
        _screenshotPath = outputPath;
        _screenshotQuery = query;
    }

    public void ActivateFromExternal(string? target)
    {
        ShowNavigator();
        if (!string.IsNullOrWhiteSpace(target))
        {
            try
            {
                string full = Path.GetFullPath(target);
                if (full.StartsWith(_personalRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string folder = Directory.Exists(full) ? full : Path.GetDirectoryName(full) ?? _personalRoot;
                    NavigateTo(folder, addHistory: false);
                }
            }
            catch { }
        }
        SearchBox.Focus();
    }

    private void InitializeTrayIcon()
    {
        if (_trayIcon is not null) return;
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open Personal Map", null, (_, _) => Dispatcher.Invoke(ShowNavigator));
        menu.Items.Add("Rebuild index", null, (_, _) => Dispatcher.Invoke(async () => await RebuildIndexAsync(true)));
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Dispatcher.Invoke(ExitApplication));
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "Personal Navigator - Ctrl+Alt+Space",
            Icon = System.Drawing.SystemIcons.Application,
            Visible = true,
            ContextMenuStrip = menu
        };
        _trayIcon.DoubleClick += (_, _) => Dispatcher.Invoke(ShowNavigator);
    }

    private void StartWatcher()
    {
        try
        {
            _watcher = new FileSystemWatcher(_personalRoot)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            FileSystemEventHandler changed = (_, e) => ScheduleRefresh(e.FullPath);
            RenamedEventHandler renamed = (_, e) => ScheduleRefresh(e.FullPath);
            _watcher.Created += changed;
            _watcher.Deleted += changed;
            _watcher.Changed += changed;
            _watcher.Renamed += renamed;
        }
        catch { }
    }

    private void ScheduleRefresh(string path)
    {
        if (_settings.ExcludedFolderSet().Any(name => path.Split(Path.DirectorySeparatorChar)
                .Contains(name, StringComparer.OrdinalIgnoreCase))) return;
        _refreshTimer?.Dispose();
        _refreshTimer = new System.Threading.Timer(_ => Dispatcher.Invoke(async () =>
        {
            if (!_indexing) await RebuildIndexAsync(false);
        }), null, 2200, Timeout.Infinite);
    }

    private async Task RebuildIndexAsync(bool showLoading)
    {
        if (_indexing && _indexCancellation is not null) _indexCancellation.Cancel();
        _indexCancellation = new CancellationTokenSource();
        _indexing = true;
        if (showLoading) LoadingPanel.Visibility = Visibility.Visible;
        StatusText.Text = "Refreshing local search index…";
        try
        {
            await _engine.RebuildAsync(_settings, _indexCancellation.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _indexing = false;
            LoadingPanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "Index refresh failed: " + ex.Message;
        }
    }

    private void RenderCurrentView()
    {
        if (!IsLoaded || MapCanvas.ActualWidth < 100 || MapCanvas.ActualHeight < 100) return;
        string query = SearchBox.Text.Trim();
        if (query.Length > 0)
        {
            _visibleResults = _engine.Search(query, _deepSearch, _settings.MaxResults);
            BreadcrumbText.Text = $"Personal  ›  Search: {query}";
            StatusText.Text = _visibleResults.Count == 0
                ? $"No close matches among {_engine.EntryCount:N0} indexed items"
                : _deepSearch
                    ? $"{_visibleResults.Count} detailed matches • names, paths and related terms"
                    : $"{_visibleResults.Count} project-level matches • use More detail for files";
        }
        else
        {
            _visibleResults = _engine.Children(_currentPath, _settings.MaxResults);
            BreadcrumbText.Text = FriendlyPath(_currentPath);
            StatusText.Text = _currentPath.Equals(_personalRoot, StringComparison.OrdinalIgnoreCase)
                ? $"{_engine.EntryCount:N0} items indexed locally • choose an area"
                : $"{_visibleResults.Count} nearby items • double-click a folder to explore";
        }

        NoResultsPanel.Visibility = _visibleResults.Count == 0 && !_indexing ? Visibility.Visible : Visibility.Collapsed;
        SelectionActions.Visibility = Visibility.Collapsed;
        _selected = null;
        DrawMap(query);
    }

    private void DrawMap(string query)
    {
        MapCanvas.Children.Clear();
        double width = MapCanvas.ActualWidth;
        double height = MapCanvas.ActualHeight;
        double centerX = width / 2;
        double centerY = height / 2;
        double scale = _settings.NodeScale;
        double nodeWidth = 166 * scale;
        double nodeHeight = 70 * scale;
        double radiusX = Math.Max(210, width / 2 - nodeWidth / 2 - 46);
        double radiusY = Math.Max(135, height / 2 - nodeHeight / 2 - 38);

        var positions = new List<Point>();
        for (int i = 0; i < _visibleResults.Count; i++)
        {
            double angle = -Math.PI / 2 + i * (Math.PI * 2 / Math.Max(1, _visibleResults.Count));
            positions.Add(new Point(centerX + Math.Cos(angle) * radiusX, centerY + Math.Sin(angle) * radiusY));
        }

        foreach (Point position in positions)
        {
            var line = new System.Windows.Shapes.Line
            {
                X1 = centerX,
                Y1 = centerY,
                X2 = position.X,
                Y2 = position.Y,
                Stroke = new SolidColorBrush(Color.FromArgb(95, 62, 83, 101)),
                StrokeThickness = 1.4,
                IsHitTestVisible = false
            };
            MapCanvas.Children.Add(line);
        }

        string centerTitle = query.Length > 0 ? "Search" : Path.GetFileName(_currentPath);
        if (string.IsNullOrEmpty(centerTitle)) centerTitle = "Personal";
        string centerDetail = query.Length > 0 ? $"“{Trim(query, 24)}”" : (_currentPath == _personalRoot ? "Your local map" : "Current folder");
        var center = CreateCenterNode(centerTitle, centerDetail, 184 * scale, 82 * scale);
        Canvas.SetLeft(center, centerX - center.Width / 2);
        Canvas.SetTop(center, centerY - center.Height / 2);
        MapCanvas.Children.Add(center);

        for (int i = 0; i < _visibleResults.Count; i++)
        {
            var node = CreateResultNode(_visibleResults[i], i, nodeWidth, nodeHeight);
            Canvas.SetLeft(node, positions[i].X - nodeWidth / 2);
            Canvas.SetTop(node, positions[i].Y - nodeHeight / 2);
            MapCanvas.Children.Add(node);
            if (_settings.Animate) AnimateNode(node, i);
        }
    }

    private Border CreateCenterNode(string title, string detail, double width, double height)
    {
        var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        panel.Children.Add(new TextBlock
        {
            Text = title, FontSize = 17, FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("TextBrush"), HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Text = detail, FontSize = 11, Foreground = FindBrush("AccentBrush"),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0)
        });
        return new Border
        {
            Width = width, Height = height, CornerRadius = new CornerRadius(18),
            Background = new SolidColorBrush(Color.FromRgb(19, 40, 43)),
            BorderBrush = FindBrush("AccentBrush"), BorderThickness = new Thickness(1.2),
            Child = panel, Effect = new System.Windows.Media.Effects.DropShadowEffect
            { BlurRadius = 24, ShadowDepth = 0, Opacity = .28, Color = Color.FromRgb(72, 210, 194) }
        };
    }

    private Border CreateResultNode(SearchResult result, int index, double width, double height)
    {
        Color accent = (Color)ColorConverter.ConvertFromString(Palette[Math.Abs(TopCategory(result.RelativePath).GetHashCode()) % Palette.Length]);
        var title = new TextBlock
        {
            Text = Trim(result.Name, 25), FontSize = 13.5, FontWeight = FontWeights.SemiBold,
            Foreground = FindBrush("TextBrush"), TextTrimming = TextTrimming.CharacterEllipsis
        };
        var detail = new TextBlock
        {
            Text = Trim(result.Reason, 31), FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromRgb(135, 153, 173)),
            Margin = new Thickness(0, 5, 0, 0), TextTrimming = TextTrimming.CharacterEllipsis
        };
        var icon = new TextBlock
        {
            Text = result.IsDirectory ? "▰" : FileGlyph(result.Path), FontSize = 14,
            Foreground = new SolidColorBrush(accent), Margin = new Thickness(0, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center
        };
        var stack = new StackPanel();
        stack.Children.Add(title);
        stack.Children.Add(detail);
        var content = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 9, 10, 8) };
        content.Children.Add(icon);
        content.Children.Add(stack);
        var node = new Border
        {
            Width = width, Height = height, CornerRadius = new CornerRadius(13),
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 33)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(150, accent.R, accent.G, accent.B)),
            BorderThickness = new Thickness(1), Child = content, Cursor = Cursors.Hand,
            ToolTip = result.Path, Tag = result, RenderTransformOrigin = new Point(.5, .5)
        };
        node.MouseEnter += (_, _) => node.Background = new SolidColorBrush(Color.FromRgb(24, 34, 46));
        node.MouseLeave += (_, _) => node.Background = new SolidColorBrush(Color.FromRgb(17, 24, 33));
        node.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                if (result.IsDirectory) NavigateTo(result.Path, addHistory: true);
                else WindowsIntegration.OpenPath(result.Path);
                e.Handled = true;
            }
            else SelectResult(result, node, accent);
        };
        node.MouseRightButtonUp += (_, _) => ShowNodeMenu(result, node);
        return node;
    }

    private void SelectResult(SearchResult result, Border node, Color accent)
    {
        _selected = result;
        SelectionActions.Visibility = Visibility.Visible;
        BreadcrumbText.Text = result.RelativePath;
        StatusText.Text = result.IsDirectory
            ? "Double-click to explore • Open launches the folder"
            : $"{Path.GetExtension(result.Path).TrimStart('.').ToUpperInvariant()} file • double-click to open";
        foreach (var child in MapCanvas.Children.OfType<Border>().Where(x => x.Tag is SearchResult))
            child.BorderThickness = new Thickness(1);
        node.BorderThickness = new Thickness(2);
        node.BorderBrush = new SolidColorBrush(accent);
    }

    private void ShowNodeMenu(SearchResult result, FrameworkElement owner)
    {
        var menu = new ContextMenu { Background = FindBrush("PanelBrush"), Foreground = FindBrush("TextBrush") };
        var open = new MenuItem { Header = result.IsDirectory ? "Open folder" : "Open file" };
        open.Click += (_, _) => WindowsIntegration.OpenPath(result.Path);
        var explore = new MenuItem { Header = "Show in Explorer" };
        explore.Click += (_, _) => WindowsIntegration.RevealPath(result.Path);
        var copy = new MenuItem { Header = "Copy path" };
        copy.Click += (_, _) => Clipboard.SetText(result.Path);
        menu.Items.Add(open);
        if (result.IsDirectory)
        {
            var map = new MenuItem { Header = "Explore in map" };
            map.Click += (_, _) => NavigateTo(result.Path, true);
            menu.Items.Add(map);
        }
        menu.Items.Add(explore);
        menu.Items.Add(copy);
        owner.ContextMenu = menu;
        menu.IsOpen = true;
    }

    private void AnimateNode(UIElement node, int index)
    {
        var scale = new ScaleTransform(.82, .82);
        node.RenderTransform = scale;
        node.Opacity = 0;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(260);
        var delay = TimeSpan.FromMilliseconds(Math.Min(index * 28, 220));
        node.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { BeginTime = delay, EasingFunction = easing });
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(.82, 1, duration) { BeginTime = delay, EasingFunction = easing });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(.82, 1, duration) { BeginTime = delay, EasingFunction = easing });
    }

    private void NavigateTo(string path, bool addHistory)
    {
        if (!Directory.Exists(path)) return;
        if (addHistory && !_currentPath.Equals(path, StringComparison.OrdinalIgnoreCase)) _history.Push(_currentPath);
        _currentPath = path;
        SearchBox.Text = string.Empty;
        BackButton.Visibility = _currentPath.Equals(_personalRoot, StringComparison.OrdinalIgnoreCase) && _history.Count == 0
            ? Visibility.Collapsed : Visibility.Visible;
        RenderCurrentView();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (SearchBox.Text.Length > 0) { SearchBox.Clear(); return; }
        if (_history.Count > 0) NavigateTo(_history.Pop(), false);
        else
        {
            string? parent = Directory.GetParent(_currentPath)?.FullName;
            if (parent is not null && parent.StartsWith(_personalRoot, StringComparison.OrdinalIgnoreCase)) NavigateTo(parent, false);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        bool empty = string.IsNullOrWhiteSpace(SearchBox.Text);
        SearchPlaceholder.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
        ClearSearchButton.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _visibleResults.Count > 0)
        {
            var first = _visibleResults[0];
            if (first.IsDirectory) NavigateTo(first.Path, true);
            else WindowsIntegration.OpenPath(first.Path);
            e.Handled = true;
        }
        else if (e.Key == Key.Down && _visibleResults.Count > 0)
        {
            _selected = _visibleResults[0];
            SelectionActions.Visibility = Visibility.Visible;
            e.Handled = true;
        }
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e) => SearchBox.Clear();

    private void DepthButton_Click(object sender, RoutedEventArgs e)
    {
        _deepSearch = !_deepSearch;
        DepthButtonText.Text = _deepSearch ? "More detail" : "Project roots";
        RenderCurrentView();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsDrawer.Visibility = SettingsDrawer.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        if (SettingsDrawer.Visibility == Visibility.Visible) ApplySettingsToControls();
    }

    private void ApplySettingsToControls()
    {
        IncludeFilesCheck.IsChecked = _settings.IncludeFiles;
        HiddenCheck.IsChecked = _settings.IncludeHidden;
        AnimateCheck.IsChecked = _settings.Animate;
        TopmostCheck.IsChecked = _settings.AlwaysOnTop;
        StartupCheck.IsChecked = _settings.StartWithWindows;
        DefaultDeepCheck.IsChecked = _settings.DefaultDeepSearch;
        ExtensionsBox.Text = _settings.AllowedExtensions;
        ExcludedFoldersBox.Text = _settings.ExcludedFolders;
        MaxResultsSlider.Value = _settings.MaxResults;
        NodeScaleSlider.Value = _settings.NodeScale;
        MaxResultsLabel.Text = _settings.MaxResults.ToString();
        DepthButtonText.Text = _deepSearch ? "More detail" : "Project roots";
    }

    private async void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.IncludeFiles = IncludeFilesCheck.IsChecked == true;
        _settings.IncludeHidden = HiddenCheck.IsChecked == true;
        _settings.Animate = AnimateCheck.IsChecked == true;
        _settings.AlwaysOnTop = TopmostCheck.IsChecked == true;
        _settings.StartWithWindows = StartupCheck.IsChecked == true;
        _settings.DefaultDeepSearch = DefaultDeepCheck.IsChecked == true;
        _settings.AllowedExtensions = ExtensionsBox.Text.Trim();
        _settings.ExcludedFolders = ExcludedFoldersBox.Text.ReplaceLineEndings(",").Trim(' ', ',');
        _settings.MaxResults = (int)MaxResultsSlider.Value;
        _settings.NodeScale = NodeScaleSlider.Value;
        _settings.Save();
        Topmost = _settings.AlwaysOnTop;
        WindowsIntegration.InstallShellIntegration(Environment.ProcessPath ?? AppContext.BaseDirectory, _settings.StartWithWindows);
        SettingsDrawer.Visibility = Visibility.Collapsed;
        await RebuildIndexAsync(true);
    }

    private async void RebuildButton_Click(object sender, RoutedEventArgs e) => await RebuildIndexAsync(true);
    private void MaxResultsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxResultsLabel is not null) MaxResultsLabel.Text = ((int)e.NewValue).ToString();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not null) WindowsIntegration.OpenPath(_selected.Path);
    }

    private void RevealButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not null) WindowsIntegration.RevealPath(_selected.Path);
    }

    private void CopyPathButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is not null) Clipboard.SetText(_selected.Path);
    }

    private void MapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded) RenderCurrentView();
    }

    private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _mapZoom = Math.Clamp(_mapZoom + (e.Delta > 0 ? .08 : -.08), .72, 1.35);
        MapCanvas.RenderTransformOrigin = new Point(.5, .5);
        MapCanvas.RenderTransform = new ScaleTransform(_mapZoom, _mapZoom);
        StatusText.Text = $"Map zoom {Math.Round(_mapZoom * 100)}%";
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (SettingsDrawer.Visibility == Visibility.Visible) SettingsDrawer.Visibility = Visibility.Collapsed;
            else Hide();
            e.Handled = true;
        }
        else if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || IsInteractive(e.OriginalSource as DependencyObject)) return;
        if (e.ClickCount == 2) WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        else DragMove();
    }

    private static bool IsInteractive(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button or TextBox or Slider or CheckBox) return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void HideButton_Click(object sender, RoutedEventArgs e) => Hide();

    private void ToggleVisibility()
    {
        if (IsVisible && WindowState != WindowState.Minimized && IsActive) Hide();
        else ShowNavigator();
    }

    private void ShowNavigator()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        Topmost = _settings.AlwaysOnTop;
        SearchBox.Focus();
    }

    private void ExitApplication()
    {
        _allowExit = true;
        _trayIcon!.Visible = false;
        WpfApplication.Current.Shutdown();
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowExit) return;
        e.Cancel = true;
        Hide();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        WindowsIntegration.UnregisterGlobalHotKey(this);
        _indexCancellation?.Cancel();
        _watcher?.Dispose();
        _refreshTimer?.Dispose();
        if (_trayIcon is not null) { _trayIcon.Visible = false; _trayIcon.Dispose(); }
    }

    private Brush FindBrush(string key) => (Brush)FindResource(key);
    private string FriendlyPath(string path)
    {
        string relative = Path.GetRelativePath(_personalRoot, path);
        return relative == "." ? "Personal" : "Personal  ›  " + relative.Replace("\\", "  ›  ");
    }
    private static string TopCategory(string relative) => relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).FirstOrDefault() ?? relative;
    private static string Trim(string value, int length) => value.Length <= length ? value : value[..Math.Max(1, length - 1)] + "…";
    private static string FileGlyph(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".webp" => "▧",
        ".mp3" or ".wav" or ".mp4" or ".mov" => "▶",
        ".cs" or ".py" or ".js" or ".ts" or ".tsx" or ".cpp" => "‹›",
        ".pdf" or ".docx" or ".txt" or ".md" => "▤",
        _ => "◇"
    };
}
