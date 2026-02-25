using System.Reactive.Concurrency;

namespace ServiceLib.ViewModels;

public class MainWindowViewModel : MyReactiveObject
{
    #region Menu

    //servers
    public ReactiveCommand<Unit, Unit> AddVmessServerCmd { get; }

    public ReactiveCommand<Unit, Unit> AddVlessServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddShadowsocksServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddSocksServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddHttpServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddTrojanServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddHysteria2ServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddTuicServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddWireguardServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddAnytlsServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddCustomServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddPolicyGroupServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddProxyChainServerCmd { get; }
    public ReactiveCommand<Unit, Unit> AddServerViaClipboardCmd { get; }
    public ReactiveCommand<Unit, Unit> AddServerViaScanCmd { get; }
    public ReactiveCommand<Unit, Unit> AddServerViaImageCmd { get; }

    //Subscription
    public ReactiveCommand<Unit, Unit> SubSettingCmd { get; }

    public ReactiveCommand<Unit, Unit> SubUpdateCmd { get; }
    public ReactiveCommand<Unit, Unit> SubUpdateViaProxyCmd { get; }
    public ReactiveCommand<Unit, Unit> SubGroupUpdateCmd { get; }
    public ReactiveCommand<Unit, Unit> SubGroupUpdateViaProxyCmd { get; }

    //Setting
    public ReactiveCommand<Unit, Unit> OptionSettingCmd { get; }

    public ReactiveCommand<Unit, Unit> RoutingSettingCmd { get; }
    public ReactiveCommand<Unit, Unit> DNSSettingCmd { get; }
    public ReactiveCommand<Unit, Unit> FullConfigTemplateCmd { get; }
    public ReactiveCommand<Unit, Unit> GlobalHotkeySettingCmd { get; }
    public ReactiveCommand<Unit, Unit> RebootAsAdminCmd { get; }
    public ReactiveCommand<Unit, Unit> ClearServerStatisticsCmd { get; }
    public ReactiveCommand<Unit, Unit> OpenTheFileLocationCmd { get; }

    //Presets
    public ReactiveCommand<Unit, Unit> RegionalPresetDefaultCmd { get; }

    public ReactiveCommand<Unit, Unit> RegionalPresetRussiaCmd { get; }

    public ReactiveCommand<Unit, Unit> RegionalPresetIranCmd { get; }

    public ReactiveCommand<Unit, Unit> ReloadCmd { get; }
    public ReactiveCommand<Unit, Unit> ToggleVpnCmd { get; }
    public ReactiveCommand<Unit, Unit> AddRoutingRuleCmd { get; }

    [Reactive]
    public bool BlReloadEnabled { get; set; }

    [Reactive]
    public bool ShowClashUI { get; set; }

    [Reactive]
    public int TabMainSelectedIndex { get; set; }

    [Reactive] public bool IsVpnConnected { get; set; }
    [Reactive] public string VpnToggleButtonText { get; set; } = "Включить VPN";
    [Reactive] public string ActiveCountryFlag { get; set; } = "🇱🇻";
    [Reactive] public string ActiveCountryName { get; set; } = "Латвия";
    [Reactive] public string ActiveSubscriptionName { get; set; } = "Ghost VPN";
    [Reactive] public string ActiveProtocol { get; set; } = "VLESS";

    #endregion Menu

    #region Init

    public MainWindowViewModel(Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppManager.Instance.Config;
        _updateView = updateView;
        ApplyVpnState(_config.SystemProxyItem.SysProxyType == ESysProxyType.ForcedChange);

        #region WhenAnyValue && ReactiveCommand

        //servers
        AddVmessServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.VMess);
        });
        AddVlessServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.VLESS);
        });
        AddShadowsocksServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.Shadowsocks);
        });
        AddSocksServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.SOCKS);
        });
        AddHttpServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.HTTP);
        });
        AddTrojanServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.Trojan);
        });
        AddHysteria2ServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.Hysteria2);
        });
        AddTuicServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.TUIC);
        });
        AddWireguardServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.WireGuard);
        });
        AddAnytlsServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.Anytls);
        });
        AddCustomServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.Custom);
        });
        AddPolicyGroupServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.PolicyGroup);
        });
        AddProxyChainServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerAsync(EConfigType.ProxyChain);
        });
        AddServerViaClipboardCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerViaClipboardAsync(null);
        });
        AddServerViaScanCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerViaScanAsync();
        });
        AddServerViaImageCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AddServerViaImageAsync();
        });

        //Subscription
        SubSettingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SubSettingAsync();
        });

        SubUpdateCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await UpdateSubscriptionProcess("", false);
        });
        SubUpdateViaProxyCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await UpdateSubscriptionProcess("", true);
        });
        SubGroupUpdateCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await UpdateSubscriptionProcess(_config.SubIndexId, false);
        });
        SubGroupUpdateViaProxyCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await UpdateSubscriptionProcess(_config.SubIndexId, true);
        });

        //Setting
        OptionSettingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await OptionSettingAsync();
        });
        RoutingSettingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RoutingSettingAsync();
        });
        DNSSettingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await DNSSettingAsync();
        });
        FullConfigTemplateCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await FullConfigTemplateAsync();
        });
        GlobalHotkeySettingCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            if (await _updateView?.Invoke(EViewAction.GlobalHotkeySettingWindow, null) == true)
            {
                NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
            }
        });
        RebootAsAdminCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await AppManager.Instance.RebootAsAdmin();
        });
        ClearServerStatisticsCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ClearServerStatistics();
        });
        OpenTheFileLocationCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await OpenTheFileLocation();
        });

        ReloadCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await Reload();
        });
        ToggleVpnCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            if (IsVpnConnected)
            {
                await DisconnectVpnAsync();
            }
            else
            {
                await ConnectVpnAsync();
            }
        });
        AddRoutingRuleCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await RoutingSettingAsync();
        });

        RegionalPresetDefaultCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ApplyRegionalPreset(EPresetType.Default);
        });

        RegionalPresetRussiaCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ApplyRegionalPreset(EPresetType.Russia);
        });

        RegionalPresetIranCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await ApplyRegionalPreset(EPresetType.Iran);
        });

        #endregion WhenAnyValue && ReactiveCommand

        #region AppEvents

        AppEvents.ReloadRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await Reload());

        AppEvents.AddServerViaScanRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await AddServerViaScanAsync());

        AppEvents.AddServerViaClipboardRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async _ => await AddServerViaClipboardAsync(null));

        AppEvents.SubscriptionsUpdateRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(async blProxy => await UpdateSubscriptionProcess("", blProxy));

        AppEvents.SysProxyChangeRequested
            .AsObservable()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(type => ApplyVpnState(type == ESysProxyType.ForcedChange));

        #endregion AppEvents

        _ = Init();
    }

    private async Task Init()
    {
        AppManager.Instance.ShowInTaskbar = true;

        //await ConfigHandler.InitBuiltinRouting(_config);
        await ConfigHandler.InitBuiltinDNS(_config);
        await ConfigHandler.InitBuiltinFullConfigTemplate(_config);
        await ProfileExManager.Instance.Init();
        await CoreManager.Instance.Init(_config, UpdateHandler);
        TaskManager.Instance.RegUpdateTask(_config, UpdateTaskHandler);

        if (_config.GuiItem.EnableStatistics || _config.GuiItem.DisplayRealTimeSpeed)
        {
            await StatisticsManager.Instance.Init(_config, UpdateStatisticsHandler);
        }
        await RefreshServers();
        await RefreshConnectionBadgeAsync();

        await Reload();
    }

    #endregion Init

    #region Actions

    private async Task UpdateHandler(bool notify, string msg)
    {
        NoticeManager.Instance.SendMessage(msg);
        if (notify)
        {
            NoticeManager.Instance.Enqueue(msg);
        }
        await Task.CompletedTask;
    }

    private async Task UpdateTaskHandler(bool success, string msg)
    {
        NoticeManager.Instance.SendMessageEx(msg);
        if (success)
        {
            var indexIdOld = _config.IndexId;
            await RefreshServers();
            if (indexIdOld != _config.IndexId)
            {
                await Reload();
            }
            if (_config.UiItem.EnableAutoAdjustMainLvColWidth)
            {
                AppEvents.AdjustMainLvColWidthRequested.Publish();
            }
        }
    }

    private async Task UpdateStatisticsHandler(ServerSpeedItem update)
    {
        if (!AppManager.Instance.ShowInTaskbar)
        {
            return;
        }
        AppEvents.DispatcherStatisticsRequested.Publish(update);
        await Task.CompletedTask;
    }

    private void ApplyVpnState(bool connected)
    {
        IsVpnConnected = connected;
        VpnToggleButtonText = connected ? "Отключить" : "Подключить";
    }

    private async Task ConnectVpnAsync()
    {
        await Reload();

        if (!CoreManager.Instance.IsCoreRunning())
        {
            AppEvents.SysProxyChangeRequested.Publish(ESysProxyType.ForcedClear);
            ApplyVpnState(false);
            NoticeManager.Instance.Enqueue("Не удалось подключить VPN. Проверьте сервер и параметры.");
            return;
        }

        AppEvents.SysProxyChangeRequested.Publish(ESysProxyType.ForcedChange);
        ApplyVpnState(true);
        NoticeManager.Instance.Enqueue("VPN включен");
    }

    private async Task DisconnectVpnAsync()
    {
        AppEvents.SysProxyChangeRequested.Publish(ESysProxyType.ForcedClear);
        await CoreManager.Instance.CoreStop();
        AppEvents.TestServerRequested.Publish();
        ApplyVpnState(false);
        NoticeManager.Instance.Enqueue("VPN отключен");
    }

    #endregion Actions

    #region Servers && Groups

    private async Task RefreshServers()
    {
        AppEvents.ProfilesRefreshRequested.Publish();
        await RefreshConnectionBadgeAsync();
        await Task.Delay(200);
    }

    private async Task RefreshConnectionBadgeAsync()
    {
        var node = await ConfigHandler.GetDefaultServer(_config);
        if (node is null)
        {
            ActiveCountryFlag = "🌐";
            ActiveCountryName = "Нет сервера";
            ActiveSubscriptionName = "Ghost VPN";
            ActiveProtocol = "VLESS";
            return;
        }

        var remarks = (node.Remarks ?? string.Empty).Trim();
        var country = ResolveCountryName(remarks);

        ActiveCountryName = country;
        ActiveCountryFlag = ResolveCountryFlag(country);
        ActiveProtocol = node.ConfigType.ToString().ToUpperInvariant();

        var subName = "Ghost VPN";
        if (node.Subid.IsNotEmpty())
        {
            var subItem = await AppManager.Instance.GetSubItem(node.Subid);
            if (subItem?.Remarks.IsNotEmpty() == true)
            {
                subName = subItem.Remarks.Trim();
            }
        }
        ActiveSubscriptionName = subName;
    }

    private static string ResolveCountryName(string remarks)
    {
        if (remarks.IsNullOrEmpty())
        {
            return "Латвия";
        }

        var lower = remarks.ToLowerInvariant();
        if (lower.Contains("latvia") || lower.Contains("латв"))
        {
            return "Латвия";
        }
        if (lower.Contains("russia") || lower.Contains("росс"))
        {
            return "Россия";
        }
        if (lower.Contains("germany") || lower.Contains("герман"))
        {
            return "Германия";
        }
        if (lower.Contains("netherlands") || lower.Contains("нидерланд"))
        {
            return "Нидерланды";
        }

        var token = remarks
            .Split(['|', '-', ',', ';', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.IsNotEmpty());
        return token.IsNotEmpty() ? token : "Латвия";
    }

    private static string ResolveCountryFlag(string country)
    {
        var lower = country.ToLowerInvariant();
        if (lower.Contains("латв") || lower.Contains("latvia"))
        {
            return "🇱🇻";
        }
        if (lower.Contains("рос") || lower.Contains("russia"))
        {
            return "🇷🇺";
        }
        if (lower.Contains("герман") || lower.Contains("germany"))
        {
            return "🇩🇪";
        }
        if (lower.Contains("нидерланд") || lower.Contains("netherlands"))
        {
            return "🇳🇱";
        }

        return "🌐";
    }

    private void RefreshSubscriptions()
    {
        AppEvents.SubscriptionsRefreshRequested.Publish();
    }

    private async Task EnsureSingleServerModeAsync()
    {
        var profiles = await AppManager.Instance.ProfileItems("");
        if (profiles == null || profiles.Count <= 1)
        {
            return;
        }

        var active = await ConfigHandler.GetDefaultServer(_config) ?? profiles.First();
        if (active.IndexId.IsNotEmpty())
        {
            await ConfigHandler.SetDefaultServerIndex(_config, active.IndexId);
        }

        var removeItems = profiles
            .Where(t => t.IndexId != active.IndexId)
            .ToList();
        if (removeItems.Count <= 0)
        {
            return;
        }

        await ConfigHandler.RemoveServers(_config, removeItems);
        NoticeManager.Instance.Enqueue("Ghost VPN: оставлен только один активный сервер.");
    }

    private async Task SetLatestImportedServerAsDefaultAsync(HashSet<string> oldServerIds)
    {
        var profiles = await AppManager.Instance.ProfileItems("");
        if (profiles == null || profiles.Count <= 0)
        {
            return;
        }

        var imported = profiles.FirstOrDefault(t => !oldServerIds.Contains(t.IndexId)) ?? profiles.Last();
        if (imported.IndexId.IsNotEmpty())
        {
            await ConfigHandler.SetDefaultServerIndex(_config, imported.IndexId);
        }
    }

    private async Task<int> UpdateNewSubscriptionsAsync(HashSet<string> oldSubIds)
    {
        var subItems = await AppManager.Instance.SubItems() ?? [];
        var newSubIds = subItems
            .Where(t => t.Id.IsNotEmpty() && !oldSubIds.Contains(t.Id))
            .Select(t => t.Id)
            .Distinct()
            .ToList();

        foreach (var subId in newSubIds)
        {
            await UpdateSubscriptionProcess(subId, false);
        }

        return newSubIds.Count;
    }

    #endregion Servers && Groups

    #region Add Servers

    public async Task AddServerAsync(EConfigType eConfigType)
    {
        ProfileItem item = new()
        {
            Subid = _config.SubIndexId,
            ConfigType = eConfigType,
            IsSub = false,
        };

        bool? ret = false;
        if (eConfigType == EConfigType.Custom)
        {
            ret = await _updateView?.Invoke(EViewAction.AddServer2Window, item);
        }
        else if (eConfigType.IsGroupType())
        {
            ret = await _updateView?.Invoke(EViewAction.AddGroupServerWindow, item);
        }
        else
        {
            ret = await _updateView?.Invoke(EViewAction.AddServerWindow, item);
        }
        if (ret == true)
        {
            if (item.IndexId.IsNotEmpty())
            {
                await ConfigHandler.SetDefaultServerIndex(_config, item.IndexId);
            }
            if (!eConfigType.IsGroupType())
            {
                await EnsureSingleServerModeAsync();
            }
            await RefreshServers();
            if (item.IndexId == _config.IndexId)
            {
                await Reload();
            }
        }
    }

    public async Task AddServerViaClipboardAsync(string? clipboardData)
    {
        if (clipboardData == null)
        {
            await _updateView?.Invoke(EViewAction.AddServerViaClipboard, null);
            return;
        }

        var oldSubIds = (await AppManager.Instance.SubItems() ?? [])
            .Where(t => t.Id.IsNotEmpty())
            .Select(t => t.Id)
            .ToHashSet();
        var oldServerIds = (await AppManager.Instance.ProfileItems("") ?? [])
            .Select(t => t.IndexId)
            .ToHashSet();
        var ret = await ConfigHandler.AddBatchServers(_config, clipboardData, string.Empty, false);
        if (ret > 0)
        {
            _ = await UpdateNewSubscriptionsAsync(oldSubIds);
            await SetLatestImportedServerAsDefaultAsync(oldServerIds);
            await EnsureSingleServerModeAsync();
            RefreshSubscriptions();
            await RefreshServers();
            NoticeManager.Instance.Enqueue(string.Format(ResUI.SuccessfullyImportedServerViaClipboard, ret));
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
    }

    public async Task AddServerViaScanAsync()
    {
        _updateView?.Invoke(EViewAction.ScanScreenTask, null);
        await Task.CompletedTask;
    }

    public async Task ScanScreenResult(byte[]? bytes)
    {
        var result = QRCodeUtils.ParseBarcode(bytes);
        await AddScanResultAsync(result);
    }

    public async Task AddServerViaImageAsync()
    {
        _updateView?.Invoke(EViewAction.ScanImageTask, null);
        await Task.CompletedTask;
    }

    public async Task ScanImageResult(string fileName)
    {
        if (fileName.IsNullOrEmpty())
        {
            return;
        }

        var result = QRCodeUtils.ParseBarcode(fileName);
        await AddScanResultAsync(result);
    }

    private async Task AddScanResultAsync(string? result)
    {
        if (result.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.NoValidQRcodeFound);
        }
        else
        {
            var oldSubIds = (await AppManager.Instance.SubItems() ?? [])
                .Where(t => t.Id.IsNotEmpty())
                .Select(t => t.Id)
                .ToHashSet();
            var oldServerIds = (await AppManager.Instance.ProfileItems("") ?? [])
                .Select(t => t.IndexId)
                .ToHashSet();
            var ret = await ConfigHandler.AddBatchServers(_config, result, string.Empty, false);
            if (ret > 0)
            {
                _ = await UpdateNewSubscriptionsAsync(oldSubIds);
                await SetLatestImportedServerAsDefaultAsync(oldServerIds);
                await EnsureSingleServerModeAsync();
                RefreshSubscriptions();
                await RefreshServers();
                NoticeManager.Instance.Enqueue(ResUI.SuccessfullyImportedServerViaScan);
            }
            else
            {
                NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
            }
        }
    }

    #endregion Add Servers

    #region Subscription

    private async Task SubSettingAsync()
    {
        if (await _updateView?.Invoke(EViewAction.SubSettingWindow, null) == true)
        {
            RefreshSubscriptions();
        }
    }

    public async Task UpdateSubscriptionProcess(string subId, bool blProxy)
    {
        await Task.Run(async () => await SubscriptionHandler.UpdateProcess(_config, subId, blProxy, UpdateTaskHandler));
    }

    #endregion Subscription

    #region Setting

    private async Task OptionSettingAsync()
    {
        var ret = await _updateView?.Invoke(EViewAction.OptionSettingWindow, null);
        if (ret == true)
        {
            AppEvents.InboundDisplayRequested.Publish();
            await Reload();
        }
    }

    private async Task RoutingSettingAsync()
    {
        var ret = await _updateView?.Invoke(EViewAction.RoutingSettingWindow, null);
        if (ret == true)
        {
            await ConfigHandler.InitBuiltinRouting(_config);
            AppEvents.RoutingsMenuRefreshRequested.Publish();
            await Reload();
        }
    }

    private async Task DNSSettingAsync()
    {
        var ret = await _updateView?.Invoke(EViewAction.DNSSettingWindow, null);
        if (ret == true)
        {
            await Reload();
        }
    }

    private async Task FullConfigTemplateAsync()
    {
        var ret = await _updateView?.Invoke(EViewAction.FullConfigTemplateWindow, null);
        if (ret == true)
        {
            await Reload();
        }
    }

    private async Task ClearServerStatistics()
    {
        await StatisticsManager.Instance.ClearAllServerStatistics();
        await RefreshServers();
    }

    private async Task OpenTheFileLocation()
    {
        var path = Utils.StartupPath();
        if (Utils.IsWindows())
        {
            ProcUtils.ProcessStart(path);
        }
        else if (Utils.IsLinux())
        {
            ProcUtils.ProcessStart("xdg-open", path);
        }
        else if (Utils.IsMacOS())
        {
            ProcUtils.ProcessStart("open", path);
        }
        await Task.CompletedTask;
    }

    #endregion Setting

    #region core job

    private bool _hasNextReloadJob = false;
    private readonly SemaphoreSlim _reloadSemaphore = new(1, 1);

    public async Task Reload()
    {
        //If there are unfinished reload job, marked with next job.
        if (!await _reloadSemaphore.WaitAsync(0))
        {
            _hasNextReloadJob = true;
            return;
        }

        try
        {
            SetReloadEnabled(false);

            var msgs = await ActionPrecheckManager.Instance.Check(_config.IndexId);
            if (msgs.Count > 0)
            {
                foreach (var msg in msgs)
                {
                    NoticeManager.Instance.SendMessage(msg);
                }
                NoticeManager.Instance.Enqueue(Utils.List2String(msgs.Take(10).ToList(), true));
                return;
            }

            await Task.Run(async () =>
            {
                await LoadCore();
                await SysProxyHandler.UpdateSysProxy(_config, false);
                await Task.Delay(1000);
            });
            AppEvents.TestServerRequested.Publish();

            var showClashUI = AppManager.Instance.IsRunningCore(ECoreType.sing_box);
            if (showClashUI)
            {
                AppEvents.ProxiesReloadRequested.Publish();
            }

            ReloadResult(showClashUI);
            ApplyVpnState(_config.SystemProxyItem.SysProxyType == ESysProxyType.ForcedChange
                && CoreManager.Instance.IsCoreRunning());
        }
        finally
        {
            SetReloadEnabled(true);
            _reloadSemaphore.Release();
            //If there is a next reload job, execute it.
            if (_hasNextReloadJob)
            {
                _hasNextReloadJob = false;
                await Reload();
            }
        }
    }

    private void ReloadResult(bool showClashUI)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            ShowClashUI = showClashUI;
        });
    }

    private void SetReloadEnabled(bool enabled)
    {
        RxApp.MainThreadScheduler.Schedule(() => BlReloadEnabled = enabled);
    }

    private async Task LoadCore()
    {
        var node = await ConfigHandler.GetDefaultServer(_config);
        await CoreManager.Instance.LoadCore(node);
    }

    #endregion core job

    #region Presets

    public async Task ApplyRegionalPreset(EPresetType type)
    {
        await ConfigHandler.ApplyRegionalPreset(_config, type);
        await ConfigHandler.InitRouting(_config);
        AppEvents.RoutingsMenuRefreshRequested.Publish();

        await ConfigHandler.SaveConfig(_config);
        await new UpdateService(_config, UpdateTaskHandler).UpdateGeoFileAll();
        await Reload();
    }

    #endregion Presets
}
