namespace Correct_test1.Models
{


    /// <summary>
    /// 图纸修改记录信息
    /// 对应标题栏中的：
    /// 标记
    /// 更改内容
    /// 更改日期
    /// 签名
    /// 变更号
    /// </summary>
    public class RevisionInfo
    {


        /// <summary>
        /// 修改标记
        /// 例如:
        /// 1
        /// 2
        /// A
        /// </summary>
        public string Mark
        {
            get;
            set;
        }





        /// <summary>
        /// 修改内容
        /// </summary>
        public string Description
        {
            get;
            set;
        }





        /// <summary>
        /// 修改日期
        /// </summary>
        public string Date
        {
            get;
            set;
        }





        /// <summary>
        /// 修改人员签名
        /// </summary>
        public string Signer
        {
            get;
            set;
        }





        /// <summary>
        /// 变更号
        /// </summary>
        public string RevisionNumber
        {
            get;
            set;
        }





        public RevisionInfo()
        {

            Mark = "";

            Description = "";

            Date = "";

            Signer = "";

            RevisionNumber = "";

        }



    }

}