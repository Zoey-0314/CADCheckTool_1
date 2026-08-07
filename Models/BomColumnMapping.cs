namespace Correct_test1.Models
{

    /// <summary>
    /// BOM表字段所在列
    /// </summary>
    public class BomColumnMapping
    {

        public int NoColumn { get; set; } = -1;


        public int PartNumberColumn { get; set; } = -1;


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