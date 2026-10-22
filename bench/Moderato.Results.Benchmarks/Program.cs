using BenchmarkDotNet.Running;

// dotnet run -c Release --project bench/Moderato.Results.Benchmarks -- --filter * --inProcess
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
