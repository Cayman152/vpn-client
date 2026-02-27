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
                        if (Utils.IsMacOS())
                        {
                            Utils.ClearMacQuarantine(exe, false);
                        }
                    }
                }
            }

            if (Utils.IsMacOS())
            {
                Utils.ClearMacQuarantine(Utils.GetBinPath(""), true);
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
            // Fallback: try to find core in local bin tree even if folder layout differs.
            var localFile = GetLocalCoreExecFile(coreInfo);
            if (localFile.IsNotEmpty())
            {
                fileName = localFile;
                await UpdateFunc(false, $"{_tag} local fallback core path: {fileName}");
            }
        }
        if (fileName.IsNullOrEmpty())
        {
            // Fallback: in LocalAppData mode try running binaries directly from install folder.
            var installFile = GetInstalledCoreExecFile(coreInfo);
            if (installFile.IsNotEmpty())
            {
                fileName = installFile;
                await UpdateFunc(false, $"{_tag} fallback core path: {fileName}");
            }
        }
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
            await UpdateFunc(false, $"{_tag} failed core path: {fileName}");

            // Last-chance fallback for Windows LocalAppData mode: retry with install folder core file.
            var installFile = GetInstalledCoreExecFile(coreInfo);
            if (Environment.GetEnvironmentVariable(Global.LocalAppData) == "1"
                && installFile.IsNotEmpty()
                && !string.Equals(installFile, fileName, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await UpdateFunc(false, $"{_tag} retry core path: {installFile}");
                    return await RunProcessNormal(installFile, coreInfo, configPath, displayLog);
                }
                catch (Exception retryEx)
                {
                    Logging.SaveLog(_tag, retryEx);
                    await UpdateFunc(mayNeedSudo, retryEx.Message);
                    return null;
                }
            }

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
        if (!hasCore)
        {
            // In LocalAppData mode, try to recover missing core binaries from the install folder.
            if (!TrySyncInstalledBinToLocal(overwrite: false))
            {
                Logging.SaveLog($"{_tag} missing core binaries: {Utils.GetBinPath("", coreInfo.CoreType.ToString())}");
            }
            else
            {
                var restored = coreInfo.CoreExes
                    .Select(name => Utils.GetBinPath(Utils.GetExeName(name), coreInfo.CoreType.ToString()))
                    .Any(File.Exists);
                if (!restored)
                {
                    Logging.SaveLog($"{_tag} failed to restore core binaries: {Utils.GetBinPath("", coreInfo.CoreType.ToString())}");
                }
            }
        }

        EnsureGeoDataFiles(coreInfo);
    }

    private void EnsureGeoDataFiles(CoreInfo coreInfo)
    {
        if (coreInfo.CoreType is not (ECoreType.Xray or ECoreType.v2fly or ECoreType.v2fly_v5))
        {
            return;
        }

        var targetBinRoot = Utils.GetBinPath("");
        var coreDir = Utils.GetBinPath("", coreInfo.CoreType.ToString());
        var installCoreDir = Path.Combine(Utils.GetBaseDirectory("bin"), coreInfo.CoreType.ToString().ToLowerInvariant());
        var installRootBin = Utils.GetBaseDirectory("bin");
        var requiredGeoFiles = new[] { "geoip.dat", "geosite.dat" };

        foreach (var geoFile in requiredGeoFiles)
        {
            try
            {
                var targetFile = Path.Combine(targetBinRoot, geoFile);
                if (File.Exists(targetFile))
                {
                    continue;
                }

                var sourceCandidates = new[]
                {
                    Path.Combine(coreDir, geoFile),
                    Path.Combine(installCoreDir, geoFile),
                    Path.Combine(installRootBin, geoFile)
                };
                var sourceFile = sourceCandidates.FirstOrDefault(File.Exists);
                if (sourceFile.IsNullOrEmpty())
                {
                    Logging.SaveLog($"{_tag} missing geodata file: {geoFile}");
                    continue;
                }

                Directory.CreateDirectory(targetBinRoot);
                File.Copy(sourceFile, targetFile, true);
                Logging.SaveLog($"{_tag} restored geodata file: {geoFile}");
            }
            catch (Exception ex)
            {
                Logging.SaveLog(_tag, ex);
            }
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

    private static string GetInstalledCoreExecFile(CoreInfo? coreInfo)
    {
        if (coreInfo?.CoreExes == null)
        {
            return string.Empty;
        }

        var installCoreDir = Path.Combine(Utils.GetBaseDirectory("bin"), coreInfo.CoreType.ToString().ToLowerInvariant());
        if (Directory.Exists(installCoreDir))
        {
            foreach (var name in coreInfo.CoreExes)
            {
                var candidate = Path.Combine(installCoreDir, Utils.GetExeName(name));
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return FindCoreExecFile(Utils.GetBaseDirectory("bin"), coreInfo);
    }

    private static string GetLocalCoreExecFile(CoreInfo? coreInfo)
    {
        return FindCoreExecFile(Utils.GetBinPath(""), coreInfo);
    }

    private static string FindCoreExecFile(string binRoot, CoreInfo? coreInfo)
    {
        if (coreInfo?.CoreExes == null || !Directory.Exists(binRoot))
        {
            return string.Empty;
        }

        var exeNames = coreInfo.CoreExes
            .Select(Utils.GetExeName)
            .Where(name => name.IsNotEmpty())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (exeNames.Count == 0)
        {
            return string.Empty;
        }

        var coreTypeName = coreInfo.CoreType.ToString();
        var candidateDirs = new[]
        {
            Path.Combine(binRoot, coreTypeName.ToLowerInvariant()),
            Path.Combine(binRoot, coreTypeName),
            Path.Combine(binRoot, coreTypeName.Replace("_", "-")),
            binRoot
        }.Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in candidateDirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            foreach (var exeName in exeNames)
            {
                var candidate = Path.Combine(dir, exeName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(binRoot, "*", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileName(file);
                if (exeNames.Any(exe => string.Equals(exe, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    return file;
                }
            }
        }
        catch (Exception ex)
        {
            Logging.SaveLog(_tag, ex);
        }

        return string.Empty;
    }

    private async Task<ProcessService?> RunProcessNormal(string fileName, CoreInfo? coreInfo, string configPath, bool displayLog)
    {
        if (Utils.IsMacOS())
        {
            Utils.SetUnixFileMode(fileName);
            Utils.ClearMacQuarantine(fileName, false);
            var coreDir = Path.GetDirectoryName(fileName);
            if (coreDir.IsNotEmpty())
            {
                Utils.ClearMacQuarantine(coreDir, true);
            }
            Utils.ClearMacQuarantine(Utils.GetBinPath(""), true);
        }

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
