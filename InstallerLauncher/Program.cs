using System;
using System.IO;
using Correct_test1.Installer;


namespace InstallerLauncher
{
    internal class Program
    {
        static void Main(string[] args)
        {

            string sourceDirectory =
                AppDomain.CurrentDomain.BaseDirectory;


            string installationDirectory =
                @"C:\Program Files\CADCheckTool_1";


            CADCheckToolInstaller installer =
                new CADCheckToolInstaller(
                    installationDirectory);


            if (args.Length == 0)
            {
                Console.WriteLine(
                    "请输入 install 或 uninstall"
                );
                return;
            }


            if (args[0].ToLower() == "install")
            {
                installer.Install();

                Console.WriteLine(
                    "安装完成"
                );
            }


            if (args[0].ToLower() == "uninstall")
            {
                installer.Uninstall();

                Console.WriteLine(
                    "卸载完成"
                );
            }
        }
    }
}