using Correct_test1.Models;
using System;
using System.Collections.Generic;


namespace Correct_test1.Checks
{

    /// <summary>
    /// BOM提取器
    ///
    /// 一个DWG可以存在多个BOM
    /// </summary>
    public class BomExtractor
    {


        private readonly BomTableRecognizer recognizer;


        public BomExtractor()
        {

            recognizer =
                new BomTableRecognizer();

        }



        /// <summary>
        /// 从多个CAD表中提取所有BOM
        /// </summary>
        public List<BomData> Extract(
            List<CadTableData> tables)
        {


            List<BomData> result =
                new List<BomData>();


            if (tables == null)
                return result;



            foreach (CadTableData table in tables)
            {


                //判断是否BOM

                if (!recognizer.IsBom(table))
                {
                    continue;
                }



                BomData bom =
                    recognizer.Parse(table);



                result.Add(bom);

            }



            return result;

        }


    }

}