using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace AlparslanOBS.Services
{
    internal static class FileNameHelper
    {
        public static string NormalizeStudentNumber(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input, "\\D", string.Empty);
        }

        public static string BuildStudentFileName(string studentNumber, string firstName, string lastName)
        {
            var normalizedNumber = NormalizeStudentNumber(studentNumber);
            var fullName = $"{firstName} {lastName}".Trim();
            var namePart = SlugifyFilePart(fullName);

            if (string.IsNullOrEmpty(normalizedNumber)) return namePart;
            if (string.IsNullOrEmpty(namePart)) return normalizedNumber;

            return $"{normalizedNumber}-{namePart}";
        }

        public static string BuildClassListFileName(string classInfo, bool isImageList)
        {
            var normalizedClass = (classInfo ?? string.Empty)
                .Replace("/", "-")
                .Replace(" ", string.Empty)
                .ToUpperInvariant();

            var suffix = isImageList ? "Resim_Listesi" : "Listesi";
            if (string.IsNullOrEmpty(normalizedClass)) return suffix;

            return $"{normalizedClass}-{suffix}";
        }

        private static string SlugifyFilePart(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var map = new Dictionary<char, char>
            {
                ['ç'] = 'c',
                ['Ç'] = 'C',
                ['ş'] = 's',
                ['Ş'] = 'S',
                ['ğ'] = 'g',
                ['Ğ'] = 'G',
                ['ü'] = 'u',
                ['Ü'] = 'U',
                ['ö'] = 'o',
                ['Ö'] = 'O',
                ['ı'] = 'i',
                ['İ'] = 'I'
            };

            var normalized = new StringBuilder();
            foreach (var ch in input)
            {
                if (map.TryGetValue(ch, out var rep)) normalized.Append(rep);
                else normalized.Append(ch);
            }

            var s = normalized.ToString().Trim();
            foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '-');
            s = Regex.Replace(s, "\\s+", "-");
            s = Regex.Replace(s, "-{2,}", "-");
            return s.Trim('-').ToLowerInvariant();
        }
    }
}
