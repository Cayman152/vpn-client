using Avalonia.Controls.Notifications;
using DialogHostAvalonia;
using GhostVPN.Desktop.Base;
using GhostVPN.Desktop.Common;
using GhostVPN.Desktop.Manager;
using GhostVPN.Desktop.ViewModels;

namespace GhostVPN.Desktop.Views;

public partial class MainWindow : WindowBase<MainWindowViewModel>
{
    private static Config _config;
    private readonly WindowNotificationManager? _manager;
    private BackupAndRestoreView? _backupAndRestoreView;
    private bool _blCloseByUser = false;

    public MainWindow()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;
        _manager = new WindowNotificationManager(TopLevel.GetTopLevel(this)) { MaxItems = 3, Position = NotificationPosition.TopRight };

        KeyDown += MainWindow_KeyDown;
        menuBackupAndRestore.Click += MenuBackupAndRestore_Click;
        ApplyGhostUiDefaults();
        _ = new ThemeSettingViewModel();

        ViewModel = new MainWindowViewModel(UpdateViewHandler);
        configurationsContent.Content ??= new ProfilesView(this);

        this.WhenActivated(disposables =>
        {
            //servers
            this.BindCommand(ViewModel, vm => vm.AddServerViaClipboardCmd, v => v.btnAddServerFromClipboard).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ToggleVpnCmd, v => v.btnToggleVpn).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddRoutingRuleCmd, v => v.btnRoutingRules).DisposeWith(disposables);

            //setting
            this.BindCommand(ViewModel, vm => vm.ReloadCmd, v => v.menuReload).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.BlReloadEnabled, v => v.menuReload.IsEnabled).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.TabMainSelectedIndex, v => v.tabShell.SelectedIndex).DisposeWith(disposables);

            AppEvents.SendSnackMsgRequested
              .AsObservable()
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(async content => await DelegateSnackMsg(content))
              .DisposeWith(disposables);

            AppEvents.AppExitRequested
              .AsObservable()
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(_ => StorageUI())
              .DisposeWith(disposables);

            AppEvents.ShutdownRequested
              .AsObservable()
              .ObserveOn(RxApp.MainThreadScheduler)
              .Subscribe(content => Shutdown(content))
              .DisposeWith(disposables);

            AppEvents.ShowHideWindowRequested
             .AsObservable()
             .ObserveOn(RxApp.MainThreadScheduler)
             .Subscribe(blShow => ShowHideWindow(blShow))
             .DisposeWith(disposables);
        });

        if (Utils.IsWindows())
        {
            Title = $"{Utils.GetVersion()} - {(Utils.IsAdministrator() ? ResUI.RunAsAdmin : ResUI.NotRunAsAdmin)}";

            ThreadPool.RegisterWaitForSingleObject(Program.ProgramStarted, OnProgramStarted, null, -1, false);
            HotkeyManager.Instance.Init(_config, OnHotkeyHandler);
        }
        else
        {
            Title = Utils.GetVersion();
        }
    }

    #region Event

    private void OnProgramStarted(object state, bool timeout)
    {
        Dispatcher.UIThread.Post(() =>
                ShowHideWindow(true),
            DispatcherPriority.Default);
    }

    private async Task DelegateSnackMsg(string content)
    {
        _manager?.Show(new Notification(null, content, NotificationType.Information));
        await Task.CompletedTask;
    }

    private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.AddServerWindow:
                if (obj is null)
                {
                    return false;
                }

                return await new AddServerWindow((ProfileItem)obj).ShowDialog<bool>(this);

            case EViewAction.AddServer2Window:
                if (obj is null)
                {
                    return false;
                }

                return await new AddServer2Window((ProfileItem)obj).ShowDialog<bool>(this);

            case EViewAction.AddGroupServerWindow:
                if (obj is null)
                {
                    return false;
                }

                return await new AddGroupServerWindow((ProfileItem)obj).ShowDialog<bool>(this);

            case EViewAction.DNSSettingWindow:
                return await new DNSSettingWindow().ShowDialog<bool>(this);

            case EViewAction.FullConfigTemplateWindow:
                return await new FullConfigTemplateWindow().ShowDialog<bool>(this);

            case EViewAction.RoutingSettingWindow:
                return await new RoutingSettingWindow().ShowDialog<bool>(this);

            case EViewAction.OptionSettingWindow:
                return await new OptionSettingWindow().ShowDialog<bool>(this);

            case EViewAction.GlobalHotkeySettingWindow:
                return await new GlobalHotkeySettingWindow().ShowDialog<bool>(this);

            case EViewAction.SubSettingWindow:
                return await new SubSettingWindow().ShowDialog<bool>(this);

            case EViewAction.ScanScreenTask:
                await ScanScreenTaskAsync();
                break;

            case EViewAction.ScanImageTask:
                await ScanImageTaskAsync();
                break;

            case EViewAction.AddServerViaClipboard:
                await AddServerViaClipboardAsync();
                break;
        }

        return await Task.FromResult(true);
    }

    private void OnHotkeyHandler(EGlobalHotkey e)
    {
        switch (e)
        {
            case EGlobalHotkey.ShowForm:
                ShowHideWindow(null);
                break;

            case EGlobalHotkey.SystemProxyClear:
            case EGlobalHotkey.SystemProxySet:
            case EGlobalHotkey.SystemProxyUnchanged:
            case EGlobalHotkey.SystemProxyPac:
                AppEvents.SysProxyChangeRequested.Publish((ESysProxyType)((int)e - 1));
                break;
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        if (_blCloseByUser)
        {
            base.OnClosing(e);
            return;
        }

        Logging.SaveLog("OnClosing -> " + e.CloseReason.ToString());

        switch (e.CloseReason)
        {
            case WindowCloseReason.OwnerWindowClosing or WindowCloseReason.WindowClosing:
                e.Cancel = true;
                _blCloseByUser = true;
                StorageUI();
                await AppManager.Instance.AppExitAsync(true);
                return;

            case WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown:
                await AppManager.Instance.AppExitAsync(false);
                break;
        }

        base.OnClosing(e);
    }

    private async void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
        {
            switch (e.Key)
            {
                case Key.V:
                    await AddServerViaClipboardAsync();
                    break;
            }
        }
        else
        {
            if (e.Key == Key.F5)
            {
                ViewModel?.Reload();
            }
        }
    }

    public async Task AddServerViaClipboardAsync()
    {
        var clipboardData = await AvaUtils.GetClipboardData(this);
        if (clipboardData.IsNotEmpty() && ViewModel != null)
        {
            await ViewModel.AddServerViaClipboardAsync(clipboardData);
        }
    }

    public async Task ScanScreenTaskAsync()
    {
        //ShowHideWindow(false);

        NoticeManager.Instance.SendMessageAndEnqueue("Функция пока не реализована.");
        await Task.CompletedTask;
        //if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        //{
        //    //var bytes = QRCodeHelper.CaptureScreen(desktop);
        //    //await ViewModel?.ScanScreenResult(bytes);
        //}

        //ShowHideWindow(true);
    }

    private async Task ScanImageTaskAsync()
    {
        var fileName = await UI.OpenFileDialog(this, null);
        if (fileName.IsNullOrEmpty())
        {
            return;
        }

        if (ViewModel != null)
        {
            await ViewModel.ScanImageResult(fileName);
        }
    }

    private void MenuBackupAndRestore_Click(object? sender, RoutedEventArgs e)
    {
        _backupAndRestoreView ??= new BackupAndRestoreView(this);
        DialogHost.Show(_backupAndRestoreView);
    }

    private void Shutdown(bool obj)
    {
        if (obj is bool b && _blCloseByUser == false)
        {
            _blCloseByUser = b;
        }
        StorageUI();
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            HotkeyManager.Instance.Dispose();
            desktop.Shutdown();
        }
    }

    #endregion Event

    #region UI

    public void ShowHideWindow(bool? blShow)
    {
        var bl = blShow ??
                    (Utils.IsLinux()
                    ? (!AppManager.Instance.ShowInTaskbar ^ (WindowState == WindowState.Minimized))
                    : !AppManager.Instance.ShowInTaskbar);
        if (bl)
        {
            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Focus();
        }
        else
        {
            if (Utils.IsLinux() && _config.UiItem.Hide2TrayWhenClose == false)
            {
                WindowState = WindowState.Minimized;
                return;
            }

            foreach (var ownedWindow in OwnedWindows)
            {
                ownedWindow.Close();
            }
            Hide();
        }

        AppManager.Instance.ShowInTaskbar = bl;
    }

    protected override void OnLoaded(object? sender, RoutedEventArgs e)
    {
        base.OnLoaded(sender, e);
        RestoreUI();
    }

    private void ApplyGhostUiDefaults()
    {
        _config.UiItem.AutoHideStartup = false;
        _config.UiItem.Hide2TrayWhenClose = false;
        _config.UiItem.CurrentTheme = nameof(ETheme.Dark);
        _config.UiItem.CurrentFontSize = 14;
    }

    private void RestoreUI()
    {
        var windowSize = ConfigHandler.GetWindowSizeItem(_config, GetType().Name);
        if (windowSize != null)
        {
            Width = windowSize.Width;
            Height = windowSize.Height;
        }
    }

    private void StorageUI()
    {
        ConfigHandler.SaveWindowSizeItem(_config, GetType().Name, Width, Height);
    }

    #endregion UI
}
