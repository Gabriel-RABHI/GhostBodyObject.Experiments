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
using System.Text;

namespace GhostBodyObject.BenchmarkRunner
{
    public abstract class BenchmarkBase
    {
        private bool _isCapturing = false;
        private string _captureCode = "";
        private List<BenchmarkResult> _capturedResults = new();
        private StringBuilder _markdownBuilder = new();
        private HashSet<BenchmarkResult> _alreadyCaptured = new();

        /// <summary>
        /// Starts capturing benchmark results for file output.
        /// </summary>
        internal void StartResultCapture(string code)
        {
            _isCapturing = true;
            _captureCode = code;
            _capturedResults = new List<BenchmarkResult>();
            _markdownBuilder = new StringBuilder();
            _alreadyCaptured = new HashSet<BenchmarkResult>();
        }

        /// <summary>
        /// Ends capturing and returns the captured results.
        /// </summary>
        internal List<BenchmarkResult> EndResultCapture()
        {
            _isCapturing = false;
            var results = _capturedResults;
            _capturedResults = new List<BenchmarkResult>();
            _alreadyCaptured = new HashSet<BenchmarkResult>();
            return results;
        }

        /// <summary>
        /// Gets the captured markdown content.
        /// </summary>
        internal string GetCapturedMarkdown() => _markdownBuilder.ToString();

        /// <summary>
        /// Captures a result if capturing is enabled and not already captured.
        /// </summary>
        protected void CaptureResult(BenchmarkResult result)
        {
            if (_isCapturing && !_alreadyCaptured.Contains(result))
            {
                result.WithCode(_captureCode);
                _capturedResults.Add(result);
                _alreadyCaptured.Add(result);
            }
        }

        /// <summary>
        /// Formats operation cost with adaptive time unit (ns, ms, s).
        /// </summary>
        protected static string FormatOperationCost(double nanoseconds)
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
        protected static string FormatOpsPerSecond(double opsPerSec)
        {
            if (opsPerSec < 10_000)
                return $"{opsPerSec:N0}";
            else if (opsPerSec < 100_000_000)
                return $"{opsPerSec / 1_000:N1}k";
            else
                return $"{opsPerSec / 1_000_000:N1}M";
        }

        /// <summary>
        /// Prints a comparison table for multiple benchmark results.
        /// </summary>
        protected void PrintComparison(string title, string description, IEnumerable<BenchmarkResult> results)
        {
            var resultList = results.ToList();
            if (resultList.Count == 0)
                return;

            // Capture results if capturing is enabled
            foreach (var result in resultList)
                CaptureResult(result);

            // Find the fastest result (minimum duration) for comparison factor calculation
            double fastestMs = resultList.Min(r => r.Duration.TotalMilliseconds);
            if (fastestMs <= 0) fastestMs = 1; // Avoid division by zero

            // Escape title and description for Spectre.Console markup
            var escapedTitle = Markup.Escape(title);
            var escapedDescription = Markup.Escape(description ?? "");

            // Print header to console
            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Rule($"[blue]{escapedTitle}[/]").Centered().RuleStyle("blue dim"));
            AnsiConsole.WriteLine();
            if (!string.IsNullOrWhiteSpace(description))
            {
                AnsiConsole.MarkupLine($"[gray]{escapedDescription}[/]");
            }
            AnsiConsole.WriteLine();

            // Create comparison table for console
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn(new TableColumn("[bold]Label[/]").LeftAligned());
            table.AddColumn(new TableColumn("[bold]GC 0/1/2[/]").Centered());
            table.AddColumn(new TableColumn("[bold]Memory[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Op Cost[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Total Op/s[/]").RightAligned());
            table.AddColumn(new TableColumn("[bold]Factor[/]").RightAligned());

            // Build markdown comparison table if capturing
            if (_isCapturing)
            {
                _markdownBuilder.AppendLine($"#### {title}");
                if (!string.IsNullOrWhiteSpace(description))
                    _markdownBuilder.AppendLine($"*{description}*");
                _markdownBuilder.AppendLine();
                _markdownBuilder.AppendLine("| Label | GC 0/1/2 | Memory | Op Cost | Total Op/s | Factor |");
                _markdownBuilder.AppendLine("|-------|----------|--------|---------|------------|--------|");
            }

            foreach (var result in resultList)
            {
                // Comparison factor: 1.0 is fastest, 0.5 means twice as slow
                double factor = fastestMs / result.Duration.TotalMilliseconds;
                bool isFastest = factor >= 0.99;

                // Escape label for Spectre.Console markup
                var escapedLabel = Markup.Escape(result.Label);

                // GC formatting
                string gcDisplay, gcPlain;
                if (result.Gen0 + result.Gen1 + result.Gen2 == 0)
                {
                    gcDisplay = "[green]None[/]";
                    gcPlain = "None";
                } else
                {
                    gcDisplay = $"[red]{result.Gen0}/{result.Gen1}/{result.Gen2}[/]";
                    gcPlain = $"{result.Gen0}/{result.Gen1}/{result.Gen2}";
                }

                // Memory formatting
                string memDisplay, memPlain;
                if (result.BytesAllocated == 0)
                {
                    memDisplay = "[green]None[/]";
                    memPlain = "None";
                } else
                {
                    memDisplay = $"[red]{FormatBytes(result.BytesAllocated)}[/]";
                    memPlain = FormatBytes(result.BytesAllocated);
                }

                // Operation cost and throughput with adaptive formatting
                string opCostDisplay = "-", opCostPlain = "-";
                string opsPerSecDisplay = "-", opsPerSecPlain = "-";
                if (result.TotalOperations > 0 && result.Duration.TotalMilliseconds > 0)
                {
                    double ms = result.Duration.TotalMilliseconds;
                    double nsPerOp = (ms * 1_000_000.0) / result.TotalOperations;
                    double opsPerSec = (result.TotalOperations / ms) * 1000.0;

                    opCostPlain = FormatOperationCost(nsPerOp);
                    opsPerSecPlain = FormatOpsPerSecond(opsPerSec);
                    opCostDisplay = $"[white]{opCostPlain}[/]";
                    opsPerSecDisplay = $"[white]{opsPerSecPlain}[/]";
                }

                // Factor formatting - bold for fastest
                string factorDisplay, factorPlain;
                factorPlain = $"{factor:N2}";
                if (isFastest)
                {
                    factorDisplay = $"[bold green]{factor:N2}[/]";
                    factorPlain = $"**{factor:N2}**";
                } else if (factor >= 0.5)
                    factorDisplay = $"[yellow]{factor:N2}[/]";
                else
                    factorDisplay = $"[red]{factor:N2}[/]";

                // Label formatting - bold for fastest (use escaped label)
                string labelDisplay = isFastest ? $"[bold]{escapedLabel}[/]" : escapedLabel;
                string labelPlain = isFastest ? $"**{result.Label}**" : result.Label;

                table.AddRow(
                    labelDisplay,
                    gcDisplay,
                    memDisplay,
                    opCostDisplay,
                    opsPerSecDisplay,
                    factorDisplay
                );

                // Add to markdown if capturing
                if (_isCapturing)
                {
                    _markdownBuilder.AppendLine($"| {labelPlain} | {gcPlain} | {memPlain} | {opCostPlain} | {opsPerSecPlain} | {factorPlain} |");
                }
            }

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            if (_isCapturing)
            {
                _markdownBuilder.AppendLine();
            }
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

        /// <summary>
        /// Run a synchronous action, monitor it, and return results.
        /// </summary>
        protected BenchmarkResult RunMonitoredAction(Action action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var startAlloc = GC.GetTotalAllocatedBytes(true);
            var startG0 = GC.CollectionCount(0);
            var startG1 = GC.CollectionCount(1);
            var startG2 = GC.CollectionCount(2);

            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();

            var endAlloc = GC.GetTotalAllocatedBytes(true);

            var result = new BenchmarkResult {
                Duration = sw.Elapsed,
                BytesAllocated = endAlloc - startAlloc,
                Gen0 = GC.CollectionCount(0) - startG0,
                Gen1 = GC.CollectionCount(1) - startG1,
                Gen2 = GC.CollectionCount(2) - startG2
            };
            return result;
        }

        /// <summary>
        /// Run an asynchronous action, monitor it, and return results.
        /// </summary>
        protected async Task<BenchmarkResult> RunMonitoredActionAsync(Func<Task> action)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var startAlloc = GC.GetTotalAllocatedBytes(true);
            var startG0 = GC.CollectionCount(0);
            var startG1 = GC.CollectionCount(1);
            var startG2 = GC.CollectionCount(2);

            var sw = Stopwatch.StartNew();
            await action();
            sw.Stop();

            var endAlloc = GC.GetTotalAllocatedBytes(true);

            var result = new BenchmarkResult {
                Duration = sw.Elapsed,
                BytesAllocated = endAlloc - startAlloc,
                Gen0 = GC.CollectionCount(0) - startG0,
                Gen1 = GC.CollectionCount(1) - startG1,
                Gen2 = GC.CollectionCount(2) - startG2
            };
            return result;
        }

        /// <summary>
        /// Runs an action in parallel threads with tight-loop synchronization.
        /// </summary>
        protected BenchmarkResult RunParallelAction(int threadCount, Action<int> action)
        {
            return RunParallelActionAsync(threadCount, (id) => { action(id); return Task.CompletedTask; }).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs an async action in parallel tasks with tight-loop synchronization.
        /// </summary>
        protected async Task<BenchmarkResult> RunParallelActionAsync(int threadCount, Func<int, Task> action)
        {
            return await RunMonitoredActionAsync(async () => {
                using var startSignal = new ManualResetEventSlim(false);
                using var readySignal = new CountdownEvent(threadCount);

                var tasks = new Task[threadCount];

                for (int i = 0; i < threadCount; i++)
                {
                    int localId = i;
                    tasks[i] = Task.Run(async () => {
                        readySignal.Signal();
                        var spin = new SpinWait();
                        while (!startSignal.IsSet)
                            spin.SpinOnce();
                        await action(localId);
                    });
                }
                readySignal.Wait();
                startSignal.Set();
                await Task.WhenAll(tasks);
            });
        }

        public BenchmarkBase WriteComment(string comment)
        {
            AnsiConsole.WriteLine();
            var escapedComment = Markup.Escape(comment);
            AnsiConsole.Write(new Rule($"[yellow]{escapedComment}[/]"));
            AnsiConsole.WriteLine();
            return this;
        }
    }
}