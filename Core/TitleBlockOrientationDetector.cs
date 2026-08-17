using Correct_test1.Models;

using System.Collections.Generic;

namespace Correct_test1.Core
{
    /// <summary>
    /// 标题栏横版 / 竖版判断。
    ///
    /// 统一复用项目现有规则：
    ///
    /// “标记”文字数量 >= 2
    ///     → 横版
    ///
    /// 否则
    ///     → 竖版
    /// </summary>
    public static class TitleBlockOrientationDetector
    {
        public static bool IsHorizontal(
            List<TitleText> texts)
        {
            if (texts == null)
                return false;


            int markCount = 0;


            foreach (TitleText text in texts)
            {
                if (text == null ||
                    string.IsNullOrWhiteSpace(
                        text.Text))
                {
                    continue;
                }


                if (text.Text.Contains(
                        "标记"))
                {
                    markCount++;
                }
            }


            return markCount >= 2;
        }
    }
}