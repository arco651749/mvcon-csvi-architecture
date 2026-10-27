namespace DmmSensorModule.Models;

/// <summary>
/// 對應指令 DMM@MEAS_VOLT 的參數結構
/// 格式將被序列化為 JSON 放在 ::參數
/// </summary>
public class VoltCommandParam
{
    public string Channel { get; set; } = "ai0";
    public double ScaleFactor { get; set; } = 1.0;
    public double HighLimit { get; set; } = 5.0;
}
