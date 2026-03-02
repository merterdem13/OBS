using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AlparslanOBS.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Xobject;

namespace AlparslanOBS.Services
{
    public enum PdfDocumentType
    {
        Unknown,
        KunyeDefteri,
        SinifListesi,
        ResimListesi
    }

    public interface IPdfExtractionService
    {
        PdfDocumentType GetPdfDocumentType(string pdfPath);
        string? ExtractClassHeader(string pdfPath);
        IEnumerable<Student> ExtractStudentsFromKunyePdf(string pdfPath);
        IEnumerable<(string StudentNumber, string Class, string Gender)> ExtractClassList(string pdfPath);
        IEnumerable<string> SplitAndSaveStudentPdfs(string pdfPath, string outputFolder);
        void CleanupTempFiles();
    }

    public class PdfExtractionService : IPdfExtractionService
    {
        private readonly ConcurrentBag<string> _tempFilesCreated = new();

        public PdfDocumentType GetPdfDocumentType(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return PdfDocumentType.Unknown;

            try
            {
                using var pdfReader = new PdfReader(pdfPath);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var text = ExtractTextFromPage(pdfDocument.GetPage(pageNum));
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    if (text.Contains("ÖĞRENCİ KÜNYE DEFTERİ", StringComparison.OrdinalIgnoreCase))
                        return PdfDocumentType.KunyeDefteri;

                    if (Regex.IsMatch(text, @"Sınıf\s+Listesi", RegexOptions.IgnoreCase))
                        return PdfDocumentType.SinifListesi;

                    if (ExtractClassFromHeader(text) != null)
                        return PdfDocumentType.ResimListesi;
                }
            }
            catch
            {
                return PdfDocumentType.Unknown;
            }

            return PdfDocumentType.Unknown;
        }

        public string? ExtractClassHeader(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return null;

            try
            {
                using var pdfReader = new PdfReader(pdfPath);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    var text = ExtractTextFromPage(pdfDocument.GetPage(pageNum));
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    var classInfo = ExtractClassFromHeader(text);
                    if (!string.IsNullOrWhiteSpace(classInfo))
                        return classInfo;
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public IEnumerable<Student> ExtractStudentsFromKunyePdf(string pdfPath)
        {
            var results = new List<Student>();

            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return results;

            try
            {
                using var pdfReader = new PdfReader(pdfPath);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    try
                    {
                        var page = pdfDocument.GetPage(pageNum);
                        var text = ExtractTextFromPage(page);

                        if (string.IsNullOrWhiteSpace(text)) continue;

                        var (firstName, lastName) = FindNameParts(text);

                        var student = new Student
                        {
                            StudentNumber = FindStudentNumber(text) ?? string.Empty,
                            TcNo = FindTcNo(text) ?? string.Empty,
                            BirthDate = FindBirthDate(text),
                            FirstName = firstName ?? string.Empty,
                            LastName = lastName ?? string.Empty
                        };

                        var imageData = ExtractImageFromPage(page);
                        if (imageData != null && imageData.Length > 0)
                        {
                            string tempFileName = Path.Combine(Path.GetTempPath(), $"OBS_Temp_{Guid.NewGuid()}.jpg");
                            File.WriteAllBytes(tempFileName, imageData);

                            student.PhotoPath = tempFileName;
                            _tempFilesCreated.Add(tempFileName);
                        }

                        results.Add(student);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Page extraction error: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PDF Ayrıştırma Hatası: {ex.Message}");
            }

            return results;
        }

        public IEnumerable<string> SplitAndSaveStudentPdfs(string pdfPath, string outputFolder)
        {
            var savedPaths = new List<string>();

            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return savedPaths;

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            System.Diagnostics.Debug.WriteLine($"\n=== PDF SPLIT BAŞLADI ===");
            System.Diagnostics.Debug.WriteLine($"Kaynak: {pdfPath}");
            System.Diagnostics.Debug.WriteLine($"Hedef: {outputFolder}");

            try
            {
                var pageData = new List<(int pageNum, string firstName, string lastName, string studentNumber)>();
                
                using (var reader = new PdfReader(pdfPath))
                using (var doc = new PdfDocument(reader))
                {
                    int totalPages = doc.GetNumberOfPages();
                    System.Diagnostics.Debug.WriteLine($"Toplam sayfa: {totalPages}");
                    
                    for (int i = 1; i <= totalPages; i++)
                    {
                        try
                        {
                            var text = ExtractTextFromPage(doc.GetPage(i));
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                var (firstName, lastName) = FindNameParts(text);
                                var studentNumber = FindStudentNumber(text);
                                pageData.Add((i, firstName ?? "", lastName ?? "", studentNumber ?? ""));
                                System.Diagnostics.Debug.WriteLine($"Sayfa {i}: {studentNumber} - {firstName} {lastName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Sayfa {i} okuma hatası: {ex.Message}");
                        }
                    }
                }

                // Kaynak PDF'i tek seferde aç — N sayfa için N ayrı PdfReader yerine 1 adet.
                // 30 sayfalık PDF'de ~%95 I/O azalması sağlar.
                using var sharedReader = new PdfReader(pdfPath);
                using var sharedSourceDoc = new PdfDocument(sharedReader);

                foreach (var (pageNum, firstName, lastName, studentNumber) in pageData)
                {
                    try
                    {
                        var studentFileName = FileNameHelper.BuildStudentFileName(studentNumber, firstName, lastName);
                        var outputPath = Path.Combine(outputFolder, $"{studentFileName}.pdf");

                        int counter = 1;
                        var originalPath = outputPath;
                        while (File.Exists(outputPath))
                        {
                            var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
                            outputPath = Path.Combine(outputFolder, $"{nameWithoutExt}_{counter}.pdf");
                            counter++;
                        }

                        using (var writer = new PdfWriter(outputPath))
                        {
                            writer.SetCloseStream(true);
                            using var targetDoc = new PdfDocument(writer);
                            sharedSourceDoc.CopyPagesTo(pageNum, pageNum, targetDoc);
                        }

                        if (File.Exists(outputPath))
                        {
                            savedPaths.Add(outputPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Sayfa {pageNum} kaydetme hatası: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ PDF Split genel hata: {ex.Message}");
            }

            return savedPaths;
        }

        public IEnumerable<(string StudentNumber, string Class, string Gender)> ExtractClassList(string pdfPath)
        {
            var results = new List<(string, string, string)>();

            if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
                return results;

            try
            {
                using var pdfReader = new PdfReader(pdfPath);
                using var pdfDocument = new PdfDocument(pdfReader);

                for (int pageNum = 1; pageNum <= pdfDocument.GetNumberOfPages(); pageNum++)
                {
                    try
                    {
                        var page = pdfDocument.GetPage(pageNum);
                        var text = ExtractTextFromPage(page);

                        if (string.IsNullOrWhiteSpace(text)) continue;

                        var classInfo = ExtractClassFromHeader(text);
                        if (string.IsNullOrEmpty(classInfo)) continue;

                        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            // Başlık ve meta satırlarını atla
                            if (IsClassListHeaderLine(line)) continue;

                            var sn = FindStudentNumberFromClassListLine(line);
                            if (!string.IsNullOrEmpty(sn))
                            {
                                var gender = ExtractGenderFromClassListLine(line);
                                results.Add((sn, classInfo, gender));
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sınıf Listesi Ayrıştırma Hatası: {ex.Message}");
            }

            return results;
        }

        public void CleanupTempFiles()
        {
            while (_tempFilesCreated.TryTake(out var tempFile))
            {
                if (File.Exists(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }
            }
        }

        private static string ExtractTextFromPage(PdfPage page)
        {
            try
            {
                return PdfTextExtractor.GetTextFromPage(page) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static byte[]? ExtractImageFromPage(PdfPage page)
        {
            try
            {
                var resources = page.GetResources();
                if (resources == null) return null;

                var xObjectResource = resources.GetResource(new PdfName("XObject"));
                if (!(xObjectResource is PdfDictionary xObjects) || xObjects.IsEmpty()) return null;

                byte[]? largestImage = null;
                int largestSize = 0;

                foreach (var xObjectName in xObjects.KeySet())
                {
                    var obj = xObjects.Get(xObjectName);
                    if (obj is PdfStream stream)
                    {
                        var subtype = stream.GetAsName(new PdfName("Subtype"));
                        if (PdfName.Image.Equals(subtype))
                        {
                            try
                            {
                                var imageXObject = new PdfImageXObject(stream);
                                byte[] imageBytes = imageXObject.GetImageBytes();

                                if (imageBytes != null && imageBytes.Length > largestSize)
                                {
                                    largestSize = imageBytes.Length;
                                    largestImage = imageBytes;
                                }
                            }
                            catch { continue; }
                        }
                    }
                }
                return largestImage;
            }
            catch
            {
                return null;
            }
        }

        #region Heuristics (Metin Arama Algoritmaları)

        private static string? FindStudentNumber(string text)
        {
            var match = Regex.Match(text, @"(?:Okul|Öğrenci|Ogrenci|Ögrenci)\s*No\s*[:\-]?\s*([0-9,]+)", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var rawValue = match.Groups[1].Value.Trim();
            var normalized = FileNameHelper.NormalizeStudentNumber(rawValue);
            return string.IsNullOrEmpty(normalized) ? null : normalized;
        }

        private static (string? FirstName, string? LastName) FindNameParts(string text)
        {
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var firstName = CleanNameValue(FindLabeledValue(lines, "Adı"));
            var lastName = FindLastName(lines);

            return (firstName, lastName);
        }

        private static string? FindLastName(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (Regex.IsMatch(line, @"^\s*Soyadı\s*[:|-]?\s*$", RegexOptions.IgnoreCase))
                {
                    if (i > 0)
                    {
                        var prevLine = lines[i - 1].Trim();
                        
                        if (!Regex.IsMatch(prevLine, @"(Adı|Baba|Anne|Okul)", RegexOptions.IgnoreCase) && 
                            !prevLine.EndsWith(":"))
                        {
                            return CleanNameValue(prevLine);
                        }
                    }

                    var match = Regex.Match(line, @"Soyadı\s*[:|-]\s*(.+?)$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        return CleanNameValue(match.Groups[1].Value.Trim());
                    }
                }
                else if (Regex.IsMatch(line, @"Soyadı\s*[:|-]\s*(.+)", RegexOptions.IgnoreCase))
                {
                    var match = Regex.Match(line, @"Soyadı\s*[:|-]\s*(.+?)(?:\s{2,}|$)", RegexOptions.IgnoreCase);
                    if (match.Success)
                        return CleanNameValue(match.Groups[1].Value.Trim());
                }
            }

            return null;
        }

        private static string? FindLabeledValue(string[] lines, string label)
        {
            foreach (var line in lines)
            {
                string? value = null;

                if (label.Equals("Adı", StringComparison.OrdinalIgnoreCase))
                {
                    if (Regex.IsMatch(line, @"(Baba|Anne)\s+Adı", RegexOptions.IgnoreCase))
                        continue;

                    var match = Regex.Match(line, @"Adı\s*[:\-]\s*(.+?)(?:\s{2,}|$)", RegexOptions.IgnoreCase);
                    if (match.Success)
                        value = match.Groups[1].Value.Trim();
                }

                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return null;
        }

        private static string? CleanNameValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            if (Regex.IsMatch(value, @"Bursluluk", RegexOptions.IgnoreCase))
                return null;

            var match = Regex.Match(value, @"[A-ZİIŞÇĞÜÖa-zışçğüö\s]+", RegexOptions.IgnoreCase);
            return match.Success ? match.Value.Trim() : value.Trim();
        }

        private static string? FindTcNo(string text)
        {
            var match = Regex.Match(text, @"T\.?\s*C\.?\s*Kimlik\s*No\s*[:\-]?\s*(\d{11})", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            return match.Groups[1].Value.Trim();
        }

        private static DateTime? FindBirthDate(string text)
        {
            var match = Regex.Match(text,
                @"Doğum\s*Yeri\s*,?\s*Tarihi\s*[:\-]\s*(?:[A-ZİIŞÇĞÜÖa-zışçğüö\s]+)?(\d{1,2}[./-]\d{1,2}[./-]\d{2,4})",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                var dateStr = match.Groups[1].Value.Trim();
                if (TryParseDate(dateStr, out var dt))
                    return dt;
            }

            return null;
        }

        private static bool TryParseDate(string dateStr, out DateTime result)
        {
            if (DateTime.TryParse(dateStr, out result))
                return true;

            var parts = dateStr.Split(new[] { '.', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int day) &&
                int.TryParse(parts[1], out int month) &&
                int.TryParse(parts[2], out int year))
            {
                if (year < 100)
                    year += 2000;

                try
                {
                    result = new DateTime(year, month, day);
                    return true;
                }
                catch
                {
                    result = default;
                    return false;
                }
            }

            result = default;
            return false;
        }

        private static string? ExtractClassFromHeader(string text)
        {
            var m = Regex.Match(text, @"(\d+)\.\s*(?:Sınıf|Sinif)\s*(?:/\s*)?([A-Za-z])\s*(?:Şubesi|Subesi)?", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                var classNum = m.Groups[1].Value.Trim();
                var section = m.Groups[2].Value.Trim().ToUpperInvariant();
                return $"{classNum}/{section}";
            }

            m = Regex.Match(text, @"(\d+)\.\s*(?:Sınıf|Sinif)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                return m.Groups[1].Value.Trim();
            }

            return null;
        }

        /// <summary>
        /// Sınıf listesi başlık/meta satırlarını filtreler.
        /// Bu satırlar öğrenci verisi içermez, sayısal değerleri yanlış eşleşme yaratır.
        /// </summary>
        private static bool IsClassListHeaderLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return true;

            var trimmed = line.Trim();

            // Çok kısa satırlar (sayfa no, sıra no gibi)
            if (trimmed.Length < 5) return true;

            // Başlık anahtar kelimeleri
            if (Regex.IsMatch(trimmed, @"(Sınıf\s*(Listesi)?|Şubesi|Okul\s*No|Öğrenci\s*No|Adı|Soyadı|Cinsiyet|Sıra)", RegexOptions.IgnoreCase))
                return true;

            // Sadece rakam ve boşluk içeren satırlar (sayfa numarası vb.)
            if (Regex.IsMatch(trimmed, @"^\d+$"))
                return true;

            return false;
        }

        private static string? FindStudentNumberFromClassListLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            // Boşluksuz ardışık rakam gruplarını yakala.
            // Eski regex (\d[\d\s\.,]*\d) satır numarasını öğrenci numarasıyla
            // birleştiriyordu: "1  567" → "1567" (yanlış eşleşme).
            var matches = Regex.Matches(line, @"\d[\d,\.]*\d|\d");
            if (matches.Count == 0) return null;

            string? best = null;
            foreach (Match match in matches)
            {
                var normalized = FileNameHelper.NormalizeStudentNumber(match.Value);
                if (string.IsNullOrEmpty(normalized)) continue;

                // Sıra numarası (1, 2, 3...) ve TC no (11 hane) filtreleme:
                // Öğrenci numaraları genelde 2-7 haneli olur.
                if (normalized.Length < 2 || normalized.Length > 7) continue;

                if (best == null || normalized.Length > best.Length)
                    best = normalized;
            }

            return best;
        }

        /// <summary>
        /// Sınıf listesi satırının son sütunundan cinsiyet verisini çıkarır.
        /// Örnek: "12 Ahmet Yılmaz         Erkek" → "Erkek"
        /// </summary>
        private static string ExtractGenderFromClassListLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return string.Empty;

            var trimmedLine = line.TrimEnd();
            var words = trimmedLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (words.Length == 0)
                return string.Empty;

            var lastWord = words[^1];

            if (lastWord.Equals("Kız", StringComparison.OrdinalIgnoreCase))
                return "Kız";
            else if (lastWord.Equals("Erkek", StringComparison.OrdinalIgnoreCase))
                return "Erkek";

            return string.Empty;
        }
        #endregion
    }
}
