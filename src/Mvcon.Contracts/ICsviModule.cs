using Avalonia.Controls;

namespace Mvcon.Contracts;

/// <summary>
/// MVCON 核心模組合約介面
/// 語法格式：模組@指令::參數
/// </summary>
public interface ICsviModule
{
    /// <summary>
    /// 模組識別代號 (對應指令開頭：模組@)
    /// 例如 "DMM", "CAM", "MOTION"
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// 模組中文顯示名稱
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 模組描述說明
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 該模組支援的所有指令清單 (對應 @指令)
    /// </summary>
    IReadOnlyList<string> SupportedCommands { get; }

    // =========================================================================
    // UI 1: 參數導引視窗 (專門編輯 ::參數)
    // =========================================================================
    /// <summary>
    /// 開啟參數設定導引 UI。
    /// 當使用者在腳本編輯器雙擊或點擊「編輯參數」時調用，
    /// 彈出專屬視窗輔助使用者設定數值，點確定後回傳序列化字串。
    /// </summary>
    /// <param name="command">當前指令 (例如 MEAS_VOLT)</param>
    /// <param name="currentParamString">目前既有的參數內容 (可為空)</param>
    /// <param name="owner">父視窗 (提供 Modal Dialog 依附)</param>
    /// <returns>確定時回傳新產生的參數字串 (如 JSON)，取消時回傳 null</returns>
    Task<string?> EditParameterAsync(string command, string currentParamString, Window? owner = null);

    // =========================================================================
    // UI 2: 運行即時 Front Panel (供主畫面 SubPanel 嵌入)
    // =========================================================================
    /// <summary>
    /// 取得該模組的前面板控制項 (UserControl)，供主程式掛載至 SubPanel
    /// </summary>
    Control GetFrontPanelControl();

    // =========================================================================
    // 底層純執行函數 (無 UI 依賴，SubVI 模式)
    // =========================================================================
    /// <summary>
    /// 執行指定指令與參數 (全速執行，背景模式)
    /// </summary>
    /// <param name="command">指令名稱 (如 MEAS_VOLT)</param>
    /// <param name="paramString">參數字串 (對應 ::參數)</param>
    /// <param name="ct">取消權杖</param>
    /// <returns>執行結果資料物件</returns>
    Task<object?> ExecuteCommandAsync(string command, string paramString, CancellationToken ct = default);
}
