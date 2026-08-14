using System;
using System.Drawing;
using System.Windows.Forms;

namespace Correct_test1.QuickRevision.Forms
{
    /// <summary>
    /// 快速划改新文字输入窗口。
    ///
    /// 本窗体只负责：
    /// 1. 显示原内容
    /// 2. 接收新内容
    /// 3. 返回确定/取消
    ///
    /// 不负责任何AutoCAD数据库操作。
    /// </summary>
    public class RevisionInputForm : Form
    {
        private Label _originalTitleLabel;
        private Label _originalValueLabel;

        private Label _replacementTitleLabel;
        private TextBox _replacementTextBox;

        private Button _okButton;
        private Button _cancelButton;


        /// <summary>
        /// 原始内容。
        /// </summary>
        public string OriginalText
        {
            get;
            private set;
        }


        /// <summary>
        /// 用户输入的新内容。
        ///
        /// 只有用户点击确定后才使用。
        /// </summary>
        public string ReplacementText
        {
            get
            {
                if (_replacementTextBox == null)
                    return "";

                return
                    (_replacementTextBox.Text ?? "")
                    .Trim();
            }
        }


        public RevisionInputForm(
            string originalText)
        {
            OriginalText =
                originalText ?? "";

            InitializeForm();

            InitializeControls();

            ApplyOriginalText();
        }


        /// <summary>
        /// 初始化窗体本身。
        /// </summary>
        private void InitializeForm()
        {
            Text =
                "快速划改";

            StartPosition =
                FormStartPosition.CenterScreen;

            FormBorderStyle =
                FormBorderStyle.FixedDialog;

            MaximizeBox =
                false;

            MinimizeBox =
                false;

            ShowInTaskbar =
                false;

            ClientSize =
                new Size(
                    420,
                    190);

            Font =
                new Font(
                    "Microsoft YaHei UI",
                    9F,
                    FontStyle.Regular,
                    GraphicsUnit.Point);
        }


        /// <summary>
        /// 创建窗体控件。
        /// </summary>
        private void InitializeControls()
        {
            //--------------------------------
            // 原内容标题
            //--------------------------------

            _originalTitleLabel =
                new Label();

            _originalTitleLabel.Text =
                "原内容：";

            _originalTitleLabel.AutoSize =
                true;

            _originalTitleLabel.Location =
                new Point(
                    24,
                    25);


            //--------------------------------
            // 原内容显示
            //--------------------------------

            _originalValueLabel =
                new Label();

            _originalValueLabel.AutoEllipsis =
                true;

            _originalValueLabel.BorderStyle =
                BorderStyle.FixedSingle;

            _originalValueLabel.Location =
                new Point(
                    95,
                    20);

            _originalValueLabel.Size =
                new Size(
                    295,
                    28);

            _originalValueLabel.TextAlign =
                ContentAlignment.MiddleLeft;


            //--------------------------------
            // 新内容标题
            //--------------------------------

            _replacementTitleLabel =
                new Label();

            _replacementTitleLabel.Text =
                "新内容：";

            _replacementTitleLabel.AutoSize =
                true;

            _replacementTitleLabel.Location =
                new Point(
                    24,
                    78);


            //--------------------------------
            // 新文字输入框
            //--------------------------------

            _replacementTextBox =
                new TextBox();

            _replacementTextBox.Location =
                new Point(
                    95,
                    74);

            _replacementTextBox.Size =
                new Size(
                    295,
                    25);

            _replacementTextBox.TabIndex =
                0;


            //--------------------------------
            // 确定按钮
            //--------------------------------

            _okButton =
                new Button();

            _okButton.Text =
                "确定";

            _okButton.Size =
                new Size(
                    90,
                    32);

            _okButton.Location =
                new Point(
                    200,
                    130);

            _okButton.TabIndex =
                1;

            _okButton.Click +=
                OkButton_Click;


            //--------------------------------
            // 取消按钮
            //--------------------------------

            _cancelButton =
                new Button();

            _cancelButton.Text =
                "取消";

            _cancelButton.Size =
                new Size(
                    90,
                    32);

            _cancelButton.Location =
                new Point(
                    300,
                    130);

            _cancelButton.TabIndex =
                2;

            _cancelButton.DialogResult =
                DialogResult.Cancel;


            //--------------------------------
            // Enter / Esc
            //--------------------------------

            AcceptButton =
                _okButton;

            CancelButton =
                _cancelButton;


            //--------------------------------
            // 添加控件
            //--------------------------------

            Controls.Add(
                _originalTitleLabel);

            Controls.Add(
                _originalValueLabel);

            Controls.Add(
                _replacementTitleLabel);

            Controls.Add(
                _replacementTextBox);

            Controls.Add(
                _okButton);

            Controls.Add(
                _cancelButton);


            //--------------------------------
            // 打开窗口后直接输入
            //--------------------------------

            Shown +=
                RevisionInputForm_Shown;
        }


        /// <summary>
        /// 显示原始文字。
        /// </summary>
        private void ApplyOriginalText()
        {
            _originalValueLabel.Text =
                OriginalText;
        }


        /// <summary>
        /// 窗体打开后自动把光标放到输入框。
        /// </summary>
        private void RevisionInputForm_Shown(
            object sender,
            EventArgs e)
        {
            _replacementTextBox.Focus();

            _replacementTextBox.SelectAll();
        }


        /// <summary>
        /// 点击确定。
        /// </summary>
        private void OkButton_Click(
            object sender,
            EventArgs e)
        {
            string newText =
                ReplacementText;


            //--------------------------------
            // 不允许空内容。
            //--------------------------------

            if (string.IsNullOrWhiteSpace(
                    newText))
            {
                MessageBox.Show(
                    this,
                    "请输入新的划改内容。",
                    "快速划改",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _replacementTextBox.Focus();

                return;
            }


            //--------------------------------
            // 新旧内容完全一样时不允许继续。
            //--------------------------------

            if (string.Equals(
                    OriginalText,
                    newText,
                    StringComparison.Ordinal))
            {
                MessageBox.Show(
                    this,
                    "新内容与原内容相同，请重新输入。",
                    "快速划改",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _replacementTextBox.Focus();

                _replacementTextBox.SelectAll();

                return;
            }


            DialogResult =
                DialogResult.OK;

            Close();
        }
    }
}