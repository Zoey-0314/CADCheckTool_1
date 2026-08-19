namespace Correct_test1.Models
{

    /// <summary>
    /// BOM表字段所在列
    /// </summary>
    public class BomColumnMapping
    {

        public int NoColumn { get; set; } = -1;


        public int PartNumberColumn { get; set; } = -1;

        /// <summary>
        /// 可选的P/N列。
        /// 有些BOM没有这一列，
        /// 所以不能加入IsValid()必填判断。
        /// </summary>
        public int PartNumberSuffixColumn
        {
            get;
            set;
        }
        =
        -1;


        public int NameColumn { get; set; } = -1;


        public int QuantityColumn { get; set; } = -1;


        /// <summary>
        /// 判断映射是否完整
        /// </summary>
        public bool IsValid()
        {

            return
                NoColumn >= 0 &&
                PartNumberColumn >= 0 &&
                NameColumn >= 0 &&
                QuantityColumn >= 0;

        }

    }

}