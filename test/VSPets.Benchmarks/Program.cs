using System;
using BenchmarkDotNet.Running;

namespace VSPets.Benchmarks
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
