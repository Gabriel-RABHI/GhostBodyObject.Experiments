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

namespace GhostBodyObject.BenchmarkRunner
{
    // -------------------------------------------------------------
    // SAMPLE BENCHMARK CLASS
    // -------------------------------------------------------------
    public class DefaultBenchmarks : BenchmarkBase
    {
        [BruteForceBenchmark("RunAll", "Run All Benchmarks", "Z-SYS")]
        public async Task RunAllBenchmarks()
        {
            await BenchmarkEngine.RunAllBenchmarksAsync();
        }

        [BruteForceBenchmark("GC", "Run GC Collect", "Z-SYS")]
        public void SequentialTest()
        {
            RunMonitoredAction(() => {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                GC.WaitForFullGCComplete();
            })
            .PrintToConsole($"GC.Collect")
            .PrintSpace();
        }

        [BruteForceBenchmark("", "Quit", "Z-SYS")]
        public void Quit()
        {
            Environment.Exit(0);
        }

        [BruteForceBenchmark("", "Switch Outputs Generation", "Z-SYS")]
        public void ToogleGenerateOutput()
        {
            BenchmarkEngine.GenerateOutputFiles = !BenchmarkEngine.GenerateOutputFiles;
            WriteComment($"{(BenchmarkEngine.GenerateOutputFiles ? "File output activated" : "File output disabled")}");
        }
    }
}