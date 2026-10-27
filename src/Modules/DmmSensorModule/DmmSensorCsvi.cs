using System.Text.Json;
using Avalonia.Controls;
using Mvcon.Contracts;
using DmmSensorModule.Core;
using DmmSensorModule.Models;
using DmmSensorModule.Views;

namespace DmmSensorModule;

/// <summary>
/// DMM 模組對外入口 (將會編譯為 DmmSensorModule.csvi)
/// 實作 MVCON 指令規範：模組@指令::參數
/// </summary>
public class DmmSensorCsvi : ICsviModule
{
    public string ModuleName => "DMM";
    public string DisplayName => "數位萬用表 (Digital Multimeter)";
    public string Description => "支援通道電壓量測 (MEAS_VOLT) 與裝置重置 (RESET)";

    public IReadOnlyList<string> SupportedCommands { get; } = new[]
    {
        "MEAS_VOLT",
        "RESET"
    };

    // UI 2: Front Panel 實體與 ViewModel (懶加載)
    private readonly DmmFrontPanelViewModel _frontPanelVm = new();
    private Control? _frontPanelControl;

    // =========================================================================
    // UI 1: 參數導引視窗 (針對指令編輯 ::參數)
    // =========================================================================
    public async Task<string?> EditParameterAsync(string command, string currentParamString, Window? owner = null)
    {
        switch (command.ToUpperInvariant())
        {
            case "MEAS_VOLT":
            {
                // 解析既有參數，若沒有則使用預設值
                VoltCommandParam param;
                try
                {
                    param = string.IsNullOrWhiteSpace(currentParamString)
                        ? new VoltCommandParam()
                        : JsonSerializer.Deserialize<VoltCommandParam>(currentParamString) ?? new VoltCommandParam();
                }
                catch
                {
                    param = new VoltCommandParam();
                }

                // 彈出參數導引精靈視窗
                var wizard = new DmmParamWizardWindow(param);
                var isOk = owner != null ? await wizard.ShowDialog<bool>(owner) : false;

                if (isOk)
                {
                    // 使用者點確定，序列化為 JSON 回傳，作為新的 ::參數
                    return JsonSerializer.Serialize(wizard.ResultParam);
                }
                return null; // 使用者按取消
            }

            case "RESET":
                // RESET 不需要參數
                return "";

            default:
                throw new NotSupportedException($"DMM 不支援此指令參數編輯: {command}");
        }
    }

    // =========================================================================
    // UI 2: 運行即時 Front Panel (供主畫面 SubPanel 嵌入)
    // =========================================================================
    public Control GetFrontPanelControl()
    {
        // 懶加載 View 並綁定 ViewModel
        return _frontPanelControl ??= new DmmFrontPanelView
        {
            DataContext = _frontPanelVm
        };
    }

    // =========================================================================
    // 底層純執行函數 (無 UI 依賴，SubVI 模式)
    // =========================================================================
    public async Task<object?> ExecuteCommandAsync(string command, string paramString, CancellationToken ct = default)
    {
        switch (command.ToUpperInvariant())
        {
            case "MEAS_VOLT":
            {
                VoltCommandParam param;
                try
                {
                    param = string.IsNullOrWhiteSpace(paramString)
                        ? new VoltCommandParam()
                        : JsonSerializer.Deserialize<VoltCommandParam>(paramString) ?? new VoltCommandParam();
                }
                catch
                {
                    param = new VoltCommandParam();
                }

                _frontPanelVm.NotifyBusy(param.Channel);

                // 呼叫純底層函數 (零 UI 開銷)
                double reading = await DmmCoreEngine.ReadVoltageAsync(param, ct);

                // 即時推播數值給 Front Panel (若有被 SubPanel 嵌入，畫面會即時反應)
                _frontPanelVm.UpdateReading(reading, param.Channel, param.HighLimit);

                return reading;
            }

            case "RESET":
            {
                await DmmCoreEngine.ResetDeviceAsync(ct);
                return "Device Reset OK";
            }

            default:
                throw new NotSupportedException($"DMM 不支援此指令: {command}");
        }
    }
}
