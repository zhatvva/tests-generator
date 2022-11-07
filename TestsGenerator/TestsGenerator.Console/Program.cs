using System.Threading.Tasks.Dataflow;
using TestsGenerator.Console;
using TestsGenerator.Core.Servises;

var newArgs = new string[]
{
    "D:\\sunmait-projects\\career-day\\back-end",
    "D:\\GenerationResult",
    "1", "1", "1"
};

args = newArgs;

if (args.Length != 5)
{
    Console.WriteLine("Invalid arguments count");
    return;
}

var inputDirectory = args[0];
var outputDirectory = args[1];

if (!int.TryParse(args[2], out var maxFilesReadingParallel))
{
    Console.WriteLine("Max files reading parallel argument should be int");
    return;
}

if (!int.TryParse(args[3], out var maxFilesWritingParallel))
{
    Console.WriteLine("Max files writing parallel argument should be int");
    return;
}

if (!int.TryParse(args[4], out var maxFilesParsingParallel))
{
    Console.WriteLine("Max files parsing parallel argument should be int");
    return;
}

try
{
    if (Directory.Exists(outputDirectory))
    {
        Directory.Delete(outputDirectory, recursive: true);
    }
    Directory.CreateDirectory(outputDirectory);

    var generator = new TestsGenerationService();
    var pipeline = new Pipeline(outputDirectory, maxFilesReadingParallel, maxFilesWritingParallel, maxFilesParsingParallel);
    var entryPoint = pipeline.GeneratePipeline(generator);

    AddSubDirectoryToQuery(inputDirectory, entryPoint);
    entryPoint.Complete();
    await entryPoint.Completion;
    Console.WriteLine("Done!");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
 
static void AddSubDirectoryToQuery(string path, TransformBlock<string, string> entryPoint)
{
    try
    {
        var directoryInfo = new DirectoryInfo(path);

        var directories = directoryInfo.GetDirectories();
        foreach (var directory in directories)
        {
            AddSubDirectoryToQuery(directory.FullName, entryPoint);
        }

        var files = directoryInfo.GetFiles();
        foreach (var file in files)
        {
            if (file.Extension == ".cs")
            {
                entryPoint.Post(file.FullName);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Cannot read directory with such path: {path}\nException: {ex}\n");
    }
}

//using TestsGenerator.Core.Servises;

//var generator = new TestsGenerationService();
//var programText =
//@"using System;
//using System.Collections;
//using System.Linq;
//using System.Text;

//namespace HelloWorld
//{
//    class Program
//    {
//        private readonly int _value;
//        private readonly Program _program;
//        public Program(int value)
//        {
//            _value = value;
//            IBebra bebra = new Mock<IBebra>();
//            int value = default;
//            _program = new Program(bebra, value);
//        }

//        public Program(IBebra bebra, int value) : this(value) 
//        {
//            Console.WriteLine(bebra);
//        }

//        static void Main(string[] args)
//        {
//            Console.WriteLine(""Hello, World!"");
//        }

//        public Program DoBebra(string toPrint)
//        {
//            Console.WriteLine(""Hello, World!"");
//        }

//        public string DoBebra(IBebra bebra, string toPrint)
//        {
//            Console.WriteLine(""Hello, World!"");
//        }

//        public void DoBebra(IBebra bebra, string toPrint)
//        {
//            Console.WriteLine(""Hello, World!"");
//        }
//    }
//}";

//var anotherProgramText =
//@"using System;

//namespace MyCode
//{
//    public class MyClass
//    {
//        public void FirstMethod()
//        {
//            Console.WriteLine(""First method"");
//        }
        
//        public void SecondMethod()
//        {
//            Console.WriteLine(""Second method"");
//        }

//        public void ThirdMethod(int a)
//        {
//            Console.WriteLine(""Third method (int)"");
//        }

//        public void ThirdMethod(double a)
//        {
//            Console.WriteLine(""Third method (double)"");
//        }
//    }
//}";

//var result = await generator.Generate(anotherProgramText);
//Console.WriteLine(result.First().Content);
//Console.ReadLine();
