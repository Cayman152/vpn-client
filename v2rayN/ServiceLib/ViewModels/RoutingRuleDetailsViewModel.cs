namespace ServiceLib.ViewModels;

public class RoutingRuleDetailsViewModel : MyReactiveObject
{
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

    [Reactive]
    public bool AutoSort { get; set; }

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

        Domain = Utils.List2String(SelectedSource.Domain, true);
        IP = Utils.List2String(SelectedSource.Ip, true);
        Process = Utils.List2String(SelectedSource.Process, true);
        RuleType = SelectedSource.RuleType?.ToString();
    }

    private async Task SaveRulesAsync()
    {
        Domain = Utils.Convert2Comma(Domain);
        IP = Utils.Convert2Comma(IP);
        Process = Utils.Convert2Comma(Process);

        if (AutoSort)
        {
            SelectedSource.Domain = Utils.String2ListSorted(Domain);
            SelectedSource.Ip = Utils.String2ListSorted(IP);
            SelectedSource.Process = Utils.String2ListSorted(Process);
        }
        else
        {
            SelectedSource.Domain = Utils.String2List(Domain);
            SelectedSource.Ip = Utils.String2List(IP);
            SelectedSource.Process = Utils.String2List(Process);
        }
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
}
