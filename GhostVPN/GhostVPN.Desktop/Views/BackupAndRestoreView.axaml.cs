using GhostVPN.Desktop.Common;

namespace GhostVPN.Desktop.Views;

public partial class BackupAndRestoreView : ReactiveUserControl<BackupAndRestoreViewModel>
{
    private Window? _window;

    public BackupAndRestoreView()
    {
        InitializeComponent();
    }

    public BackupAndRestoreView(Window window)
    {
        _window = window;

        InitializeComponent();
        menuLocalBackup.Click += MenuLocalBackup_Click;
        menuLocalRestore.Click += MenuLocalRestore_Click;

        ViewModel = new BackupAndRestoreViewModel(UpdateViewHandler);

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.OperationMsg, v => v.txtMsg.Text).DisposeWith(disposables);
        });
    }

    private async void MenuLocalBackup_Click(object? sender, RoutedEventArgs e)
    {
        var fileName = await UI.SaveFileDialog(_window, "Zip|*.zip");
        if (fileName.IsNullOrEmpty())
        {
            return;
        }

        ViewModel?.LocalBackup(fileName);
    }

    private async void MenuLocalRestore_Click(object? sender, RoutedEventArgs e)
    {
        var fileName = await UI.OpenFileDialog(_window, null);
        if (fileName.IsNullOrEmpty())
        {
            return;
        }

        ViewModel?.LocalRestore(fileName);
    }

    private async Task<bool> UpdateViewHandler(EViewAction action, object? obj)
    {
        return await Task.FromResult(true);
    }
}
