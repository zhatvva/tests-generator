using TestsGenerator.Core.Models;

namespace TestsGenerator.Core.Interfaces
{
    public interface ITestsGenerator
    {
        public Task<List<GenerationResult>> Generate(string file);
    }
}
