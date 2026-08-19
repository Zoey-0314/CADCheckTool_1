using Autodesk.AutoCAD.Runtime;

using Correct_test1.Configs;
using Correct_test1.VersionCheck.Core;


namespace Correct_test1.Core
{
    public class PluginInitializer :
        IExtensionApplication
    {
        public void Initialize()
        {
            // 1. 路径配置

            try
            {
                AppPathConfig
                    .Initialize();
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "PluginInitializer.PathConfig");
            }


            // 2. 非标归档索引

            try
            {
                NonStandardArchiveCache
                    .Preload();
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "PluginInitializer.ArchivePreload");
            }


            // 3. 版本归档索引

            try
            {
                VersionArchiveCache
                    .Preload();
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "PluginInitializer.VersionArchivePreload");
            }


            // 4. 标准件数据库

            try
            {
                StandardPartDatabase
                    .PreloadAsync();
            }
            catch (System.Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "PluginInitializer.StandardPartPreload");
            }


            AppLogger.Info(
                "CADCheckTool初始化完成。"
                + "归档索引、版本索引和标准件数据库"
                + "已开始后台预加载。",
                "PluginInitializer");
        }


        public void Terminate()
        {
        }
    }
}