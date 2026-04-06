using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace VSPets.Benchmarks
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Summary[] _ = BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
