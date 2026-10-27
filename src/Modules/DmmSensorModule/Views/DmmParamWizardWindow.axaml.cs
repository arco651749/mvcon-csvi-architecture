using Avalonia.Controls;
using Avalonia.Interactivity;
using DmmSensorModule.Models;

namespace DmmSensorModule.Views;

public partial class DmmParamWizardWindow : Window
{
    public VoltCommandParam ResultParam { get; private set; }

    public DmmParamWizardWindow()
    {
        InitializeComponent();
        ResultParam = new VoltCommandParam();
        DataContext = ResultParam;
    }

    public DmmParamWizardWindow(VoltCommandParam initialParam)
    {
        InitializeComponent();
        // 建立編輯副本，避免使用者按取消時污染原資料
        ResultParam = new VoltCommandParam
        {
            Channel = initialParam.Channel,
            ScaleFactor = initialParam.ScaleFactor,
            HighLimit = initialParam.HighLimit
        };
        DataContext = ResultParam;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
