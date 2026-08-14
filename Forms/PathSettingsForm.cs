using Correct_test1.Configs;
using Correct_test1.Core;

using System;
using System.IO;
using System.Windows.Forms;

namespace Correct_test1
{
    public partial class PathSettingsForm :
        Form
    {
        public PathSettingsForm()
        {
            InitializeComponent();


            LoadCurrentSettings();
        }


        private void LoadCurrentSettings()
        {
            AppPathSettings settings =
                AppPathConfig.Current;


            txtArchivePath.Text =
                settings
                    .NonStandardArchivePath;


            txtStandardPartPath.Text =
                settings
                    .StandardPartDatabasePath;
        }


        private void btnBrowseArchive_Click(
            object sender,
            EventArgs e)
        {
            using (
                FolderBrowserDialog dialog =
                    new FolderBrowserDialog())
            {
                dialog.Description =
                    "请选择非标归档图纸根目录";


                if (Directory.Exists(
                        txtArchivePath.Text))
                {
                    dialog.SelectedPath =
                        txtArchivePath.Text;
                }


                if (dialog.ShowDialog(
                        this)
                    != DialogResult.OK)
                {
                    return;
                }


                txtArchivePath.Text =
                    dialog.SelectedPath;
            }
        }


        private void btnBrowseStandardPart_Click(
            object sender,
            EventArgs e)
        {
            using (
                OpenFileDialog dialog =
                    new OpenFileDialog())
            {
                dialog.Title =
                    "请选择诺升标准件数据库";


                dialog.Filter =
                    "Excel工作簿 (*.xlsx)|*.xlsx";


                dialog.CheckFileExists =
                    true;


                if (File.Exists(
                        txtStandardPartPath.Text))
                {
                    dialog.InitialDirectory =
                        Path.GetDirectoryName(
                            txtStandardPartPath.Text);


                    dialog.FileName =
                        Path.GetFileName(
                            txtStandardPartPath.Text);
                }


                if (dialog.ShowDialog(
                        this)
                    != DialogResult.OK)
                {
                    return;
                }


                txtStandardPartPath.Text =
                    dialog.FileName;
            }
        }


        private void btnSave_Click(
            object sender,
            EventArgs e)
        {
            string archivePath =
                txtArchivePath
                    .Text
                    .Trim();


            string standardPartPath =
                txtStandardPartPath
                    .Text
                    .Trim();


            //--------------------------------
            // 保存前验证
            //--------------------------------

            if (!Directory.Exists(
                    archivePath))
            {
                MessageBox.Show(
                    "非标归档目录不存在或当前无法访问：\n"
                    + archivePath,
                    "路径设置");

                return;
            }


            if (!File.Exists(
                    standardPartPath))
            {
                MessageBox.Show(
                    "标准件数据库文件不存在或当前无法访问：\n"
                    + standardPartPath,
                    "路径设置");

                return;
            }


            if (!string.Equals(
                    Path.GetExtension(
                        standardPartPath),
                    ".xlsx",
                    StringComparison
                        .OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "标准件数据库必须是 .xlsx 文件。",
                    "路径设置");

                return;
            }


            try
            {
                AppPathConfig.Save(
                    new AppPathSettings
                    {
                        NonStandardArchivePath =
                            archivePath,

                        StandardPartDatabasePath =
                            standardPartPath
                    });


                //--------------------------------
                // 用户保存以后立即后台刷新。
                //
                // 不需要重启AutoCAD。
                //--------------------------------

                NonStandardArchiveCache
                    .RefreshAsync();


                StandardPartDatabase
                    .RefreshAsync();


                MessageBox.Show(
                    "路径设置已保存。\n\n"
                    + "非标归档索引和标准件数据库"
                    + "正在后台重新加载。",
                    "路径设置");


                this.DialogResult =
                    DialogResult.OK;


                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "保存路径设置失败：\n"
                    + ex.Message,
                    "路径设置");
            }
        }


        private void btnDefault_Click(
            object sender,
            EventArgs e)
        {
            txtArchivePath.Text =
                AppPathConfig
                    .DefaultNonStandardArchivePath;


            txtStandardPartPath.Text =
                AppPathConfig
                    .DefaultStandardPartDatabasePath;
        }


        private void btnCancel_Click(
            object sender,
            EventArgs e)
        {
            this.Close();
        }
    }
}