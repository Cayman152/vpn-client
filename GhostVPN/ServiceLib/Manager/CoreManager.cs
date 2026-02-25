namespace ServiceLib.Manager;

/// <summary>
/// Core process processing class
/// </summary>
public class CoreManager
{
    private static readonly Lazy<CoreManager> _instance = new(() => new());
    public static CoreManager Instance => _instance.Value;
    private Config _config;
    private WindowsJobService? _processJob;
    private ProcessService? _processService;
    private ProcessService? _processPreService;
    private bool _linuxSudo = false;
    private Func<bool, string, Task>? _updateFunc;
    private const string _tag = "CoreHandler";

    public async Task Init(Config config, Func<bool, string, Task> updateFunc)
    {
        _config = config;
        _updateFunc = updateFunc;

        //Copy the bin folder to the storage location (for init)
        if (Environment.GetEnvironmentVariable(Global.LocalAppData) == "1")
        {
            TrySyncInstalledBinToLocal(overwrite: true);
        }

        if (Utils.IsNonWindows())
        {
            var coreInfo = CoreInfoManager.Instance.GetCoreInfo();
            foreach (var it in coreInfo)
            {
                if (it.CoreType == ECoreType.GhostVPN)
                {
                    if (Utils.UpgradeAppExists(out var upgradeFileName))
                    {
                        await Utils.SetLinuxChmod(upgradeFileName);
                    }
                    continue;
                }

                foreach (var name in it.CoreExes)
                {
                    var exe = Utils.GetBinPath(Utils.GetExeName(name), it.CoreType.ToString());
                    if (File.Exists(exe))
                    {
                        await Utils.SetLinuxChmod(exe);
                    }
                }
            }
        }
    }

    public async Task LoadCore(ProfileItem? node)
    {
        if (node == null)
        {
            await UpdateFunc(false, ResUI.CheckServerSettings);
            return;
        }

        var fileName = Utils.GetBinConfigPath(Global.CoreConfigFileName);
        var result = await CoreConfigHandler.GenerateClientConfig(node, fileName);
        if (result.Success != true)
        {
            await UpdateFunc(true, result.Msg);
            return;
        }

        await UpdateFunc(false, $"{node.GetSummary()}");
        await UpdateFunc(false, $"{Utils.GetRuntimeInfo()}");
        await UpdateFunc(false, string.Format(ResUI.StartService, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
        await CoreStop();
        await Task.Delay(100);

        if (Utils.IsWindows() && _config.TunModeItem.EnableTun)
        {
            await Task.Delay(100);
            await WindowsUtils.RemoveTunDevice();
        }

        await CoreStart(node);
        await CoreStartPreService(node);
        if (_processService != null)
        {
            await UpdateFunc(true, $"{node.GetSummary()}");
        }
    }

    public async Task<ProcessService?> LoadCoreConfigSpeedtest(List<ServerTestItem> selecteds)
    {
        var coreType = selecteds.Any(t => Global.SingboxOnlyConfigType.Contains(t.ConfigType)) ? ECoreType.sing_box : ECoreType.Xray;
        var fileName = string.Format(Global.CoreSpeedtestConfigFileName, Utils.GetGuid(false));
        var configPath = Utils.GetBinConfigPath(fileName);
        var result = await CoreConfigHandler.GenerateClientSpeedtestConfig(_config, configPath, selecteds, coreType);
        await UpdateFunc(false, result.Msg);
        if (result.Success != true)
        {
            return null;
        }

        await UpdateFunc(false, string.Format(ResUI.StartService, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")));
        await UpdateFunc(false, configPath);

        var coreInfo = CoreInfoManager.Instance.GetCoreInfo(coreType);
        return await RunProcess(coreInfo, fileName, true, false);
    }

    public async Task<ProcessService?> LoadCoreConfigSpeedtest(ServerTestItem testItem)
    {
        var node = await AppManager.Instance.GetProfileItem(testItem.IndexId);
        if (node is null)
        {
            return null;
        }

        var fileName = string.Format(Global.CoreSpeedtestConfigFileName, Utils.GetGuid(false));
        var configPath = Utils.GetBinConfigPath(fileName);
        var result = await CoreConfigHandler.GenerateClientSpeedtestConfig(_config, node, testItem, configPath);
        if (result.Success != true)
        {
            return null;
        }

        var coreType = AppManager.Instance.GetCoreType(node, node.ConfigType);
        var coreInfo = CoreInfoManager.Instance.GetCoreInfo(coreType);
        return await RunProcess(coreInfo, fileName, true, false);
    }

    public async Task CoreStop()
    {
        try
        {
            if (_linuxSudo)
            {
                await CoreAdminManager.Instance.KillProcessAsLinuxSudo();
                _linuxSudo = false;
            }

            if (_processService != null)
            {
                await _processService.StopAsync();
                _processService.Dispose();
                _processService = null;
            }

            if (_processPreService != null)
            {
                await _processPreService.StopAsync();
                _processPreService.Dispose();
                _processPreService = null;
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }
    }

    public bool IsCoreRunning()
    {
        return _processService != null && !_processService.HasExited;
    }

    #region Private

    private async Task CoreStart(ProfileItem node)
    {
        var coreType = AppManager.Instance.RunningCoreType = AppManager.Instance.GetCoreType(node, node.ConfigType);
        var coreInfo = CoreInfoManager.Instance.GetCoreInfo(coreType);

        var displayLog = node.ConfigType != EConfigType.Custom || node.DisplayLog;
        var proc = await RunProcess(coreInfo, Global.CoreConfigFileName, displayLog, true);
        if (proc is null)
        {
            return;
        }
        _processService = proc;
    }

    private async Task CoreStartPreService(ProfileItem node)
    {
        if (_processService != null && !_processService.HasExited)
        {
            var coreType = AppManager.Instance.GetCoreType(node, node.ConfigType);
            var itemSocks = await ConfigHandler.GetPreSocksItem(_config, node, coreType);
            if (itemSocks != null)
            {
                var preCoreType = itemSocks.CoreType ?? ECoreType.sing_box;
                var fileName = Utils.GetBinConfigPath(Global.CorePreConfigFileName);
                var result = await CoreConfigHandler.GenerateClientConfig(itemSocks, fileName);
                if (result.Success)
                {
                    var coreInfo = CoreInfoManager.Instance.GetCoreInfo(preCoreType);
                    var proc = await RunProcess(coreInfo, Global.CorePreConfigFileName, true, true);
                    if (proc is null)
                    {
                        return;
                    }
                    _processPreService = proc;
                }
            }
        }
    }

    private async Task UpdateFunc(bool notify, string msg)
    {
        await _updateFunc?.Invoke(notify, msg);
    }

    #endregion Private

    #region Process

    private async Task<ProcessService?> RunProcess(CoreInfo? coreInfo, string configPath, bool displayLog, bool mayNeedSudo)
    {
        EnsureCoreBinaries(coreInfo);

        var fileName = CoreInfoManager.Instance.GetCoreExecFile(coreInfo, out var msg);
        if (fileName.IsNullOrEmpty())
        {
            await UpdateFunc(false, msg);
            return null;
        }

        try
        {
            if (mayNeedSudo
                && _config.TunModeItem.EnableTun
                && (coreInfo.CoreType is ECoreType.sing_box or ECoreType.mihomo)
                && Utils.IsNonWindows())
            {
                _linuxSudo = true;
                await CoreAdminManager.Instance.Init(_config, _updateFunc);
                return await CoreAdminManager.Instance.RunProcessAsLinuxSudo(fileName, coreInfo, configPath);
            }

            return await RunProcessNormal(fileName, coreInfo, configPath, displayLog);
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            await UpdateFunc(mayNeedSudo, ex.Message);
            return null;
        }
    }

    private void EnsureCoreBinaries(CoreInfo? coreInfo)
    {
        if (coreInfo?.CoreExes == null || coreInfo.CoreType == ECoreType.GhostVPN)
        {
            return;
        }

        // In LocalAppData mode always refresh core binaries from install dir to avoid stale/corrupted copies.
        if (Environment.GetEnvironmentVariable(Global.LocalAppData) == "1")
        {
            _ = TrySyncInstalledBinToLocal(overwrite: true);
        }

        var hasCore = coreInfo.CoreExes
            .Select(name => Utils.GetBinPath(Utils.GetExeName(name), coreInfo.CoreType.ToString()))
            .Any(File.Exists);
        if (hasCore)
        {
            return;
        }

        // In LocalAppData mode, try to recover missing core binaries from the install folder.
        if (!TrySyncInstalledBinToLocal(overwrite: false))
        {
            Logging.SaveLog($"{_tag} missing core binaries: {Utils.GetBinPath("", coreInfo.CoreType.ToString())}");
            return;
        }

        var restored = coreInfo.CoreExes
            .Select(name => Utils.GetBinPath(Utils.GetExeName(name), coreInfo.CoreType.ToString()))
            .Any(File.Exists);
        if (!restored)
        {
            Logging.SaveLog($"{_tag} failed to restore core binaries: {Utils.GetBinPath("", coreInfo.CoreType.ToString())}");
        }
    }

    private bool TrySyncInstalledBinToLocal(bool overwrite)
    {
        if (Environment.GetEnvironmentVariable(Global.LocalAppData) != "1")
        {
            return false;
        }

        var fromPath = Utils.GetBaseDirectory("bin");
        var toPath = Utils.GetBinPath("");
        if (fromPath == toPath)
        {
            return false;
        }
        if (!Directory.Exists(fromPath))
        {
            Logging.SaveLog($"{_tag} install bin directory not found: {fromPath}");
            return false;
        }

        try
        {
            FileUtils.CopyDirectory(fromPath, toPath, true, overwrite);
            return true;
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
            return false;
        }
    }

    private async Task<ProcessService?> RunProcessNormal(string fileName, CoreInfo? coreInfo, string configPath, bool displayLog)
    {
        var environmentVars = new Dictionary<string, string>();
        foreach (var kv in coreInfo.Environment)
        {
            environmentVars[kv.Key] = string.Format(kv.Value, coreInfo.AbsolutePath ? Utils.GetBinConfigPath(configPath).AppendQuotes() : configPath);
        }

        var procService = new ProcessService(
            fileName: fileName,
            arguments: string.Format(coreInfo.Arguments, coreInfo.AbsolutePath ? Utils.GetBinConfigPath(configPath).AppendQuotes() : configPath),
            workingDirectory: Utils.GetBinConfigPath(),
            displayLog: displayLog,
            redirectInput: false,
            environmentVars: environmentVars,
            updateFunc: _updateFunc
        );

        await procService.StartAsync();

        await Task.Delay(100);

        if (procService is null or { HasExited: true })
        {
            throw new Exception(ResUI.FailedToRunCore);
        }
        AddProcessJob(procService.Handle);

        return procService;
    }

    private void AddProcessJob(nint processHandle)
    {
        if (Utils.IsWindows())
        {
            _processJob ??= new();
            try
            {
                _processJob?.AddProcess(processHandle);
            }
            catch { }
        }
    }

    #endregion Process
}
