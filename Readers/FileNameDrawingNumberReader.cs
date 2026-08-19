using System;
using System.IO;

namespace Correct_test1.Readers
{
    /// <summary>
    /// 从文件名读取图号（文件名去掉扩展名后，第一个空格前的内容）
    /// </summary>
    public class FileNameDrawingNumberReader
    {
        public string ReadDrawingNumber(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "";

            string fileName = Path.GetFileNameWithoutExtension(filePath) ?? "";

            if (string.IsNullOrWhiteSpace(fileName))
                return "";

            int idx = fileName.IndexOf(' ');
            if (idx <= 0)
            {
                // 如果没有空格，或者空格在开头，返回整个文件名（若为空则返回空串）
                return fileName.Trim();
            }

            string firstPart = fileName.Substring(0, idx).Trim();
            return string.IsNullOrEmpty(firstPart) ? "" : firstPart;
        }
    }
}
