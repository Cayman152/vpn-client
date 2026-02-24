using v2rayN.Desktop.Base;

namespace v2rayN.Desktop.Views;

public partial class RoutingRuleDetailsWindow : WindowBase<RoutingRuleDetailsViewModel>
{
    public RoutingRuleDetailsWindow()
    {
        InitializeComponent();
    }

    public RoutingRuleDetailsWindow(RulesItem rulesItem)
    {
        InitializeComponent();

        Loaded += Window_Loaded;
        btnCancel.Click += (s, e) => Close();

        ViewModel = new RoutingRuleDetailsViewModel(rulesItem, UpdateViewHandler);

        cmbOutboundTag.ItemsSource = Global.OutboundTags;
        cmbRuleType.ItemsSource = Utils.GetEnumNames<ERuleType>().AppendEmpty();

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.SelectedSource.OutboundTag, v => v.cmbOutboundTag.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.OutboundTag, v => v.cmbOutboundTag.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Domain, v => v.txtDomain.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.IP, v => v.txtIP.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Process, v => v.txtProcess.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AutoSort, v => v.chkAutoSort.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RuleType, v => v.cmbRuleType.SelectedValue).DisposeWith(disposables);

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
        }
        return await Task.FromResult(true);
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        txtRemarks.Focus();
    }
}
