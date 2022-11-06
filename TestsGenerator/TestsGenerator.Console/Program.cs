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

        public Program DoBebra(string toPrint)
        {
            Console.WriteLine(""Hello, World!"");
        }

        public string DoBebra(IBebra bebra, string toPrint)
        {
            Console.WriteLine(""Hello, World!"");
        }

        public void DoBebra(IBebra bebra, string toPrint)
        {
            Console.WriteLine(""Hello, World!"");
        }
    }
}";

var anotherProgramText =
@"using System;

namespace MyCode
{
    public class MyClass
    {
        public void FirstMethod()
        {
            Console.WriteLine(""First method"");
        }
        
        public void SecondMethod()
        {
            Console.WriteLine(""Second method"");
        }

        public void ThirdMethod(int a)
        {
            Console.WriteLine(""Third method (int)"");
        }

        public void ThirdMethod(double a)
        {
            Console.WriteLine(""Third method (double)"");
        }
    }
}";

var result = await generator.Generate(anotherProgramText);
Console.WriteLine(result.First().Content);
Console.ReadLine();
