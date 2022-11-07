using System.Threading.Tasks.Dataflow;
using TestsGenerator.Core.Interfaces;
using TestsGenerator.Core.Models;

namespace TestsGenerator.Console
{
    internal class Pipeline
    {
        private readonly string _outputDirectory;
        private readonly int _maxFilesReadingParallel;
        private readonly int _maxFilesWritingParallel;
        private readonly int _maxFilesParsingParallel;

        public Pipeline(string outputDirectory, int maxFilesReadingParallel, 
            int maxFilesWritingParallel, int maxFilesParsingParallel)
        {
            if (maxFilesReadingParallel <= 0)
            {
                throw new ArgumentException($"Should be greater than 0", nameof(maxFilesReadingParallel));
            }
            
            if (maxFilesReadingParallel <= 0)
            {
                throw new ArgumentException($"Should be greater than 0", nameof(maxFilesWritingParallel));
            }

            if (maxFilesReadingParallel <= 0)
            {
                throw new ArgumentException($"Should be greater than 0", nameof(maxFilesParsingParallel));
            }

            _outputDirectory = outputDirectory;
            _maxFilesReadingParallel = maxFilesReadingParallel;
            _maxFilesWritingParallel = maxFilesWritingParallel;
            _maxFilesParsingParallel = maxFilesParsingParallel;
        }

        public TransformBlock<string, string> GeneratePipeline(ITestsGenerator generator)
        {
            var readFile = new TransformBlock<string, string>
            (
                async path => await IO.ReadAsync(path),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _maxFilesReadingParallel
                }
            );

            var generateTests = new TransformManyBlock<string, GenerationResult>
            (
                async data => await generator.Generate(data),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _maxFilesParsingParallel
                }
            );

            var writeTestToFile = new ActionBlock<GenerationResult>
            (
                async data => 
                {
                    var outputFileName = data.ClassName + "Tests";
                    var fullPath = Path.Combine(_outputDirectory, outputFileName);
                    fullPath = Path.ChangeExtension(fullPath, ".cs");
                    await IO.WriteAsync(fullPath, data.Content);
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = _maxFilesWritingParallel
                }
            );

            readFile.LinkTo(generateTests, new DataflowLinkOptions { PropagateCompletion = true });
            generateTests.LinkTo(writeTestToFile, new DataflowLinkOptions { PropagateCompletion = true });

            return readFile;
        }
    }
}
