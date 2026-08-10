using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Correct_test1.Core;

namespace Correct_test1.Installer
{
    /// <summary>
    /// 管理当前用户 AutoCAD Applications 注册表中的插件自动加载配置。
    /// </summary>
    public sealed class CADPluginRegistryManager
    {
        private const string ApplicationName = "CADCheckTool_1";
        private const string Description = "CADCheckTool_1 AutoCAD Engineering Drawing Inspection Plugin";
        private const int LoadControls = 15;

        /// <summary>
        /// 为当前 Windows 用户已发现的所有 AutoCAD 产品配置注册插件。
        /// </summary>
        public void RegisterPlugin(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath))
                throw new ArgumentException("DLL 路径不能为空。", nameof(dllPath));

            var applicationKeys = FindAutoCadApplicationKeys(true);
            if (applicationKeys.Count == 0)
                throw new InvalidOperationException("未找到当前用户的 AutoCAD 注册表项。请先至少启动一次 AutoCAD，再执行安装。");

            try
            {
                using (RegistryKey currentUser = OpenCurrentUser64())
                {
                    foreach (string applicationsPath in applicationKeys)
                    {
                        using (RegistryKey applicationsKey = currentUser.CreateSubKey(applicationsPath))
                        using (RegistryKey pluginKey = applicationsKey.CreateSubKey(ApplicationName))
                        {
                            pluginKey.SetValue("DESCRIPTION", Description, RegistryValueKind.String);
                            pluginKey.SetValue("LOADER", dllPath, RegistryValueKind.String);
                            pluginKey.SetValue("LOADCTRLS", LoadControls, RegistryValueKind.DWord);
                            pluginKey.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                        }
                    }
                }

                AppLogger.Info("[Installer] Plugin registered successfully", "Installer");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Installer", message: "[Installer] Plugin registration failed");
                throw;
            }
        }

        /// <summary>
        /// 移除本插件创建的所有 AutoCAD 自动加载注册表项，不影响其他插件。
        /// </summary>
        public void UnregisterPlugin()
        {
            try
            {
                using (RegistryKey currentUser = OpenCurrentUser64())
                {
                    foreach (string applicationsPath in FindAutoCadApplicationKeys(false))
                    {
                        using (RegistryKey applicationsKey = currentUser.OpenSubKey(applicationsPath, true))
                        {
                            if (applicationsKey != null)
                                applicationsKey.DeleteSubKeyTree(ApplicationName, false);
                        }
                    }
                }

                AppLogger.Info("[Installer] Plugin removed successfully", "Installer");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Installer", message: "[Installer] Plugin removal failed");
                throw;
            }
        }

        /// <summary>
        /// 检查已发现的 AutoCAD 产品是否均已注册此插件。
        /// </summary>
        public bool IsRegistered()
        {
            List<string> applicationKeys = FindAutoCadApplicationKeys(false);
            if (applicationKeys.Count == 0)
                return false;

            using (RegistryKey currentUser = OpenCurrentUser64())
            {
                foreach (string applicationsPath in applicationKeys)
                {
                    using (RegistryKey pluginKey = currentUser.OpenSubKey(applicationsPath + "\\" + ApplicationName, false))
                    {
                        if (pluginKey == null || pluginKey.GetValue("LOADER") == null)
                            return false;
                    }
                }
            }

            return true;
        }

        // 递归寻找已有 Applications 节点，因此不会把 AutoCAD 版本、语言或产品编号写死在代码中。
        private static List<string> FindAutoCadApplicationKeys(bool createMissingApplicationsKey)
        {
            var result = new List<string>();
            const string autoCadRootPath = @"Software\Autodesk\AutoCAD";

            using (RegistryKey currentUser = OpenCurrentUser64())
            using (RegistryKey autoCadRoot = currentUser.OpenSubKey(autoCadRootPath, createMissingApplicationsKey))
            {
                if (autoCadRoot == null)
                    return result;

                FindApplicationKeys(autoCadRoot, autoCadRootPath, result);
            }

            return result;
        }

        // 强制使用 64 位注册表视图，确保与 64 位 AutoCAD 读取的 HKCU 路径一致。
        private static RegistryKey OpenCurrentUser64()
        {
            return RegistryKey.OpenBaseKey(
                RegistryHive.CurrentUser,
                RegistryView.Registry64);
        }

        private static void FindApplicationKeys(RegistryKey currentKey, string currentPath, List<string> result)
        {
            foreach (string subKeyName in currentKey.GetSubKeyNames())
            {
                string subKeyPath = currentPath + "\\" + subKeyName;
                if (string.Equals(subKeyName, "Applications", StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(subKeyPath);
                    continue;
                }

                using (RegistryKey subKey = currentKey.OpenSubKey(subKeyName, false))
                {
                    if (subKey != null)
                        FindApplicationKeys(subKey, subKeyPath, result);
                }
            }
        }
    }
}
