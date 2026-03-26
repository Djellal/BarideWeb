namespace BarideWeb.Services
{
    public static class FileValidation
    {
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"
        };

        public const long MaxFileSize = 20 * 1024 * 1024; // 20 MB

        public static bool IsValidFile(IFormFile file)
        {
            if (file.Length == 0 || file.Length > MaxFileSize)
                return false;

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                return false;

            if (file.FileName.Contains("..") || file.FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return false;

            return true;
        }

        public static bool IsValidScannedExtension(string fileName)
        {
            var ext = Path.GetExtension(fileName);
            return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
        }
    }
}
