# MVCON (C# + Avalonia) 概念驗證專案 (PoC)

本專案實現了如 LabVIEW CSVI 概念的現代跨平台自動化軟體架構：
將 **純底層運算**、**UI 1 (參數導引視窗)** 與 **UI 2 (即時 Front Panel)** 打包至同一個 DLL，並以 `.csvi` 作為副檔名，由主程式動態掃描與掛載。

---

## 📌 指令語法規範

MVCON 採用標準結構化指令格式：
```text
模組@指令::參數
```

* **範例**：`DMM@MEAS_VOLT::{"Channel":"ai0","ScaleFactor":1.0,"HighLimit":4.0}`
  * `DMM`：對應模組識別碼 (`ICsviModule.ModuleName`)。
  * `MEAS_VOLT`：對應支援指令 (`ICsviModule.SupportedCommands`)。
  * `{"Channel":"ai0", ...}`：由模組 UI 1 導引精靈編輯並序列化產生之 JSON 參數。

---

## 🏗 專案架構目錄

```text
Mvcon-Avalonia-PoC/
├── Mvcon.sln                             # 完整 Visual Studio / JetBrains Rider 解決方案檔
├── src/
│   ├── Mvcon.Contracts/                  # 共用契約規範庫
│   │   ├── Mvcon.Contracts.csproj
│   │   └── ICsviModule.cs                # 核心模組介面 (EditParameter, GetFrontPanel, ExecuteCommand)
│   │
│   ├── Modules/
│   │   └── DmmSensorModule/              # 範例 CSVI 模組 (建置後產出 DmmSensorModule.csvi)
│   │       ├── DmmSensorModule.csproj    # 包含自動更名並複製為 .csvi 的 Post-Build Target
│   │       ├── Models/
│   │       │   └── VoltCommandParam.cs   # 指令參數 POCO (JSON 序列化)
│   │       ├── Core/
│   │       │   └── DmmCoreEngine.cs      # [底層函數] 純數據採樣/演算法 (無 UI 依賴)
│   │       ├── Views/
│   │       │   ├── DmmParamWizardWindow  # [UI 1] 參數設定精靈視窗 (編輯 ::參數)
│   │       │   ├── DmmFrontPanelView     # [UI 2] 運行即時 Front Panel 面板
│   │       │   └── DmmFrontPanelViewModel.cs
│   │       └── DmmSensorCsvi.cs          # 模組進入點 (實作 ICsviModule)
│   │
│   └── Mvcon.Host/                       # MVCON 前端主控台 (Avalonia 11 桌面程式)
│       ├── Mvcon.Host.csproj
│       ├── Services/
│       │   └── CsviPluginLoader.cs       # 動態載入 Plugins/*.csvi (Assembly.LoadFrom)
│       ├── MainWindow.axaml / .cs        # 整合介面 (指令編輯、SubPanel 容器、日誌監看)
│       └── Program.cs / App.axaml
```

---

## 🚀 如何建置與運行

### 1. 開發環境需求
* **.NET 8.0 SDK** (或 .NET 9.0 SDK)
* 支援 IDE：**Visual Studio 2022**、**JetBrains Rider** 或 **VS Code (C# Dev Kit)**

### 2. 建置步驟
1. 使用 Visual Studio 或命令列開啟 `Mvcon.sln`。
2. 建置 `DmmSensorModule` 專案：
   * 建置完成後，`DmmSensorModule.csproj` 內建的 Target 會自動將編譯出的 DLL 改名為 `DmmSensorModule.csvi`，並直接拷貝到 `Mvcon.Host/bin/Debug/net8.0/Plugins/` 目錄下！
3. 啟動並運行 `Mvcon.Host` 主程式。

### 3. 操作測試功能
* **測試 UI 1 (編輯參數)**：
  點擊左側「⚙ 編輯參數」按鈕，彈出專屬參數導引視窗，調整通道、倍率或上限，點確定後指令框的 `::參數` 會自動更新！
* **測試 UI 2 (SubPanel 嵌入)**：
  點擊「📌 掛載 Front Panel」，右側的 SubPanel 容器會動態換上該模組的前面板。點擊「❌ 卸載」可清空容器。
* **測試底層執行 (SubVI 模式)**：
  點擊「⚡ 執行指令」，主程式以背景非同步呼叫模組底層函數，零 UI 負擔完成運算；若此時右側有掛載 Front Panel，數值與警報燈會同步跳動！
