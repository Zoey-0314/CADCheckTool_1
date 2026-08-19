using System;
using System.Collections.Generic;
using Microsoft.Win32;
using Correct_test1.Core;

namespace Correct_test1.Installer
{
    public sealed class CADPluginRegistryManager
    {
        private const string ApplicationName = "CADCheckTool_1";
        private const string Description = "CADCheckTool_1 AutoCAD Engineering Drawing Inspection Plugin";
        private const int LoadControls = 15;

        private sealed class ApplicationKeyLocation
        {
            public RegistryView View { get; set; }
            public string ApplicationsPath { get; set; }
        }

        /// <summary>
        /// 为当前 Windows 用户已发现的所有 AutoCAD 产品配置注册插件。
        /// </summary>
        public void RegisterPlugin(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath))
                throw new ArgumentException("DLL 路径不能为空。", nameof(dllPath));

            try
            {
                List<ApplicationKeyLocation> applicationKeys =
                    FindAutoCadApplicationKeys(true);

                if (applicationKeys.Count == 0)
                {
                    throw new InvalidOperationException(
                        "未找到当前用户的 AutoCAD Applications 注册表项。");
                }

                foreach (ApplicationKeyLocation location in applicationKeys)
                {
                    using (RegistryKey localMachine = OpenLocalMachine(location.View))
                    using (RegistryKey applicationsKey =
                        localMachine.CreateSubKey(location.ApplicationsPath))
                    using (RegistryKey pluginKey =
                        applicationsKey.CreateSubKey(ApplicationName))
                    {
                        if (pluginKey == null)
                            throw new InvalidOperationException(
                                "无法创建 AutoCAD 插件注册表项。");

                        pluginKey.SetValue("DESCRIPTION", Description, RegistryValueKind.String);
                        pluginKey.SetValue("LOADER", dllPath, RegistryValueKind.String);
                        pluginKey.SetValue("LOADCTRLS", LoadControls, RegistryValueKind.DWord);
                        pluginKey.SetValue("MANAGED", 1, RegistryValueKind.DWord);
                    }
                }

                AppLogger.Info(
                    "[Installer] 插件注册成功",
                    "Installer");
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Error(
                    ex,
                    "Installer",
                    message: "[Installer] 注册表权限被拒绝。请以管理员身份运行安装程序。");
                throw;
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "Installer",
                    message: "[Installer] 插件注册失败");
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
                foreach (ApplicationKeyLocation location in FindAutoCadApplicationKeys(false))
                {
                    using (RegistryKey localMachine = OpenLocalMachine(location.View))
                    using (RegistryKey applicationsKey =
                        localMachine.OpenSubKey(location.ApplicationsPath, true))
                    {
                        if (applicationsKey != null)
                        {
                            applicationsKey.DeleteSubKeyTree(
                                ApplicationName,
                                false);
                        }
                    }
                }

                AppLogger.Info(
                    "[Installer] 插件移除成功",
                    "Installer");
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Error(
                    ex,
                    "Installer",
                    message: "[Installer] 注册表权限被拒绝。请以管理员身份运行安装程序。");
                throw;
            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "Installer",
                    message: "[Installer] 插件移除失败");
                throw;
            }
        }

        /// <summary>
        /// 检查已发现的 AutoCAD 产品是否均已注册此插件。
        /// </summary>
        public bool IsRegistered()
        {
            try
            {
                List<ApplicationKeyLocation> applicationKeys =
                    FindAutoCadApplicationKeys(false);

                if (applicationKeys.Count == 0)
                    return false;

                foreach (ApplicationKeyLocation location in applicationKeys)
                {
                    using (RegistryKey localMachine = OpenLocalMachine(location.View))
                    using (RegistryKey pluginKey = localMachine.OpenSubKey(
                        location.ApplicationsPath + "\\" + ApplicationName,
                        false))
                    {
                        if (pluginKey == null || pluginKey.GetValue("LOADER") == null)
                            return false;
                    }
                }

                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                AppLogger.Error(
                    ex,
                    "Installer",
                    message: "[Installer] 注册表权限被拒绝。请以管理员身份运行安装程序。");
                return false;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Installer", message: "[Installer] 注册表检查失败");
                return false;
            }
        }

        // 递归寻找已有 Applications 节点，因此不会把 AutoCAD 版本、语言或产品编号写死在代码中。
        private static List<ApplicationKeyLocation> FindAutoCadApplicationKeys(
            bool createMissingApplicationsKey)
        {
            List<ApplicationKeyLocation> result =
                new List<ApplicationKeyLocation>();

            FindAutoCadApplicationKeys(
                RegistryView.Registry64,
                @"Software\Autodesk\AutoCAD",
                createMissingApplicationsKey,
                result);

            FindAutoCadApplicationKeys(
                RegistryView.Registry64,
                @"Software\WOW6432Node\Autodesk\AutoCAD",
                createMissingApplicationsKey,
                result);

            FindAutoCadApplicationKeys(
                RegistryView.Registry32,
                @"Software\Autodesk\AutoCAD",
                createMissingApplicationsKey,
                result);

            return result;
        }

        private static void FindAutoCadApplicationKeys(
            RegistryView view,
            string autoCadRootPath,
            bool createMissingApplicationsKey,
            List<ApplicationKeyLocation> result)
        {
            using (RegistryKey localMachine = OpenLocalMachine(view))
            using (RegistryKey autoCadRoot = localMachine.OpenSubKey(
                autoCadRootPath,
                createMissingApplicationsKey))
            {
                if (autoCadRoot == null)
                    return;

                List<string> paths = new List<string>();
                FindApplicationKeys(autoCadRoot, autoCadRootPath, paths);

                foreach (string path in paths)
                {
                    bool exists = false;
                    foreach (ApplicationKeyLocation item in result)
                    {
                        if (item.View == view &&
                            string.Equals(
                                item.ApplicationsPath,
                                path,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        result.Add(new ApplicationKeyLocation
                        {
                            View = view,
                            ApplicationsPath = path
                        });
                    }
                }
            }
        }

        // 强制使用 64 位注册表视图，确保与 64 位 AutoCAD 读取的 HKLM 路径一致。
        private static RegistryKey OpenLocalMachine(RegistryView view)
        {
            return RegistryKey.OpenBaseKey(
                RegistryHive.LocalMachine,
                view);
        }

        private static void FindApplicationKeys(
            RegistryKey currentKey,
            string currentPath,
            List<string> result)
        {
            foreach (string subKeyName in currentKey.GetSubKeyNames())
            {
                string subKeyPath = currentPath + "\\" + subKeyName;

                if (string.Equals(
                    subKeyName,
                    "Applications",
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(subKeyPath);
                    continue;
                }

                using (RegistryKey subKey = currentKey.OpenSubKey(subKeyName, false))
                {
                    if (subKey != null)
                    {
                        FindApplicationKeys(subKey, subKeyPath, result);
                    }
                }
            }
        }
    }
}
