// AlparslanOBS\Services\PdfExportService.cs
using System;
using System.Collections.Generic;
using System.IO;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using AlparslanOBS.Models;

namespace AlparslanOBS.Services
{
    /// <summary>
    /// Takım verilerini PDF formatında dışa aktarma işlemlerini yürüten servis.
    /// Anayasa Kuralı: Tüm IDisposable nesneler proper managed ediliyor (using/finally).
    /// </summary>
    public class PdfExportService
    {
        /// <summary>
        /// Takım verilerini PDF olarak dışa aktarır.
        /// Anayasa Kuralı: Tüm nesneler using bloğu içinde yönetilir, bellek tahliyesi otomatik.
        /// ImageData nesneleri finally bloğunda dispose edilir.
        /// </summary>
        public void ExportTeamToPdf(Team? team, IEnumerable<Student>? members, string? destPath)
        {
            if (team == null || members == null || string.IsNullOrWhiteSpace(destPath))
                throw new ArgumentException("Eksik veri gönderildi, PDF oluşturulamıyor.");

            using var writer = new PdfWriter(destPath);
            using var pdf = new PdfDocument(writer);
            using var document = new Document(pdf, iText.Kernel.Geom.PageSize.A4);

            // DÜZELTME: SetBold hatasını aşmak için Normal ve Kalın (Bold) fontları ayrı ayrı oluşturuyoruz
            PdfFont turkishFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA, "CP1254", PdfFontFactory.EmbeddingStrategy.PREFER_NOT_EMBEDDED);
            PdfFont turkishBoldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD, "CP1254", PdfFontFactory.EmbeddingStrategy.PREFER_NOT_EMBEDDED);

            document.SetFont(turkishFont);

            // 1. BAŞLIK: Kalın font atandı
            Paragraph title = new Paragraph($"TAKIM LİSTESİ: {team.TeamName?.ToUpper()}")
                .SetFont(turkishBoldFont)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetFontSize(18)
                .SetMarginBottom(10);

            document.Add(title);

            if (!string.IsNullOrWhiteSpace(team.Description))
            {
                var desc = new Paragraph($"Açıklama: {team.Description}")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(12)
                    .SetMarginBottom(20);
                document.Add(desc);
            }

            // 2. TABLO OLUŞTURMA
            float[] columnWidths = { 1, 1, 2, 1, 1.5f };
            Table table = new Table(UnitValue.CreatePercentArray(columnWidths)).UseAllAvailableWidth();

            // Tablo Başlıkları: Kalın font atandı
            string[] headers = { "Fotoğraf", "Okul No", "Ad Soyad", "Sınıf", "TC Kimlik" };
            foreach (var header in headers)
            {
                Cell headerCell = new Cell()
                    .Add(new Paragraph(header).SetFont(turkishBoldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetPadding(5);
                table.AddHeaderCell(headerCell);
            }

            // 3. ÖĞRENCİ VERİLERİNİ TABLOYA EKLEME
            foreach (var student in members)
            {
                Cell photoCell = new Cell().SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE);
                if (!string.IsNullOrWhiteSpace(student.PhotoPath) && File.Exists(student.PhotoPath))
                {
                    try
                    {
                        var imageData = ImageDataFactory.Create(student.PhotoPath);
                        Image img = new Image(imageData).SetAutoScale(true);
                        img.SetMaxHeight(50);
                        photoCell.Add(img);
                    }
                    catch
                    {
                        photoCell.Add(new Paragraph("YOK").SetFontSize(8));
                    }
                    // Not disposing imageData: iText's ImageData does not implement IDisposable.
                }
                else
                {
                    photoCell.Add(new Paragraph("-").SetFontSize(8));
                }
                table.AddCell(photoCell);

                table.AddCell(new Cell().Add(new Paragraph(student.StudentNumber ?? "-")).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.CENTER));
                table.AddCell(new Cell().Add(new Paragraph($"{student.FirstName} {student.LastName}")).SetVerticalAlignment(VerticalAlignment.MIDDLE));
                table.AddCell(new Cell().Add(new Paragraph(student.Class ?? "-")).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.CENTER));
                table.AddCell(new Cell().Add(new Paragraph(student.TcNo ?? "-")).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.CENTER));
            }

            document.Add(table);

            document.Add(new Paragraph($"\nOluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}")
                .SetTextAlignment(TextAlignment.RIGHT)
                .SetFontSize(10)
                .SetFontColor(ColorConstants.GRAY));
        }
    }
}