using System;
using System.IO;
using Correct_test1.Core;

namespace Correct_test1.Installer
{
    /// <summary>
    /// CADCheckTool 安装部署助手。
    /// 
    /// 注意：
    /// 文件复制由 Inno Setup 完成。
    /// 本类只负责：
    /// 1. 验证安装文件
    /// 2. 注册 AutoCAD 自动加载
    /// 3. 卸载时清理注册并备份用户数据
    /// </summary>
    public sealed class CADCheckToolInstaller
    {
        private const string PluginFileName = "CADCheckTool_1.dll";


        private readonly string installationDirectory;

        private readonly CADPluginRegistryManager registryManager;


        /// <summary>
        /// 默认安装目录：
        /// C:\Program Files\CADCheckTool_1
        /// </summary>
        public CADCheckToolInstaller()
            : this(
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ProgramFiles),
                    "CADCheckTool_1"))
        {

        }



        /// <summary>
        /// 指定安装目录
        /// </summary>
        public CADCheckToolInstaller(string installationDirectory)
        {
            if (string.IsNullOrWhiteSpace(installationDirectory))
            {
                throw new ArgumentException(
                    "安装目录不能为空。",
                    nameof(installationDirectory));
            }


            this.installationDirectory =
                Path.GetFullPath(
                    installationDirectory);


            registryManager =
                new CADPluginRegistryManager();
        }



        public string InstallationDirectory
        {
            get
            {
                return installationDirectory;
            }
        }



        /// <summary>
        /// 执行安装后的初始化。
        /// 
        /// Inno Setup 已经完成文件复制，
        /// 此处只注册 AutoCAD。
        /// </summary>
        public void Install()
        {
            try
            {

                string dllPath =
                    Path.Combine(
                        installationDirectory,
                        PluginFileName);



                if (!File.Exists(dllPath))
                {
                    throw new FileNotFoundException(
                        "安装目录中不存在 CAD 插件 DLL。",
                        dllPath);
                }



                Directory.CreateDirectory(
                    Path.Combine(
                        installationDirectory,
                        "Logs"));



                Directory.CreateDirectory(
                    Path.Combine(
                        installationDirectory,
                        "Configs"));



                registryManager.RegisterPlugin(
                    dllPath);



                AppLogger.Info(
                    "[Installer] Installation completed successfully",
                    "Installer");

            }
            catch (Exception ex)
            {
                AppLogger.Error(
                    ex,
                    "Installer",
                    message:
                    "[Installer] Installation failed");

                throw;
            }
        }




        /// <summary>
        /// 卸载：
        /// 1. 删除 AutoCAD 自动加载
        /// 2. 备份用户数据
        /// 3. 删除安装目录
        /// </summary>
        public void Uninstall()
        {
            try
            {

                registryManager.UnregisterPlugin();



                BackupUserData();



                if (Directory.Exists(
                    installationDirectory))
                {
                    Directory.Delete(
                        installationDirectory,
                        true);
                }



                AppLogger.Info(
                    "[Installer] Uninstallation completed successfully",
                    "Installer");

            }
            catch (Exception ex)
            {

                AppLogger.Error(
                    ex,
                    "Installer",
                    message:
                    "[Installer] Uninstallation failed");

                throw;
            }

        }





        private void BackupUserData()
        {

            string backupRoot =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "CADCheckTool_1",
                    "UninstallBackups",
                    DateTime.Now.ToString(
                        "yyyyMMddHHmmss"));



            BackupDirectory(
                Path.Combine(
                    installationDirectory,
                    "Configs"),
                Path.Combine(
                    backupRoot,
                    "Configs"));



            BackupDirectory(
                Path.Combine(
                    installationDirectory,
                    "Logs"),
                Path.Combine(
                    backupRoot,
                    "Logs"));

        }





        private static void BackupDirectory(
            string source,
            string destination)
        {

            if (!Directory.Exists(source))
                return;



            Directory.CreateDirectory(
                destination);



            foreach (string file in
                Directory.GetFiles(source))
            {

                string target =
                    Path.Combine(
                        destination,
                        Path.GetFileName(file));


                File.Copy(
                    file,
                    target,
                    true);
            }



            foreach (string dir in
                Directory.GetDirectories(source))
            {

                BackupDirectory(
                    dir,
                    Path.Combine(
                        destination,
                        Path.GetFileName(dir)));

            }

        }

    }
}