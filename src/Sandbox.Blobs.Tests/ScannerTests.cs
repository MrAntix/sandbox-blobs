using System.Diagnostics;
using System.IO;
using Antix.Testing;
using Xunit;

namespace Sandbox.Blobs.Tests
{
    public class ScannerTests
    {
        [Fact]
        public void finds_blobs()
        {
            var scanner = new Scanner();

            var dir = Path.Combine(
                Directory.GetCurrentDirectory(),
                "results");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            scanner.Scan(
                GetType().FindResourceStream("Foundation-1-skew.jpg"),
                Path.Combine(dir, "Foundation-1.png"));

            scanner.Scan(
                GetType().FindResourceStream("Practitioner-1.jpg"),
                Path.Combine(dir, "Practitioner-1.png"));
            scanner.Scan(
                GetType().FindResourceStream("Practitioner-2.jpg"),
                Path.Combine(dir, "Practitioner-2.png"));
            scanner.Scan(
                GetType().FindResourceStream("Practitioner-3.jpg"),
                Path.Combine(dir, "Practitioner-3.png"));

            Process.Start(dir);
        }
    }
}