using BenchmarkDotNet.Running;
using Strongbars.Benchmarks;

// Run in Release mode: dotnet run -c Release --project Strongbars.Benchmarks
BenchmarkRunner.Run<AllTemplatesBenchmark>();
