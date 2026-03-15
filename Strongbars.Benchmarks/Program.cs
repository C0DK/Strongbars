using BenchmarkDotNet.Running;
using Strongbars.Benchmarks;

// BenchmarkDotNet searches the CWD upward for a .sln or .csproj.
// When invoked via `dotnet run --project Strongbars.Benchmarks` from the repo
// root the CWD is the repo root which only has a .slnx (not recognised).
// Walk up from the assembly output directory to find the .csproj so BDN
// always locates the project regardless of the CWD the caller used.
var dir = new DirectoryInfo(AppContext.BaseDirectory);
while (dir != null && !dir.GetFiles("*.csproj").Any())
    dir = dir.Parent;
if (dir != null)
    Directory.SetCurrentDirectory(dir.FullName);

// Run in Release mode: dotnet run -c Release --project Strongbars.Benchmarks
BenchmarkRunner.Run<AllTemplatesBenchmark>();
