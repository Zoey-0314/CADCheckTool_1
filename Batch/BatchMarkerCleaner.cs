using Autodesk.AutoCAD.DatabaseServices;

using Correct_test1.Markers;

using System;
using System.Collections.Generic;
using System.IO;


namespace Correct_test1.Batch
{

    public class BatchMarkerCleaner
    {

        /// <summary>
        /// 清除指定文件夹内所有DWG的检查标记
        /// 
        /// 删除：
        /// REVISION_CHECK图层中的所有实体
        /// 
        /// 保留：
        /// 原图其他内容
        /// </summary>
        
        public List<string> ClearFolderMarkers(
            string folderPath)
        {

            List<string> results =
                new List<string>();

            if (!Directory.Exists(folderPath))
                return results;

            string[] files =
                Directory.GetFiles(
                    folderPath,
                    "*.dwg",
                    SearchOption.AllDirectories
                );

            foreach (string file in files)
            {

                Database db = null;

                try
                {

                    db =
                        new Database(
                            false,
                            true
                        );

                    db.ReadDwgFile(
                        file,
                        FileOpenMode.OpenForReadAndAllShare,
                        false,
                        ""
                    );

                    db.CloseInput(true);

                    RevisionMarker marker =
                        new RevisionMarker();

                    marker.ClearMarkers(
                        db
                    );
                    TitleBlockDrawingNumberMarker titleBlockMarker =
    new TitleBlockDrawingNumberMarker();


                    titleBlockMarker.ClearMarkers(
                        db
                    );


                    // 保存清除后的DWG

                    db.SaveAs(
                        file,
                        DwgVersion.Current
                    );

                    results.Add(
                        file
                    );

                }
                catch (Exception)
                {

                    // 单个文件失败不影响其他文件

                    continue;

                }
                finally
                {

                    if (db != null)
                    {

                        db.Dispose();

                    }

                }

            }

            return results;

        }

    }

}