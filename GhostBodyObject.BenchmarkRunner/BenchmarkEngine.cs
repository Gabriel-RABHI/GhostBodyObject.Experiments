/*
 * Copyright (c) 2026 Gabriel RABHI / DOT-BEES
 *
 * This file is part of Ghost-Body-Object (GBO).
 *
 * Ghost-Body-Object (GBO) is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Ghost-Body-Object (GBO) is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with this program. If not, see <https://www.gnu.org/licenses/>.
 *
 * --------------------------------------------------------------------------
 *
 * COMMERICIAL LICENSING:
 *
 * If you wish to use this software in a proprietary (closed-source) application,
 * you must purchase a Commercial License from Gabriel RABHI / DOT-BEES.
 *
 * For licensing inquiries, please contact: <mailto:gabriel.rabhi@gmail.com>
 * or visit: <https://www.ghost-body-object.com>
 *
 * --------------------------------------------------------------------------
 */

using Spectre.Console;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GhostBodyObject.BenchmarkRunner
{
    public static class BenchmarkEngine
    {
        public static bool GenerateOutputFiles { get; set; } = false;

        /// <summary>
        /// Gets all discovered benchmarks.
        /// </summary>
        public static IReadOnlyList<BenchmarkMetadata> GetAllBenchmarks() => FindBenchmarks();

        /// <summary>
        /// Formats operation cost with adaptive time unit (ns, µs, ms, s, min).
        /// </summary>
        public static string FormatOperationCost(double nanoseconds)
        {
            if (nanoseconds < 1_000)
                return $"{nanoseconds:N2} ns";
            else if (nanoseconds < 1_000_000)
                return $"{nanoseconds / 1_000:N2} µs";
            else if (nanoseconds < 1_000_000_000)
                return $"{nanoseconds / 1_000_000:N2} ms";
            else if (nanoseconds < 60_000_000_000)
                return $"{nanoseconds / 1_000_000_000:N2} s";
            else
                return $"{nanoseconds / 60_000_000_000:N2} min";
        }

        /// <summary>
        /// Formats operations per second with adaptive unit (plain, k, M).
        /// </summary>
        public static string FormatOpsPerSecond(double opsPerSec)
        {
            if (opsPerSec < 10_000)
                return $"{opsPerSec:N0}";
            else if (opsPerSec < 100_000_000)
                return $"{opsPerSec / 1_000:N1}k";
            else
                return $"{opsPerSec / 1_000_000:N1}M";
        }

        /// <summary>
        /// Runs all discovered benchmarks and optionally generates output files.
        /// </summary>
        public static async Task RunAllBenchmarksAsync()
        {
            var benchmarks = FindBenchmarks()
                .Where(b => b.Attribute.Category != "Z-SYS") // Exclude system benchmarks
                .ToList();

            if (!benchmarks.Any())
            {
                AnsiConsole.MarkupLine("[red]No benchmarks found to run.[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[blue]Running {benchmarks.Count} benchmarks...[/]");
            AnsiConsole.WriteLine();

            var allResults = new List<BenchmarkResultRecord>();
            var markdownBuilder = new StringBuilder();
            var runDate = DateTime.Now;

            markdownBuilder.AppendLine($"# Benchmark Results");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine($"**Run Date:** {runDate:yyyy-MM-dd HH:mm:ss}");
            markdownBuilder.AppendLine();
            markdownBuilder.AppendLine("---");
            markdownBuilder.AppendLine();

            string? currentCategory = null;

            foreach (var benchmark in benchmarks)
            {
                try
                {
                    if (currentCategory != benchmark.Attribute.Category)
                    {
                        currentCategory = benchmark.Attribute.Category;
                        markdownBuilder.AppendLine($"## Category: {currentCategory}");
                        markdownBuilder.AppendLine();
                    }

                    var instance = (BenchmarkBase)Activator.CreateInstance(benchmark.Type)!;

                    AnsiConsole.MarkupLine("[blue]********************************************************************************[/]");
                    AnsiConsole.MarkupLine($"[blue]******** {benchmark.Attribute.Name}[/]");
                    AnsiConsole.MarkupLine("[blue]********************************************************************************[/]");

                    // Capture results from the benchmark
                    instance.StartResultCapture(benchmark.Attribute.Code);

                    if (benchmark.Method.ReturnType == typeof(Task))
                        await (Task)benchmark.Method.Invoke(instance, null)!;
                    else
                        benchmark.Method.Invoke(instance, null);

                    var capturedResults = instance.EndResultCapture();
                    var capturedMarkdown = instance.GetCapturedMarkdown();

                    // Add to markdown
                    markdownBuilder.AppendLine($"### {benchmark.Attribute.Name} (`{benchmark.Attribute.Code}`)");
                    markdownBuilder.AppendLine();

                    if (capturedResults.Count > 0)
                    {
                        // If there's captured markdown from PrintComparison, use it
                        if (!string.IsNullOrWhiteSpace(capturedMarkdown))
                        {
                            markdownBuilder.Append(capturedMarkdown);
                        } else
                        {
                            // Fallback: generate simple table with adaptive formatting
                            markdownBuilder.AppendLine("| Label | Duration | Memory | GC 0/1/2 | Op Cost | Op/s |");
                            markdownBuilder.AppendLine("|-------|----------|--------|----------|---------|------|");

                            foreach (var result in capturedResults)
                            {
                                var record = result.ToRecord();
                                var memStr = record.BytesAllocated == 0 ? "None" : FormatBytes(record.BytesAllocated);
                                var gcStr = record.Gen0 + record.Gen1 + record.Gen2 == 0 ? "None" : $"{record.Gen0}/{record.Gen1}/{record.Gen2}";
                                var opCostStr = record.NanosecondsPerOperation.HasValue ? FormatOperationCost(record.NanosecondsPerOperation.Value) : "-";
                                var opsStr = record.OperationsPerSecond.HasValue ? FormatOpsPerSecond(record.OperationsPerSecond.Value) : "-";
                                var durationStr = FormatDuration(record.DurationMs);

                                markdownBuilder.AppendLine($"| {record.Label} | {durationStr} | {memStr} | {gcStr} | {opCostStr} | {opsStr} |");
                            }
                            markdownBuilder.AppendLine();
                        }

                        foreach (var result in capturedResults)
                        {
                            allResults.Add(result.ToRecord());
                        }
                    } else
                    {
                        markdownBuilder.AppendLine("*No results captured.*");
                        markdownBuilder.AppendLine();
                    }

                    AnsiConsole.WriteLine();
                } catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                    markdownBuilder.AppendLine($"**Error:** {ex.Message}");
                    markdownBuilder.AppendLine();
                }
            }

            // Generate output files if enabled
            if (GenerateOutputFiles && allResults.Count > 0)
            {
                var category = "ALL";
                var code = "RunAll";
                WriteOutputFiles(category, code, runDate, markdownBuilder.ToString(), allResults);
            }

            AnsiConsole.MarkupLine($"[green]Completed running {benchmarks.Count} benchmarks.[/]");
        }

        /// <summary>
        /// Formats duration with adaptive unit.
        /// </summary>
        private static string FormatDuration(double milliseconds)
        {
            if (milliseconds < 1)
                return $"{milliseconds * 1000:N2} µs";
            else if (milliseconds < 1000)
                return $"{milliseconds:N2} ms";
            else if (milliseconds < 60000)
                return $"{milliseconds / 1000:N2} s";
            else
                return $"{milliseconds / 60000:N2} min";
        }

        /// <summary>
        /// Writes output files for a benchmark run.
        /// </summary>
        public static void WriteOutputFiles(string category, string code, DateTime runDate, string markdownContent, List<BenchmarkResultRecord> results)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dateStr = $"{runDate.Year}-{runDate.Month:00}-{runDate.Day:00}";
            var baseFileName = $"{SanitizeFileName(category)}-{SanitizeFileName(code)}-";

            // Find next file number
            int nextNumber = 1;
            var existingFiles = Directory.GetFiles(baseDir, $"{baseFileName}*.*");
            foreach (var file in existingFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('-');
                if (parts.Length >= 5 && int.TryParse(parts[^1], out int num))
                {
                    if (num >= nextNumber)
                        nextNumber = num + 1;
                }
            }

            var outputFileName = $"{baseFileName}Output-{dateStr}-{nextNumber:000}";
            var dataFileName = $"{baseFileName}Data-{dateStr}-{nextNumber:000}";

            var mdPath = Path.Combine(baseDir, outputFileName + ".md");
            var jsonPath = Path.Combine(baseDir, dataFileName + ".json");

            // Write markdown file
            File.WriteAllText(mdPath, markdownContent);

            // Write JSON file with formatted values
            var jsonData = new BenchmarkDataFile {
                RunDate = runDate,
                Category = category,
                Code = code,
                Results = results.Select(r => new BenchmarkResultRecordWithFormatted {
                    Code = r.Code,
                    Label = r.Label,
                    DurationMs = r.DurationMs,
                    DurationFormatted = FormatDuration(r.DurationMs),
                    BytesAllocated = r.BytesAllocated,
                    MemoryFormatted = r.BytesAllocated == 0 ? "None" : FormatBytes(r.BytesAllocated),
                    Gen0 = r.Gen0,
                    Gen1 = r.Gen1,
                    Gen2 = r.Gen2,
                    TotalOperations = r.TotalOperations,
                    OperationsPerSecond = r.OperationsPerSecond,
                    OperationsPerSecondFormatted = r.OperationsPerSecond.HasValue ? FormatOpsPerSecond(r.OperationsPerSecond.Value) : null,
                    NanosecondsPerOperation = r.NanosecondsPerOperation,
                    OperationCostFormatted = r.NanosecondsPerOperation.HasValue ? FormatOperationCost(r.NanosecondsPerOperation.Value) : null
                }).ToList()
            };
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(jsonPath, JsonSerializer.Serialize(jsonData, jsonOptions));

            AnsiConsole.MarkupLine($"[green]Output files generated:[/]");
            AnsiConsole.MarkupLine($"  [grey]Markdown:[/] {mdPath}");
            AnsiConsole.MarkupLine($"  [grey]JSON:[/] {jsonPath}");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var result = new StringBuilder();
            foreach (var c in name)
            {
                if (!invalid.Contains(c) && c != ' ')
                    result.Append(c);
            }
            return result.Length > 0 ? result.ToString() : "Default";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }

        public static void DiscoverAndShow()
        {
            DiscoverAndShowAsync().GetAwaiter().GetResult();
        }

        public static void WriteWarningLine(string content)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(content);
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static async Task DiscoverAndShowAsync()
        {
            var benchmarks = FindBenchmarks();
            if (!benchmarks.Any())
            {
                AnsiConsole.MarkupLine("[red]No benchmarks found. Ensure your classes inherit BenchmarkBase and methods have [BenchmarkAttribute].[/]");
                return;
            }
            AnsiConsole.Write(new FigletText("GBO-Benchmark").Color(Color.Cyan1));
            AnsiConsole.MarkupLine("[grey]Simple Brut Force Benchmarks Runner[/]");
            AnsiConsole.WriteLine();

            if (Debugger.IsAttached)
            {
                WriteWarningLine("Debugger is attached : this may slowdown execution.");
                AnsiConsole.WriteLine();
            }
#if DEBUG
            WriteWarningLine("DEBUG directive defined : this will slowdown execution. Please, compile and run in Release.");
            AnsiConsole.WriteLine();
#endif
            while (true)
            {
                var padding = benchmarks.Max(b => b.Attribute.Category.Length);
                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<BenchmarkMetadata>()
                        .Title("Select a benchmark to run:")
                        .PageSize(15)
                        .MoreChoicesText("[grey](Move up and down for more)[/]")
                        .AddChoices(benchmarks)
                        .UseConverter(b => $"[grey][[ {b.Attribute.Category.PadRight(padding)} ]][/] [white]{b.Attribute.Name}[/] [grey]({b.Attribute.Code})[/]")
                    );
                try
                {
                    var instance = (BenchmarkBase)Activator.CreateInstance(selection.Type)!;
                    var runDate = DateTime.Now;

                    AnsiConsole.MarkupLine("[blue]********************************************************************************[/]");
                    AnsiConsole.MarkupLine($"[blue]******** {selection.Attribute.Name}[/]");
                    AnsiConsole.MarkupLine("[blue]********************************************************************************[/]");

                    // Start capturing if output generation is enabled
                    if (GenerateOutputFiles)
                    {
                        instance.StartResultCapture(selection.Attribute.Code);
                    }

                    if (selection.Method.ReturnType == typeof(Task))
                        await (Task)selection.Method.Invoke(instance, null)!;
                    else
                        selection.Method.Invoke(instance, null);

                    // Generate output files for individual benchmark if enabled
                    if (GenerateOutputFiles)
                    {
                        var capturedResults = instance.EndResultCapture();
                        var capturedMarkdown = instance.GetCapturedMarkdown();

                        if (capturedResults.Count > 0)
                        {
                            var markdownBuilder = new StringBuilder();
                            markdownBuilder.AppendLine($"# {selection.Attribute.Name}");
                            markdownBuilder.AppendLine();
                            markdownBuilder.AppendLine($"**Run Date:** {runDate:yyyy-MM-dd HH:mm:ss}");
                            markdownBuilder.AppendLine($"**Category:** {selection.Attribute.Category}");
                            markdownBuilder.AppendLine($"**Code:** {selection.Attribute.Code}");
                            markdownBuilder.AppendLine();
                            markdownBuilder.AppendLine("---");
                            markdownBuilder.AppendLine();

                            if (!string.IsNullOrWhiteSpace(capturedMarkdown))
                            {
                                markdownBuilder.Append(capturedMarkdown);
                            } else
                            {
                                markdownBuilder.AppendLine("| Label | Duration | Memory | GC 0/1/2 | Op Cost | Op/s |");
                                markdownBuilder.AppendLine("|-------|----------|--------|----------|---------|------|");

                                foreach (var result in capturedResults)
                                {
                                    var record = result.ToRecord();
                                    var memStr = record.BytesAllocated == 0 ? "None" : FormatBytes(record.BytesAllocated);
                                    var gcStr = record.Gen0 + record.Gen1 + record.Gen2 == 0 ? "None" : $"{record.Gen0}/{record.Gen1}/{record.Gen2}";
                                    var opCostStr = record.NanosecondsPerOperation.HasValue ? FormatOperationCost(record.NanosecondsPerOperation.Value) : "-";
                                    var opsStr = record.OperationsPerSecond.HasValue ? FormatOpsPerSecond(record.OperationsPerSecond.Value) : "-";
                                    var durationStr = FormatDuration(record.DurationMs);

                                    markdownBuilder.AppendLine($"| {record.Label} | {durationStr} | {memStr} | {gcStr} | {opCostStr} | {opsStr} |");
                                }
                                markdownBuilder.AppendLine();
                            }

                            var resultRecords = capturedResults.Select(r => r.ToRecord()).ToList();
                            WriteOutputFiles(
                                selection.Attribute.Category,
                                selection.Attribute.Code,
                                runDate,
                                markdownBuilder.ToString(),
                                resultRecords);
                        }
                    }
                } catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
                AnsiConsole.WriteLine();
            }
        }

        private static List<BenchmarkMetadata> FindBenchmarks()
        {
            var results = new List<BenchmarkMetadata>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsSubclassOf(typeof(BenchmarkBase)) || type.IsAbstract)
                        continue;

                    foreach (var method in type.GetMethods())
                    {
                        var attr = method.GetCustomAttribute<BruteForceBenchmarkAttribute>();
                        if (attr != null)
                            results.Add(new BenchmarkMetadata(type, method, attr));
                    }
                }
            }
            return results
                .OrderBy(x => x.Attribute.Category)
                .ThenBy(x => x.Attribute.Code)
                .ToList();
        }
    }

    /// <summary>
    /// JSON data file structure for benchmark results.
    /// </summary>
    public class BenchmarkDataFile
    {
        public DateTime RunDate { get; set; }
        public string Category { get; set; } = "";
        public string Code { get; set; } = "";
        public List<BenchmarkResultRecordWithFormatted> Results { get; set; } = new();
    }

    /// <summary>
    /// Extended benchmark result record with formatted string values for JSON output.
    /// </summary>
    public class BenchmarkResultRecordWithFormatted
    {
        public string Code { get; set; } = "";
        public string Label { get; set; } = "";
        public double DurationMs { get; set; }
        public string? DurationFormatted { get; set; }
        public long BytesAllocated { get; set; }
        public string? MemoryFormatted { get; set; }
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
        public long TotalOperations { get; set; }
        public double? OperationsPerSecond { get; set; }
        public string? OperationsPerSecondFormatted { get; set; }
        public double? NanosecondsPerOperation { get; set; }
        public string? OperationCostFormatted { get; set; }
    }

    /// <summary>
    /// Metadata about a discovered benchmark.
    /// </summary>
    public record BenchmarkMetadata(Type Type, MethodInfo Method, BruteForceBenchmarkAttribute Attribute);
}