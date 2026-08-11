using System;
using System.IO;


namespace Correct_test1.Batch
{

    public class DrawingWeightCalculator
    {

        public double Calculate(
            string file)
        {

            try
            {
                // 旧方式为了计算进度打开并解析DWG，会造成一次额外解析。
                // 新方式使用文件大小估算权重，避免重复加载DWG。
                FileInfo info = new FileInfo(file);
                double weight = info.Length / 1024.0 / 1024.0;

                // 防止空文件或极小文件产生零权重。
                return weight > 0 ? weight : 1;
            }
            catch (Exception)
            {
                // 文件信息读取失败时使用基础权重，不影响正式检查流程。
                return 1;
            }
        }
    }
}