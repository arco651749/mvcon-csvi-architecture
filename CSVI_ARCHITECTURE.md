# MVCON CSVI (Configurable SubVI) 架構設計規範書
### 現代跨平台工業自動化與測試模組體系 (C# + Avalonia)

> **版本**：2.0 (Grand Unified CSVI Edition)  
> **更新日期**：2026-10-28  
> **核心哲學**：一組件四面向 (One Assembly, Four Facets)、目錄即指令 (Directory-Driven Discovery)、零冗餘目錄 (Zero Legacy Folders)。

---

## 📌 1. 設計哲學與背景 (Background & Philosophy)

在傳統工業量測與自動化測試（ATE）領域，**NI LabVIEW** 的 **VI（Virtual Instrument）** 架構具備無可取代的直覺優勢：
- **Block Diagram**：負責背景演算法與通訊（SubVI 呼叫）。
- **Front Panel**：負責視覺化儀表顯示，並可透過 SubPanel 嵌入主介面。
- **Express VI / Config Dialog**：使用者雙擊模組時，彈出參數精靈對話框導引設定。

然而，LabVIEW 存在以下痛點：
1. **多頭維護與冗餘目錄**：因為執行與彈窗無法同簽名，被強行切分出 `USERPARAMETEREDITOR/` 目錄專門放彈窗 VI，導致檔案分裂與維護混亂。
2. **授權昂貴與執行檔龐大**：動輒數 GB 的 Runtime Engine。
3. **無法靈活 Git Diff**：二進位 VI 無法良好進行版本控制。

**MVCON CSVI（Configurable SubVI）2.0** 終極整合現代 **C# (.NET 8/9) + Avalonia UI** 框架，實現真正的 **「四位一體」** 統一虛擬儀表：
1. **四位一體打包 (`.csvi`)**：將「純底層執行」、「UI 1 參數導引視窗」、「即時運算推播」、「UI 2 子面板人機/驅動前面板」封裝於同一個動態組件。
2. **目錄即指令與驅動 (Directory-Driven Discovery)**：
   - `MODULES/<Module>/*.csvi`：目錄下有多少 `.csvi` 就有多少指令。
   - `MODULES/<Module>/DRIVER/*.csvi`：目錄下有多少 `.csvi` 就有多少驅動。
   - **徹底消除 `USERPARAMETEREDITOR` 冗餘目錄**！
3. **DSL 結構化語法**：透過 `模組@指令::參數`（或 `Module@Command,Params`），達成最高可讀性與配方儲存。
4. **極致效能分離**：全速生產時只跑純 C# IL，0ms UI 開銷；需要除錯時開窗即視化。
5. **零鎖檔熱插拔**：以 `File.ReadAllBytes` 載入記憶體，產線運轉中組件可隨時覆蓋替換。

---

## 🏗️ 2. 模組架構：四位一體 (The Four Facets)

每一個 `.csvi` 檔案本質上是一個標準的 .NET 動態連結庫（Assembly DLL），具備四個完全解耦但協同運作的面向：

```text
┌────────────────────────────────────────────────────────┐
│                   MyModule.csvi                        │
│                                                        │
│  [面向 1] 純執行 (Headless Execution)                  │
│    - 純 C# 邏輯，0ms UI 開銷                          │
│    - 負責硬體通訊 (VISA/SCPI/DAQ)、邏輯判斷與微步等待  │
│    - 供產線全速批次自動化執行 (SubVI 模式，無任何損耗) │
│                                                        │
│  [面向 2] 參數導引精靈 (Parameter Wizard Dialog)       │
│    - 雙擊指令儲存格時彈出的對話框                      │
│    - 自動向 BasicEngines 抓取點位、軸代號、變數清單    │
│    - 零記憶負擔 (Zero Cognitive Load)，防止手動打字拼錯│
│                                                        │
│  [面向 3] 即時運算視覺化 (Live Debug Visualizer)       │
│    - 執行時透過 NotifyExecution 事件推播最新狀態       │
│    - 運算過程、通訊耗時、電位狀態即時呈現              │
│                                                        │
│  [面向 4] 子面板與驅動 HMI (SubPanel & Driver HMI)     │
│    - GetFrontPanelControl() 回傳 Avalonia 原生控制項   │
│    - 可動態嵌入主介面 User Tab 的 SubPanel 容器        │
│    - 亦可作為 MODULES/<Module>/DRIVER/ 的硬體監控台   │
└────────────────────────────────────────────────────────┘
```

---

## 📂 3. 目錄驅動拓撲 (Directory-Driven Topology)

在 CSVI 架構下，**`USERPARAMETEREDITOR` 目錄已完全廢止**。

```text
dist/MVCON_Runtime/
├── MVCON.App.exe                  <-- 主程式 (純粹的 UI 支架骨架)
├── MODULES/
│   ├── IO/
│   │   ├── SetPort.csvi           <-- 指令 1: 內建純執行 + 導引視窗 + 前面板
│   │   ├── ClrPort.csvi           <-- 指令 2
│   │   ├── WaitPortOn.csvi        <-- 指令 3
│   │   ├── WaitPortOff.csvi       <-- 指令 4
│   │   ├── Flash(OnOff).csvi      <-- 指令 5
│   │   ├── Flash(OffOn).csvi      <-- 指令 6
│   │   ├── CheckLogicOn.csvi      <-- 指令 7
│   │   ├── CheckLogicOff.csvi     <-- 指令 8
│   │   ├── SetPortName.csvi       <-- 指令 9
│   │   ├── AddVirtualIO.csvi      <-- 指令 10
│   │   ├── AddIODriver.csvi       <-- 指令 11
│   │   ├── InitIOPort.csvi        <-- 指令 12
│   │   ├── ForceUpdate.csvi       <-- 指令 13
│   │   ├── Shift.csvi             <-- 指令 14
│   │   ├── IO.csvi                <-- 模組多合一複合包 (備援)
│   │   └── DRIVER/
│   │       └── MockIO.csvi        <-- 驅動 1: 實作 IIoDriver + 驅動前面板
│   ├── MOTION/
│   │   ├── GoPoint.csvi
│   │   ├── WaitMotor.csvi
│   │   └── DRIVER/
│   │       └── MockMotion.csvi
│   ├── CAPTURE/
│   └── DAQ/
```

---

## 🔌 4. 統一契約介面 (`IParameterGuidance.cs`)

```csharp
namespace MVCON.Core.Guidance;

public interface IParameterGuidance
{
    string CommandKey { get; }

    // 面向 2: 參數導引視窗 (Wizard Dialog)
    Task<string?> ShowGuidanceDialogAsync(string currentParam, BasicEngines engines, object? owner = null);

    // 面向 1: 純執行 (Headless Execution)
    CommandResult Execute(ExecutionContext ctx, string action, string parameter);

    // 面向 4: 前面板 / SubPanel 嵌入 / 驅動 HMI
    object? GetFrontPanelControl() => null;
}
```

---

## 🚀 5. 跨平台與實機驗證成果

本架構在 Windows 虛擬顯示器環境（Desktop Cockpit）中進行全流程驗證：
- 支援 16-Bit 原生 IO 即時狀態顯示與手動點擊切換。
- 支援 SubPanel 一鍵熱插拔切換（IO CSVI 面板 ➔ Driver CSVI 面板 ➔ 預設面板）。
- 雙擊參數欄即刻彈出 `IO@SetPort_Parameter.vi`，即時反選並產生參數。
- 86/86 單元測試全數通過，100% 綠燈。
