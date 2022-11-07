using TestsGenerator.Core.Models;

namespace TestsGenerator.Core.Interfaces
{
    public interface ITestsGenerator
    {
        public List<GenerationResult> Generate(string file);
    }
}
