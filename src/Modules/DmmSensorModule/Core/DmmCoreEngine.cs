using DmmSensorModule.Models;

namespace DmmSensorModule.Core;

/// <summary>
/// DMM 純底層函數庫 (完全不依賴任何 Avalonia UI 命名空間)
/// 負責實際通訊、硬體採樣或演算法處理
/// </summary>
public static class DmmCoreEngine
{
    public static async Task<double> ReadVoltageAsync(VoltCommandParam param, CancellationToken ct = default)
    {
        // 模擬採樣延遲 (例如 150ms)
        await Task.Delay(150, ct);

        // 模擬感測器讀值 (0.5V ~ 4.5V)
        double rawReading = (Random.Shared.NextDouble() * 4.0) + 0.5;

        // 套用放大倍率
        double finalReading = rawReading * param.ScaleFactor;
        return finalReading;
    }

    public static async Task ResetDeviceAsync(CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        // 模擬硬體重置
    }
}
