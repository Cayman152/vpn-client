using System.IO;
using GhostVPN.Desktop.Base;
using GhostVPN.Desktop.Common;

namespace GhostVPN.Desktop.Views;

public partial class RoutingRuleSettingWindow : WindowBase<RoutingRuleSettingViewModel>
{
    public RoutingRuleSettingWindow()
    {
        InitializeComponent();
    }

    public RoutingRuleSettingWindow(RoutingItem routingItem)
    {
        InitializeComponent();

        Loaded += Window_Loaded;
        btnCancel.Click += (s, e) => Close();
        btnRuleAdd.Click += btnRuleAdd_Click;
        KeyDown += RoutingRuleSettingWindow_KeyDown;
        lstRules.SelectionChanged += lstRules_SelectionChanged;
        lstRules.DoubleTapped += LstRules_DoubleTapped;
        menuRuleSelectAll.Click += menuRuleSelectAll_Click;

        ViewModel = new RoutingRuleSettingViewModel(routingItem, UpdateViewHandler);

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.RulesItems, v => v.lstRules.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource, v => v.lstRules.SelectedItem).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ImportRulesFromFileCmd, v => v.menuImportRulesFromFile).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ExportRulesToJsonCmd, v => v.menuExportRulesToJson).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddDirectCinemaPresetCmd, v => v.menuDirectPresetCinema).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddDirectBanksPresetCmd, v => v.menuDirectPresetBanks).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddDirectProvidersPresetCmd, v => v.menuDirectPresetProviders).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddDirectGtaVPresetCmd, v => v.menuDirectPresetGtaV).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddProxyDiscordPresetCmd, v => v.menuProxyPresetDiscord).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RuleRemoveCmd, v => v.menuRuleRemove).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RuleExportSelectedCmd, v => v.menuRuleExportSelected).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveTopCmd, v => v.menuMoveTop).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveUpCmd, v => v.menuMoveUp).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveDownCmd, v => v.menuMoveDown).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.MoveBottomCmd, v => v.menuMoveBottom).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);
        });
    }

    private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        switch (action)
        {
            case EViewAction.CloseWindow:
                Close(true);
                break;

            case EViewAction.ShowYesNo:
                if (await UI.ShowYesNo(this, ResUI.RemoveServer) != ButtonResult.Yes)
                {
                    return false;
                }
                break;

            case EViewAction.AddBatchRoutingRulesYesNo:
                if (await UI.ShowYesNo(this, ResUI.AddBatchRoutingRulesYesNo) != ButtonResult.Yes)
                {
                    return false;
                }
                break;

            case EViewAction.RoutingRuleDetailsWindow:
                if (obj is null)
                {
                    return false;
                }

                return await new RoutingRuleDetailsWindow((RulesItem)obj).ShowDialog<bool>(this);

            case EViewAction.ImportRulesFromFile:
                var fileName = await UI.OpenFileDialog(this, null);
                if (fileName.IsNullOrEmpty())
                {
                    return false;
                }
                ViewModel?.ImportRulesFromFileAsync(fileName);
                break;

            case EViewAction.ExportRoutingRulesToFile:
                if (obj is not string json)
                {
                    return false;
                }

                var saveFileName = await UI.SaveFileDialog(this, "routing-rules.json");
                if (saveFileName.IsNullOrEmpty())
                {
                    return false;
                }

                if (Path.GetExtension(saveFileName).IsNullOrEmpty())
                {
                    saveFileName += ".json";
                }
                File.WriteAllText(saveFileName, json);
                break;

            case EViewAction.SetClipboardData:
                if (obj is null)
                {
                    return false;
                }

                await AvaUtils.SetClipboardData(this, (string)obj);
                break;

            case EViewAction.ImportRulesFromClipboard:
                var clipboardData = await AvaUtils.GetClipboardData(this);
                if (clipboardData.IsNotEmpty())
                {
                    ViewModel?.ImportRulesFromClipboardAsync(clipboardData);
                }

                break;
        }

        return await Task.FromResult(true);
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        lstRules.Focus();
    }

    private void RoutingRuleSettingWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta)
        {
            if (e.Key == Key.A)
            {
                lstRules.SelectAll();
            }
            else if (e.Key == Key.C)
            {
                ViewModel?.RuleExportSelectedAsync();
            }
        }
        else
        {
            switch (e.Key)
            {
                case Key.T:
                    ViewModel?.MoveRule(EMove.Top);
                    break;

                case Key.U:
                    ViewModel?.MoveRule(EMove.Up);
                    break;

                case Key.D:
                    ViewModel?.MoveRule(EMove.Down);
                    break;

                case Key.B:
                    ViewModel?.MoveRule(EMove.Bottom);
                    break;

                case Key.Delete:
                case Key.Back:
                    ViewModel?.RuleRemoveAsync();
                    break;
            }
        }
    }

    private void lstRules_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.SelectedSources = lstRules.SelectedItems.Cast<RulesItemModel>().ToList();
        }
    }

    private void LstRules_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        ViewModel?.RuleEditAsync(false);
    }

    private void menuRuleSelectAll_Click(object? sender, RoutedEventArgs e)
    {
        lstRules.SelectAll();
    }

    private async void btnRuleAdd_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        await ViewModel.RuleEditAsync(true);
    }

}
