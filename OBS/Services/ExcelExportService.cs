using System;
using System.Collections.Generic;
using System.Linq;
using OBS.Models;
using ClosedXML.Excel;

namespace OBS.Services
{
    public class ExcelExportService
    {
        /// <summary>
        /// Takım üyelerini Excel dosyasına aktarır.
        /// Kolonlar: SIRA | NO | AD | SOYAD | TC | Doğum T | VELİ NO | Cinsiyet
        /// Sıralama: Okul numarasına göre.
        /// </summary>
        public void ExportTeamToExcel(string teamName, IEnumerable<Student> members, IDictionary<int, string> guardianPhones, string destPath)
        {
            if (string.IsNullOrWhiteSpace(destPath))
                throw new ArgumentException("Hedef dosya yolu belirtilmedi.");

            var sorted = members
                .OrderBy(m => int.TryParse(m.StudentNumber, out var n) ? n : int.MaxValue)
                .ThenBy(m => m.StudentNumber)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add(teamName.Length > 31 ? teamName[..31] : teamName);

            var headers = new[] { "SIRA", "NO", "AD", "SOYAD", "TC", "DOĞUM T.", "VELİ NO", "CİNSİYET" };

            // Takım Adı Başlığı
            var titleCell = ws.Cell(1, 1);
            titleCell.Value = teamName;
            titleCell.Style.Font.Bold = true;
            titleCell.Style.Font.FontSize = 16;
            titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Range(1, 1, 1, headers.Length).Merge();
            ws.Row(1).Height = 30;

            // Sütun Başlıkları (2. satır)
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(2, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2D5A27");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Metin sütunlarına text format uygula (sayıya dönüşümü ve tarih yorumlamasını engeller)
            ws.Column(2).Style.NumberFormat.Format = "@"; // NO
            ws.Column(5).Style.NumberFormat.Format = "@"; // TC
            ws.Column(6).Style.NumberFormat.Format = "@"; // DOĞUM T.
            ws.Column(7).Style.NumberFormat.Format = "@"; // VELİ NO

            // Veri satırları (3. satırdan itibaren)
            for (int i = 0; i < sorted.Count; i++)
            {
                var s = sorted[i];
                int row = i + 3;

                ws.Cell(row, 1).Value = i + 1;
                ws.Cell(row, 2).SetValue(s.StudentNumber ?? "");
                ws.Cell(row, 3).Value = s.FirstName ?? "";
                ws.Cell(row, 4).Value = s.LastName ?? "";
                ws.Cell(row, 5).SetValue(s.TcNo ?? "");
                ws.Cell(row, 6).Value = s.BirthDate?.ToString("dd.MM.yyyy") ?? "-";

                string guardianPhone = "-";
                if (s.GuardianId.HasValue && guardianPhones.TryGetValue(s.GuardianId.Value, out var phone))
                    guardianPhone = phone;
                ws.Cell(row, 7).SetValue(guardianPhone);

                ws.Cell(row, 8).Value = s.Gender ?? "-";

                // Tüm hücreleri ortala
                for (int c = 1; c <= headers.Length; c++)
                    ws.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Zebra renklendirme
                if (i % 2 == 1)
                {
                    for (int c = 1; c <= headers.Length; c++)
                        ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#F0F4EF");
                }
            }

            // Kolon genişliklerini içeriğe göre ayarla, minimum genişlik garantile
            ws.Columns(1, headers.Length).AdjustToContents(2, sorted.Count + 2);
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 8);   // SIRA
            for (int c = 2; c <= headers.Length; c++)
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 16);

            workbook.SaveAs(destPath);
        }
    }
}
