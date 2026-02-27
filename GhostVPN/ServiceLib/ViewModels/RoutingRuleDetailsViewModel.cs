namespace ServiceLib.ViewModels;

public class RoutingRuleDetailsViewModel : MyReactiveObject
{
    private static readonly string[] _domainExpressionPrefixes =
    [
        "domain:",
        "full:",
        "keyword:",
        "regexp:",
        "geosite:",
        "ext:",
        "ext-domain:"
    ];

    [Reactive]
    public RulesItem SelectedSource { get; set; }

    [Reactive]
    public string Domain { get; set; }

    [Reactive]
    public string IP { get; set; }

    [Reactive]
    public string Process { get; set; }

    [Reactive]
    public string? RuleType { get; set; }

    public ReactiveCommand<Unit, Unit> SaveCmd { get; }

    public RoutingRuleDetailsViewModel(RulesItem rulesItem, Func<EViewAction, object?, Task<bool>>? updateView)
    {
        _config = AppManager.Instance.Config;
        _updateView = updateView;

        SaveCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveRulesAsync();
        });

        if (rulesItem.Id.IsNullOrEmpty())
        {
            rulesItem.Id = Utils.GetGuid(false);
            rulesItem.OutboundTag = Global.ProxyTag;
            rulesItem.Enabled = true;
            SelectedSource = rulesItem;
        }
        else
        {
            SelectedSource = rulesItem;
        }

        // The simplified editor does not expose advanced matchers.
        SelectedSource.Enabled = true;
        SelectedSource.Port = null;
        SelectedSource.Network = null;
        SelectedSource.Protocol = null;
        SelectedSource.InboundTag = null;

        Domain = ToEditorDomainText(SelectedSource.Domain);
        IP = Utils.List2String(SelectedSource.Ip, true);
        Process = Utils.List2String(SelectedSource.Process, true);
        RuleType = SelectedSource.RuleType?.ToString();
    }

    private async Task SaveRulesAsync()
    {
        Domain = Utils.Convert2Comma(Domain);
        IP = Utils.Convert2Comma(IP);
        Process = Utils.Convert2Comma(Process);

        SelectedSource.Domain = ParseEditorDomainText(Domain);
        SelectedSource.Ip = Utils.String2List(IP);
        SelectedSource.Process = Utils.String2List(Process);
        SelectedSource.Enabled = true;
        SelectedSource.Port = null;
        SelectedSource.Network = null;
        SelectedSource.Protocol = null;
        SelectedSource.InboundTag = null;
        SelectedSource.RuleType = RuleType.IsNullOrEmpty() ? null : (ERuleType)Enum.Parse(typeof(ERuleType), RuleType);

        var hasRule = SelectedSource.Domain?.Count > 0
          || SelectedSource.Ip?.Count > 0
          || SelectedSource.Process?.Count > 0;

        if (!hasRule)
        {
            NoticeManager.Instance.Enqueue(string.Format(ResUI.RoutingRuleDetailRequiredTips, "Domain/IP/Process"));
            return;
        }
        //NoticeHandler.Instance.Enqueue(ResUI.OperationSuccess);
        await _updateView?.Invoke(EViewAction.CloseWindow, null);
    }

    private static string ToEditorDomainText(List<string>? domains)
    {
        if (domains == null || domains.Count == 0)
        {
            return string.Empty;
        }

        var formatted = domains
            .Select(StripDomainPrefixForEditor)
            .ToList();
        return Utils.List2String(formatted, true);
    }

    private static string StripDomainPrefixForEditor(string domain)
    {
        if (domain.IsNullOrEmpty())
        {
            return domain;
        }

        return domain.StartsWith("domain:", StringComparison.OrdinalIgnoreCase)
            ? domain.Substring("domain:".Length)
            : domain;
    }

    private static List<string>? ParseEditorDomainText(string? rawDomainText)
    {
        var domains = Utils.String2List(rawDomainText);
        if (domains == null || domains.Count == 0)
        {
            return null;
        }

        var normalized = domains
            .Select(AddPrefixForPlainDomain)
            .Where(d => !d.IsNullOrEmpty())
            .ToList();

        return normalized.Count == 0 ? null : normalized;
    }

    private static string AddPrefixForPlainDomain(string domain)
    {
        if (domain.IsNullOrEmpty())
        {
            return domain;
        }

        var value = domain.Trim();
        if (value.IsNullOrEmpty())
        {
            return string.Empty;
        }

        if (value.StartsWith('#') || _domainExpressionPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return value;
        }

        return Utils.IsDomain(value) ? $"domain:{value}" : value;
    }
}
