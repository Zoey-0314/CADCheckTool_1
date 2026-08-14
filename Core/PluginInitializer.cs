using Autodesk.AutoCAD.Runtime;

namespace Correct_test1.Core
{
    /// <summary>
    /// CADCheckTool插件生命周期入口。
    ///
    /// NETLOAD或AutoCAD自动加载插件后，
    /// Initialize会执行一次。
    /// </summary>
    public class PluginInitializer :
        IExtensionApplication
    {
        public void Initialize()
        {
            try
            {
                //--------------------------------
                // 不等待。
                //
                // AutoCAD继续正常启动，
                // Z盘归档索引在后台建立。
                //--------------------------------

                NonStandardArchiveCache
                    .Preload();


                AppLogger.Info(
                    "CADCheckTool初始化完成，"
                    + "非标归档索引已开始后台预加载。",
                    "PluginInitializer");
            }
            catch (System.Exception ex)
            {
                //--------------------------------
                // Z盘索引失败不能导致整个插件
                // 加载失败。
                //--------------------------------

                AppLogger.Error(
                    ex,
                    "PluginInitializer.Initialize");
            }
        }


        public void Terminate()
        {
        }
    }
}