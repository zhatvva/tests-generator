using TestsGenerator.Core.Servises;

var generator = new TestsGenerationService();
var programText =
@"using System;
using System.Collections;
using System.Linq;
using System.Text;

namespace HelloWorld
{
    class Program
    {
        private readonly int _value;
        private readonly Program _program;
        public Program(int value)
        {
            _value = value;
            IBebra bebra = new Mock<IBebra>();
            int value = default;
            _program = new Program(bebra, value);
        }

        public Program(IBebra bebra, int value) : this(value) 
        {
            Console.WriteLine(bebra);
        }

        static void Main(string[] args)
        {
            Console.WriteLine(""Hello, World!"");
        }

        public void DoBebra(string toPrint)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";

var result = await generator.Generate(programText);
Console.WriteLine(result.First().Content);
Console.ReadLine();
