using Autodesk.AutoCAD.Runtime;

using Correct_test1.Configs;

namespace Correct_test1.Core
{
    public class PluginInitializer :
        IExtensionApplication
    {
        public void Initialize()
        {
            //--------------------------------
            // 三件事：
            //
            // 1. 读取用户路径配置
            // 2. 后台预加载归档索引
            // 3. 后台预加载标准件Excel
            //--------------------------------

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
                + "归档索引和标准件数据库"
                + "已开始后台预加载。",
                "PluginInitializer");
        }


        public void Terminate()
        {
        }
    }
}