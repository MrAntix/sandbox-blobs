using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Point = AForge.Point;

namespace Sandbox.Blobs
{
    public class Scanner
    {
        const float BLOB_WIDTH = 52F;

        public ScannerResult Scan(
            Stream imageStream,
            string fileName,
            int candidateNumberDigits, int cols,
            bool debug)
        {
            var points = Scan(imageStream, fileName, debug)
                .OrderBy(p => p.Y)
                .ToArray();

            var result = new ScannerResult();
            if (candidateNumberDigits > 0)
            {
                var chars = new char[candidateNumberDigits];
                for (var i = 0; i < candidateNumberDigits; i++)
                {
                    chars[i] = (char) (48 + (points[i].X - 700)/BLOB_WIDTH);
                }

                result.CandidateNumber = new string(chars);
            }

            var answerCols = new Columns(cols);
            for (var i = candidateNumberDigits; i < points.Length; i++)
            {
                answerCols.Add(points[i]);
            }
            result.Answers = (from c in answerCols
                              from a in c.Answers
                              select a).ToArray();

            return result;
        }

        static IEnumerable<Point> Scan(
            Stream imageStream, string fileName, bool debug)
        {
            var bitmap = GetCorrectedBitmap(imageStream, debug);

            // Find +'s
            var blobCounter = new BlobCounter
                {
                    FilterBlobs = true,
                    MinWidth = 15,
                    MaxWidth = 45,
                    MinHeight = 15,
                    MaxHeight = 45,
                };

            blobCounter.ProcessImage(bitmap);

            var leftSet = new Set(p => p.X, (s, c) => s.Value > c + s.Threshold);
            var rightSet = new Set(p => p.X, (s, c) => s.Value < c - s.Threshold);

            foreach (var blob in
                blobCounter.GetObjectsInformation())
            {
                if (blob.CenterOfGravity.Y > 150
                    && blob.CenterOfGravity.Y < 1700)
                {
                    if (blob.CenterOfGravity.X < bitmap.Width/2F) leftSet.Add(blob.CenterOfGravity);
                    else rightSet.Add(blob.CenterOfGravity);
                }
            }

            //new PointsMarker(leftSet.Points, Color.Green, 10)
            //    .ApplyInPlace(bitmap);
            //new PointsMarker(rightSet.Points, Color.Red, 10)
            //    .ApplyInPlace(bitmap);

            // Find Marks
            blobCounter = new BlobCounter
                {
                    FilterBlobs = true,
                    BackgroundThreshold = Color.FromArgb(255, 85, 85, 85),
                    MinWidth = 30,
                    MaxWidth = 80,
                    MinHeight = 12,
                    MaxHeight = 50,
                };

            blobCounter.ProcessImage(bitmap);

            foreach (var blob in
                blobCounter.GetObjectsInformation())
            {
                if (blob.CenterOfGravity.X > bitmap.Width/2F)
                {
                    foreach (var point in rightSet.Points)
                    {
                        var rowSet = new Set(p => p.Y);
                        rowSet.Init(point);

                        if (rowSet.Add(blob.CenterOfGravity))
                        {
                            var blobPoint = blob.CenterOfGravity.Round();
                            if (debug) Draw(bitmap, blobPoint, Color.Orange, 20);

                            yield return blobPoint;
                        }
                    }
                }
                else
                {
                    foreach (var point in leftSet.Points)
                    {
                        var rowSet = new Set(p => p.Y);
                        rowSet.Init(point);

                        if (rowSet.Add(blob.CenterOfGravity))
                        {
                            var blobPoint = blob.CenterOfGravity.Round();
                            if (debug) Draw(bitmap, blobPoint, Color.Orange, 20);

                            yield return blobPoint;
                        }
                    }
                }
            }

            if (debug) bitmap.Save(fileName, ImageFormat.Png);
        }

        static void Draw(Bitmap bitmap, IntPoint intPoint, Color markerColor, int radius)
        {
            using (var gfx = Graphics.FromImage(bitmap))
            using (var brush = new SolidBrush(markerColor))
            {
                gfx.FillRectangle(
                    brush,
                    new Rectangle(intPoint.X - (radius/2), intPoint.Y - (radius/2), radius, radius));
            }
        }

        static Bitmap GetCorrectedBitmap(
            Stream imageStream, bool debug)
        {
            var bitmap = new Bitmap(imageStream);

            new Invert().ApplyInPlace(bitmap);

            bitmap = MakeGrayBitmap(bitmap);
            bitmap = DeskewBitmap(bitmap);
            if (debug) bitmap = ColorBitmap(bitmap);

            return ResizeAndCropBitmap(bitmap);
        }

        static Bitmap ColorBitmap(Bitmap bitmap)
        {
            using (bitmap)
            {
                return new GrayscaleToRGB().Apply(bitmap);
            }
        }

        static Bitmap MakeGrayBitmap(Bitmap bitmap)
        {
            using (bitmap)
            {
                return Grayscale.CommonAlgorithms.BT709.Apply(bitmap);
            }
        }

        static Bitmap DeskewBitmap(Bitmap bitmap)
        {
            var skewChecker = new DocumentSkewChecker();
            var angle = skewChecker.GetSkewAngle(bitmap);

            if (Math.Abs(angle) > 45) return bitmap;

            var rotationFilter = new RotateBilinear(-angle)
                {
                    FillColor = Color.White
                };

            return rotationFilter.Apply(bitmap);
        }

        static Bitmap ResizeAndCropBitmap(Bitmap bitmap)
        {
            const int width = 1250;
            const int height = 1900;
            const int edge = 20;

            using (bitmap)
            using (var resizedBitmap = new ResizeBilinear(width, height).Apply(bitmap))
            {
                // Find corners
                var blobCounter = new BlobCounter
                    {
                        FilterBlobs = true,
                        // BackgroundThreshold = Color.FromArgb(255, 25, 25, 25),
                        MinWidth = 20,
                        MaxWidth = 100,
                        MinHeight = 20,
                        MaxHeight = 100,
                    };

                blobCounter.ProcessImage(resizedBitmap);

                var corners = new Blob[2];

                foreach (var blob in
                    blobCounter.GetObjectsInformation())
                {
                    if (blob.CenterOfGravity.X < edge
                        || blob.CenterOfGravity.X > width - edge
                        || blob.CenterOfGravity.Y < edge
                        || blob.CenterOfGravity.Y > height - edge) continue;

                    if (corners[0] == null
                        || Closer(0, 0, blob, corners[0]))
                        corners[0] = blob;

                    if (corners[1] == null
                        || Closer(width, height, blob, corners[1]))
                        corners[1] = blob;
                }

                //new PointsMarker(corners
                //                     .Where(c => c != null)
                //                     .Select(c => c.CenterOfGravity.Round()), Color.CadetBlue, 50)
                //    .ApplyInPlace(resizedBitmap);

                var topLeft = corners[0].CenterOfGravity.Round();
                var bottomRight = corners[1].CenterOfGravity.Round();

                using (var croppedBitmap = new Crop(
                    new Rectangle(
                        topLeft.X - edge, topLeft.Y - edge,
                        bottomRight.X - topLeft.X + edge*2, bottomRight.Y - topLeft.Y + edge*2))
                    .Apply((resizedBitmap)))
                {
                    return new ResizeBilinear(1250, 1900).Apply(croppedBitmap);
                }
            }
        }

        static bool Closer(float aX, float aY, Blob b, Blob c)
        {
            var a = new Point(aX, aY);
            return Distance(a, b.CenterOfGravity) < Distance(a, c.CenterOfGravity);
        }

        static double Distance(Point a, Point b)
        {
            return Math.Abs(
                Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2)
                );
        }

        public class Columns : IEnumerable<Column>
        {
            readonly List<Column> _columns;

            public Columns(int cols)
            {
                _columns = new List<Column>();

                switch (cols)
                {
                    default:
                        throw new NotSupportedException();
                    case 1:
                        _columns.Add(new Column {Offset = 112});
                        break;
                    case 2:
                        _columns.Add(new Column { Offset = 965 });
                        _columns.Add(new Column { Offset = 680 });

                        break;
                    case 4:
                        _columns.Add(new Column { Offset = 965 });
                        _columns.Add(new Column { Offset = 680 });
                        _columns.Add(new Column { Offset = 398 });
                        _columns.Add(new Column { Offset = 118 });

                        break;
                }
            }

            public void Add(Point point)
            {
                var column = _columns.FirstOrDefault(c => point.X > c.Offset);

                if (column == null)
                    throw new Exception(
                        string.Format("No column was found for point ({0},{1})", point.X, point.Y));

                column.Answers.Add(
                    new string(new[] {(char) (65 + (point.X - column.Offset)/BLOB_WIDTH)})
                    );
            }

            public IEnumerator<Column> GetEnumerator()
            {
                return _columns.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public class Column
        {
            readonly List<string> _answers = new List<string>();
            public int Offset { get; set; }

            public List<string> Answers
            {
                get { return _answers; }
            }
        }

        public class Set
        {
            readonly Func<Point, float> _getValue;
            readonly Func<Set, float, bool> _resetIf;
            readonly int _threshold;

            public Set(
                Func<Point, float> getValue,
                Func<Set, float, bool> test = null,
                int threshold = 10)
            {
                Points = new List<IntPoint>();

                _resetIf = test ?? ((c1, c2) => false);
                _getValue = getValue;
                _threshold = threshold;
            }

            public float? Value { get; set; }
            public List<IntPoint> Points { get; set; }

            public int Threshold
            {
                get { return _threshold; }
            }

            public void Init(Point point)
            {
                Value = _getValue(point);
                Points = new List<IntPoint>();
            }

            public bool Add(Point point)
            {
                var value = _getValue(point);

                if (!Value.HasValue
                    || _resetIf(this, value))
                {
                    Value = value;
                    Points = new List<IntPoint>();
                }

                if (Value > value + Threshold
                    || Value < value - Threshold) return false;

                Points.Add(point.Round());

                return true;
            }
        }
    }
}