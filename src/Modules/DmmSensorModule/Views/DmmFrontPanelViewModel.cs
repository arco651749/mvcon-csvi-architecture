using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DmmSensorModule.Views;

public partial class DmmFrontPanelViewModel : ObservableObject
{
    [ObservableProperty] private string _displayVoltageText = "--.--- V";
    [ObservableProperty] private string _activeChannelText = "CH: ai0";
    [ObservableProperty] private string _statusMessage = "待機中 (Standby)";
    [ObservableProperty] private IBrush _displayColor = Brushes.LightGreen;
    [ObservableProperty] private IBrush _statusLedColor = Brushes.Gray;

    public void UpdateReading(double voltage, string channel, double highLimit)
    {
        DisplayVoltageText = $"{voltage:F3} V";
        ActiveChannelText = $"CH: {channel}";

        if (voltage > highLimit)
        {
            StatusMessage = $"[警告] 讀值超過上限 ({highLimit:F2}V)！";
            DisplayColor = Brushes.OrangeRed;
            StatusLedColor = Brushes.Red;
        }
        else
        {
            StatusMessage = "正常量測中 (Sampling OK)";
            DisplayColor = Brushes.LightGreen;
            StatusLedColor = Brushes.Lime;
        }
    }

    public void NotifyBusy(string channel)
    {
        ActiveChannelText = $"CH: {channel}";
        StatusMessage = "底層採樣執行中...";
        StatusLedColor = Brushes.Yellow;
    }
}
