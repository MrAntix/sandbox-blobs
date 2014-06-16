using System.Diagnostics;
using System.IO;
using Antix.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Sandbox.Blobs.Tests
{
    public class ScannerTests
    {
        readonly string _dir;

        public ScannerTests()
        {
            _dir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "results");

            if (!Directory.Exists(_dir))
            {
                Directory.CreateDirectory(_dir);
            }

            Process.Start(_dir);
        }

        //[Fact]
        //public void scan_Practitioner_1()
        //{
        //    var scanner = new Scanner();
        //    var
        //        result = scanner.Scan(
        //            GetType().FindResourceStream("Practitioner-1.jpg"),
        //            Path.Combine(_dir, "Practitioner-1.png"),
        //            10, 2, true);

        //    Debug.WriteLine(
        //        JsonConvert.SerializeObject(result, Formatting.Indented)
        //        );
        //}

        //[Fact]
        //public void scan_Practitioner_2()
        //{
        //    var scanner = new Scanner();
        //    var
        //        result = scanner.Scan(
        //            GetType().FindResourceStream("Practitioner-2.jpg"),
        //            Path.Combine(_dir, "Practitioner-2.png"),
        //            0, 2, true);

        //    Debug.WriteLine(
        //        JsonConvert.SerializeObject(result, Formatting.Indented)
        //        );
        //}

        //[Fact]
        //public void scan_Practitioner_3()
        //{
        //    var scanner = new Scanner();
        //    var
        //        result = scanner.Scan(
        //            GetType().FindResourceStream("Practitioner-3.jpg"),
        //            Path.Combine(_dir, "Practitioner-3.png"),
        //            0, 2, true);

        //    Debug.WriteLine(
        //        JsonConvert.SerializeObject(result, Formatting.Indented)
        //        );
        //}

        [Fact]
        public void scan_faded_itil()
        {
            var scanner = new Scanner();
            var
                result = scanner.Scan(
                    GetType().FindResourceStream("Itil-faded-1.jpg"),
                    Path.Combine(_dir, "Itil-faded-1.png"),
                    6, 1, true);

            Debug.WriteLine(
                JsonConvert.SerializeObject(result, Formatting.Indented)
                );
        }

        [Fact]
        public void scan_faded_itil_benchmark()
        {
            var scanner = new Scanner();
            var image = GetType().FindResourceStream("Itil-faded-1.jpg");

            var benchmark = new Benchmark(
                () => scanner.Scan(
                    image,
                    Path.Combine(_dir, "Itil-faded-1.png"),
                    6, 1, false)
                );

            Debug.WriteLine(benchmark.Run(1));
            Debug.WriteLine(benchmark.Run(10));
            Debug.WriteLine(benchmark.Run(100));
        }
    }
}