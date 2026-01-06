using Spectre.Console;
using System.Text.Json.Serialization;

namespace GhostBodyObject.BenchmarkRunner
{
    public class BenchmarkResult
    {
        public const int LABEL_PADDING = 60;
        public const int VALUE_PADDING = 20;
        public const string INDENT = "    ";

        public TimeSpan Duration { get; init; }
        public long BytesAllocated { get; init; }
        public int Gen0 { get; init; }
        public int Gen1 { get; init; }
        public int Gen2 { get; init; }

        public string Label { get; private set; } = "No label.";
        public long TotalOperations { get; private set; } = 0;
        public string Code { get; private set; } = "";

        /// <summary>
        /// Sets the label for this benchmark result.
        /// </summary>
        public BenchmarkResult WithLabel(string label)
        {
            Label = label;
            return this;
        }

        /// <summary>
        /// Sets the benchmark code for identification.
        /// </summary>
        public BenchmarkResult WithCode(string code)
        {
            Code = code;
            return this;
        }

        /// <summary>
        /// Sets the total number of operations for throughput calculations.
        /// </summary>
        public BenchmarkResult WithOperations(long totalOperations)
        {
            TotalOperations = totalOperations;
            return this;
        }

        /// <summary>
        /// Displays the execution summary (Time, Memory, GC) in a vertical list.
        /// </summary>
        public BenchmarkResult PrintToConsole(string label)
        {
            var table = new Table();
            table.Border(TableBorder.None);
            table.HideHeaders();
            table.AddColumn(new TableColumn("Label").PadRight(2));
            table.AddColumn(new TableColumn("Value").RightAligned());

            AnsiConsole.Write(new Rule($"[blue]{label}[/]").Centered().RuleStyle("blue dim"));
            AnsiConsole.WriteLine();

            if (Gen0 + Gen1 + Gen2 == 0)
                table.AddRow(
                    $"{INDENT}[gray]Garbage Collector (gen 0, 1, 2)[/]".PadRight(LABEL_PADDING),
                    $"[Green]None[/]".PadLeft(VALUE_PADDING));
            else
                table.AddRow(
                    $"{INDENT}[gray]Garbage Collector (gen 0, 1, 2)[/]".PadRight(LABEL_PADDING),
                    $"[red]{Gen0} / {Gen1} / {Gen2}[/]".PadLeft(VALUE_PADDING));

            if (BytesAllocated == 0)
                table.AddRow(
                    $"{INDENT}[gray]Memory Used[/]".PadRight(LABEL_PADDING),
                    $"[Green]None[/]".PadLeft(VALUE_PADDING));
            else
                table.AddRow(
                    $"{INDENT}[gray]Memory Used[/]".PadRight(LABEL_PADDING),
                    $"[red]{FormatBytes(BytesAllocated)}[/]".PadLeft(VALUE_PADDING));

            table.AddRow(
                $"{INDENT}[gray]Duration[/]".PadRight(LABEL_PADDING),
                $"[yellow]{Duration.TotalMilliseconds:N0} ms[/]".PadLeft(VALUE_PADDING));

            AnsiConsole.Write(table);
            Label = label;
            return this;
        }

        /// <summary>
        /// Computes and displays throughput (Ops/Sec) and Latency underneath the main results.
        /// </summary>
        public BenchmarkResult PrintDelayPerOp(long totalOperations)
        {
            if (totalOperations <= 0) return this;

            TotalOperations = totalOperations;
            double ms = Duration.TotalMilliseconds;
            double opsPerSec = ms > 0 ? (totalOperations / ms) * 1000.0 : 0;
            double nsPerOp = ms > 0 ? (ms * 1_000_000.0) / totalOperations : 0;

            var table = new Table();
            table.Border(TableBorder.None);
            table.HideHeaders();
            table.AddColumn(new TableColumn("Label").PadRight(2));
            table.AddColumn(new TableColumn("Value").RightAligned());

            table.AddRow(
                $"{INDENT}[Gray]Operation cost[/]".PadRight(LABEL_PADDING),
                $"[White]{FormatOperationCost(nsPerOp)}[/]".PadLeft(VALUE_PADDING));

            table.AddRow(
                $"{INDENT}[Gray]Operations per second[/]".PadRight(LABEL_PADDING),
                $"[White]{FormatOpsPerSecond(opsPerSec)}[/]".PadLeft(VALUE_PADDING));

            AnsiConsole.Write(table);
            return this;
        }

        /// <summary>
        /// Formats operation cost with adaptive time unit (ns, µs, ms, s, min).
        /// </summary>
        private static string FormatOperationCost(double nanoseconds)
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
        private static string FormatOpsPerSecond(double opsPerSec)
        {
            if (opsPerSec < 10_000)
                return $"{opsPerSec:N0}";
            else if (opsPerSec < 100_000_000)
                return $"{opsPerSec / 1_000:N1}k";
            else
                return $"{opsPerSec / 1_000_000:N1}M";
        }

        public BenchmarkResult PrintSpace()
        {
            AnsiConsole.WriteLine();
            return this;
        }

        /// <summary>
        /// Converts to a JSON-serializable record.
        /// </summary>
        public BenchmarkResultRecord ToRecord()
        {
            double ms = Duration.TotalMilliseconds;
            double? opsPerSec = TotalOperations > 0 && ms > 0 ? (TotalOperations / ms) * 1000.0 : null;
            double? nsPerOp = TotalOperations > 0 && ms > 0 ? (ms * 1_000_000.0) / TotalOperations : null;

            return new BenchmarkResultRecord
            {
                Code = Code,
                Label = Label,
                DurationMs = ms,
                BytesAllocated = BytesAllocated,
                Gen0 = Gen0,
                Gen1 = Gen1,
                Gen2 = Gen2,
                TotalOperations = TotalOperations,
                OperationsPerSecond = opsPerSec,
                NanosecondsPerOperation = nsPerOp
            };
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
            return $"{number:n1} {suffixes[counter]}"; // e.g. "1.2 MB"
        }
    }

    /// <summary>
    /// JSON-serializable benchmark result record for file output.
    /// </summary>
    public class BenchmarkResultRecord
    {
        public string Code { get; set; } = "";
        public string Label { get; set; } = "";
        public double DurationMs { get; set; }
        public long BytesAllocated { get; set; }
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
        public long TotalOperations { get; set; }
        public double? OperationsPerSecond { get; set; }
        public double? NanosecondsPerOperation { get; set; }
    }
}