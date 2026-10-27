using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mvcon.Contracts;

namespace Mvcon.Host.Services;

/// <summary>
/// 專門負責掃描並載入 *.csvi 檔案 (本質上是自訂副檔名的 .NET Assembly DLL)
/// </summary>
public static class CsviPluginLoader
{
    public static List<ICsviModule> LoadPluginsFromFolder(string folderPath)
    {
        var modules = new List<ICsviModule>();

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
            return modules;
        }

        // 搜尋副檔名為 .csvi 的檔案
        var csviFiles = Directory.GetFiles(folderPath, "*.csvi");

        foreach (var file in csviFiles)
        {
            try
            {
                // .NET Assembly.LoadFrom 直接支援任何副檔名
                var assembly = Assembly.LoadFrom(file);

                var moduleTypes = assembly.GetTypes()
                    .Where(t => typeof(ICsviModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                foreach (var type in moduleTypes)
                {
                    if (Activator.CreateInstance(type) is ICsviModule module)
                    {
                        modules.Add(module);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PluginLoader] 載入 {file} 失敗: {ex.Message}");
            }
        }

        return modules;
    }
}
