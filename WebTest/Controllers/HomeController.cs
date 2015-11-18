using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using WebTest.Models;
namespace WebTest.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }

        private List<TestItemClass> CreateTestItems()
        {
            var resultsList = new List<TestItemClass>();
            for (int i = 0; i < 15; i++)
            {
                var a = new TestItemClass()
                {
                    Id = i,
                    Address = "Test Excel Address at " + i,
                    Money = 20000 + i * 10,
                    FullName = "Pham Hong Sang" + i
                };
                resultsList.Add(a);
            }
            return resultsList;
        }

        private Stream CreateExcelFile(Stream stream = null)
        {
            var list = CreateTestItems();
            using (var excelPackage = new ExcelPackage(stream ?? new MemoryStream()))
            {
                // Tạo author cho file Excel
                excelPackage.Workbook.Properties.Author = "Hanker";
                // Tạo title cho file Excel
                excelPackage.Workbook.Properties.Title = "EPP test background";
                // thêm tí comments vào làm màu 
                excelPackage.Workbook.Properties.Comments = "This is my fucking generated Comments";
                // Add Sheet vào file Excel
                excelPackage.Workbook.Worksheets.Add("First Sheet");
                // Lấy Sheet bạn vừa mới tạo ra để thao tác 
                var workSheet = excelPackage.Workbook.Worksheets[1];
                // Đỗ data vào Excel file
                workSheet.Cells[1, 1].LoadFromCollection(list, true, TableStyles.Dark9);
                BindingFormatForExcel(workSheet, list);
                excelPackage.Save();
                return excelPackage.Stream;
            }
        }

        private void BindingFormatForExcel(ExcelWorksheet worksheet, List<TestItemClass> listItems)
        {
            // Set default width cho tất cả column
            worksheet.DefaultColWidth = 10;
            // Tự động xuống hàng khi text quá dài
            worksheet.Cells.Style.WrapText = true;
            // Tạo header
            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Full Name";
            worksheet.Cells[1, 3].Value = "Address";
            worksheet.Cells[1, 4].Value = "Money";

            // Lấy range vào tạo format cho range đó ở đây là từ A1 tới D1
            using (var range = worksheet.Cells["A1:D1"])
            {
                // Set PatternType
                range.Style.Fill.PatternType = ExcelFillStyle.DarkGray;
                // Set Màu cho Background
                range.Style.Fill.BackgroundColor.SetColor(Color.Aqua);
                // Canh giữa cho các text
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                // Set Font cho text  trong Range hiện tại
                range.Style.Font.SetFromFont(new Font("Arial", 10));
                // Set Border
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thick;
                // Set màu ch Border
                range.Style.Border.Bottom.Color.SetColor(Color.Blue);
            }

            // Đỗ dữ liệu từ list vào 
            for (int i = 0; i < listItems.Count; i++)
            {
                var item = listItems[i];
                worksheet.Cells[i + 2, 1].Value = item.Id + 1;
                worksheet.Cells[i + 2, 2].Value = item.FullName;
                worksheet.Cells[i + 2, 3].Value = item.Address;
                worksheet.Cells[i + 2, 4].Value = item.Money;
                // Format lại color nếu như thỏa điều kiện
                if (item.Money > 20050)
                {
                    // Ở đây chúng ta sẽ format lại theo dạng fromRow,fromCol,toRow,toCol
                    using (var range = worksheet.Cells[i + 2, 1, i + 2, 4])
                    {
                        // Format text đỏ và đậm
                        range.Style.Font.Color.SetColor(Color.Red);
                        range.Style.Font.Bold = true;
                    }
                }

            }
            // Format lại định dạng xuất ra ở cột Money
            worksheet.Cells[2, 4, listItems.Count + 4, 4].Style.Numberformat.Format = "$#,##.00";
            // fix lại width của column với minimum width là 15
            worksheet.Cells[1, 1, listItems.Count + 5, 4].AutoFitColumns(15);

            // Thực hiện tính theo formula trong excel
            // Hàm Sum 
            worksheet.Cells[listItems.Count + 3, 3].Value = "Total is :";
            worksheet.Cells[listItems.Count + 3, 4].Formula = "SUM(D2:D" + (listItems.Count + 1) + ")";
            // Hàm SumIf
            worksheet.Cells[listItems.Count + 4, 3].Value = "Greater than 20050 :";
            worksheet.Cells[listItems.Count + 4, 4].Formula = "SUMIF(D2:D" + (listItems.Count + 1) + ",\">20050\")";
            // Tinh theo %
            worksheet.Cells[listItems.Count + 5, 3].Value = "Percentatge: ";
            worksheet.Cells[listItems.Count + 5, 4].Style.Numberformat.Format = "0.00%";
            // Dòng này có nghĩa là ở column hiện tại lấy với địa chỉ (Row hiện tại - 1)/ (Row hiện tại - 2) Cùng một colum
            worksheet.Cells[listItems.Count + 5, 4].FormulaR1C1 = "(R[-1]C/R[-2]C)";
        }

        private DataTable ReadFromExcelfile(string path, string sheetName)
        {
            // Khởi tạo data table
            DataTable dt = new DataTable();
            // Load file excel và các setting ban đầu
            using (ExcelPackage package = new ExcelPackage(new FileInfo(path)))
            {
                if (package.Workbook.Worksheets.Count < 1)
                {
                    // Log - Không có sheet nào tồn tại trong file excel của bạn
                    return null;
                }
                // Khởi Lấy Sheet đầu tiện trong file Excel để truy vấn, truyền vào name của Sheet để lấy ra sheet cần, nếu name = null thì lấy sheet đầu tiên
                ExcelWorksheet workSheet = package.Workbook.Worksheets.FirstOrDefault(x => x.Name == sheetName) ?? package.Workbook.Worksheets.FirstOrDefault();
                // Đọc tất cả các header
                foreach (var firstRowCell in workSheet.Cells[1, 1, 1, workSheet.Dimension.End.Column])
                {
                    dt.Columns.Add(firstRowCell.Text);
                }
                // Đọc tất cả data bắt đầu từ row thứ 2
                for (var rowNumber = 2; rowNumber <= workSheet.Dimension.End.Row; rowNumber++)
                {
                    // Lấy 1 row trong excel để truy vấn
                    var row = workSheet.Cells[rowNumber, 1, rowNumber, workSheet.Dimension.End.Column];
                    // tạo 1 row trong data table
                    var newRow = dt.NewRow();
                    foreach (var cell in row)
                    {
                        newRow[cell.Start.Column - 1] = cell.Text;

                    }
                    dt.Rows.Add(newRow);
                }
            }
            return dt;
        }

        [HttpGet]
        public ActionResult Export()
        {
            // Gọi lại hàm để tạo file excel
            var stream = CreateExcelFile();
            // Tạo buffer memory strean để hứng file excel
            var buffer = stream as MemoryStream;
            // Đây là content Type dành cho file excel, còn rất nhiều content-type khác nhưng cái này mình thấy okay nhất
            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            // Dòng này rất quan trọng, vì chạy trên firefox hay IE thì dòng này sẽ hiện Save As dialog cho người dùng chọn thư mục để lưu
            // File name của Excel này là ExcelDemo
            Response.AddHeader("Content-Disposition", "attachment; filename=ExcelDemo.xlsx");
            // Lưu file excel của chúng ta như 1 mảng byte để trả về response
            Response.BinaryWrite(buffer.ToArray());
            // Send tất cả ouput bytes về phía clients
            Response.Flush();
            Response.End();
            // Redirect về luôn trang index :D
            return RedirectToAction("Index");
        }
        [HttpGet]
        public ActionResult ReadFromExcel()
        {
            var data = ReadFromExcelfile(@"D:\ExcelDemo.xlsx", "First Sheet");
            return View(data);
        }
    }
}
