namespace TestsGenerator.Console
{
    internal static class IO
    {
        public static async Task<string> ReadAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                throw new ArgumentException("Invalid file path", nameof(filePath));
            }

            using var reader = new StreamReader(filePath);
            return await reader.ReadToEndAsync();
        }

        public static async Task WriteAsync(string filePath, string content)
        {
            using var writer = new StreamWriter(filePath);
            await writer.WriteAsync(content);
        }
    }
}
