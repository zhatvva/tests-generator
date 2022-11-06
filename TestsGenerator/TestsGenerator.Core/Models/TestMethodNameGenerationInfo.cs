namespace TestsGenerator.Core.Models
{
    internal class TestMethodNameGenerationInfo
    {
        public int MethodsWithTheSameNameCount { get; set; }
        public int LastGenerationNumber { get; set; } = 0;

        public TestMethodNameGenerationInfo(int mathodsWithTheSameNameCount)
        {
            MethodsWithTheSameNameCount = mathodsWithTheSameNameCount;
        }
    }
}
