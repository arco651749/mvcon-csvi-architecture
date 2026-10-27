using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mvcon.Contracts;
using Mvcon.Host.Services;

namespace Mvcon.Host;

public partial class MainWindow : Window
{
    private readonly List<ICsviModule> _loadedModules = new();
    private readonly StringBuilder _logBuilder = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        // 模組外掛目錄
        string pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        AppendLog($"[系統] 掃描外掛目錄: {pluginDir}");

        var found = CsviPluginLoader.LoadPluginsFromFolder(pluginDir);
        _loadedModules.Clear();
        _loadedModules.AddRange(found);

        LstModules.ItemsSource = _loadedModules.Select(m => $"{m.ModuleName} - {m.DisplayName}").ToList();

        if (_loadedModules.Count > 0)
        {
            LstModules.SelectedIndex = 0;
            AppendLog($"[外掛] 成功載入 {_loadedModules.Count} 個 .csvi 模組！");
        }
        else
        {
            AppendLog("[提示] Plugins 目錄內尚無 .csvi 模組，請先編譯 DmmSensorModule。");
        }
    }

    private void OnModuleSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (LstModules.SelectedIndex >= 0 && LstModules.SelectedIndex < _loadedModules.Count)
        {
            var mod = _loadedModules[LstModules.SelectedIndex];
            AppendLog($"[選取] 模組: {mod.ModuleName} ({mod.DisplayName})");
        }
    }

    // =========================================================================
    // UI 1: 編輯 ::參數 (解析語法 -> 叫出對應模組的導引視窗 -> 填回指令)
    // =========================================================================
    private async void OnEditParameterClick(object? sender, RoutedEventArgs e)
    {
        string currentLine = TxtCommandLine.Text?.Trim() ?? "";
        if (!TryParseCommand(currentLine, out string modName, out string cmd, out string param))
        {
            AppendLog("[錯誤] 指令語法不合法！必須為：模組@指令::參數");
            return;
        }

        var module = _loadedModules.FirstOrDefault(m => string.Equals(m.ModuleName, modName, StringComparison.OrdinalIgnoreCase));
        if (module == null)
        {
            AppendLog($"[錯誤] 找不到名為 [{modName}] 的 .csvi 模組！");
            return;
        }

        AppendLog($"[UI 1] 開啟 {modName}@{cmd} 參數導引精靈...");

        // 呼叫模組 UI 1 (參數導引)
        string? newParamString = await module.EditParameterAsync(cmd, param, this);

        if (newParamString != null)
        {
            // 使用者點擊「確定」，將新參數字串重新組裝回指令行！
            string newLine = $"{modName}@{cmd}::{newParamString}";
            TxtCommandLine.Text = newLine;
            AppendLog($"[參數更新] 指令已更新為：\n{newLine}");
        }
        else
        {
            AppendLog("[UI 1] 使用者取消了參數編輯。");
        }
    }

    // =========================================================================
    // UI 2: 掛載 Front Panel 到主畫面 SubPanel
    // =========================================================================
    private void OnMountFrontPanelClick(object? sender, RoutedEventArgs e)
    {
        string currentLine = TxtCommandLine.Text?.Trim() ?? "";
        if (!TryParseCommand(currentLine, out string modName, out _, out _))
        {
            AppendLog("[錯誤] 無法解析模組名稱！");
            return;
        }

        var module = _loadedModules.FirstOrDefault(m => string.Equals(m.ModuleName, modName, StringComparison.OrdinalIgnoreCase));
        if (module == null)
        {
            AppendLog($"[錯誤] 找不到模組 [{modName}]");
            return;
        }

        // 像 LabVIEW SubPanel 一樣直接嵌入！
        SubPanelContainer.Content = module.GetFrontPanelControl();
        AppendLog($"[UI 2] 已將模組 [{modName}] 的 Front Panel 嵌入至 SubPanel！");
    }

    private void OnUnmountFrontPanelClick(object? sender, RoutedEventArgs e)
    {
        SubPanelContainer.Content = new TextBlock
        {
            Text = "[目前尚未掛載任何 Front Panel]",
            Foreground = Avalonia.Media.Brushes.Gray,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        AppendLog("[UI 2] 已卸載 SubPanel 內容。");
    }

    // =========================================================================
    // 底層純執行 (SubVI 模式，吃 @指令::參數)
    // =========================================================================
    private async void OnExecuteCommandClick(object? sender, RoutedEventArgs e)
    {
        string currentLine = TxtCommandLine.Text?.Trim() ?? "";
        if (!TryParseCommand(currentLine, out string modName, out string cmd, out string param))
        {
            AppendLog("[錯誤] 指令語法不合法！必須為：模組@指令::參數");
            return;
        }

        var module = _loadedModules.FirstOrDefault(m => string.Equals(m.ModuleName, modName, StringComparison.OrdinalIgnoreCase));
        if (module == null)
        {
            AppendLog($"[錯誤] 找不到模組 [{modName}]");
            return;
        }

        AppendLog($"[執行] 發送指令 => {modName}@{cmd}，參數: {param}");

        try
        {
            // 純底層執行！
            var result = await module.ExecuteCommandAsync(cmd, param);
            AppendLog($"[執行成功] 回傳結果: {result}");
        }
        catch (Exception ex)
        {
            AppendLog($"[執行異常] {ex.Message}");
        }
    }

    // =========================================================================
    // 輔助函式：解析 模組@指令::參數
    // =========================================================================
    private static bool TryParseCommand(string input, out string moduleName, out string command, out string parameters)
    {
        moduleName = "";
        command = "";
        parameters = "";

        if (string.IsNullOrWhiteSpace(input)) return false;

        // 範例：DMM@MEAS_VOLT::{"Channel":"ai0"}
        int atIndex = input.IndexOf('@');
        if (atIndex <= 0) return false;

        moduleName = input.Substring(0, atIndex).Trim();

        string remaining = input.Substring(atIndex + 1);
        int colonIndex = remaining.IndexOf("::", StringComparison.Ordinal);

        if (colonIndex < 0)
        {
            command = remaining.Trim();
            parameters = "";
        }
        else
        {
            command = remaining.Substring(0, colonIndex).Trim();
            parameters = remaining.Substring(colonIndex + 2).Trim();
        }

        return !string.IsNullOrEmpty(moduleName) && !string.IsNullOrEmpty(command);
    }

    private void AppendLog(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        _logBuilder.AppendLine($"[{timestamp}] {message}");
        TxtLog.Text = _logBuilder.ToString();
    }
}
