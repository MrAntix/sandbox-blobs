using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using Accord.Imaging.Filters;
using Point = AForge.Point;

namespace Sandbox.Blobs
{
    public class Scanner
    {
        public ScannerResult[] Scan(Stream imageStream, string fileName)
        {
            var bitmap = GetCorrectedBitmap(imageStream);

            // Find +'s
            var blobCounter = new BlobCounter
                {
                    FilterBlobs = true,
                    MinWidth = 15,
                    MaxWidth = 35,
                    MinHeight = 15,
                    MaxHeight = 35,
                };

            blobCounter.ProcessImage(bitmap);

            var leftSet = new Set(p => p.X, (s, c2) => s.Value > c2 + s.Threshold);
            var rightSet = new Set(p => p.X, (s, c2) => s.Value < c2 - s.Threshold);

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

            new PointsMarker(leftSet.Points, Color.Green, 10)
                .ApplyInPlace(bitmap);
            new PointsMarker(rightSet.Points, Color.Red, 10)
                .ApplyInPlace(bitmap);

            // Find Marks
            blobCounter = new BlobCounter
                {
                    FilterBlobs = true,
                    BackgroundThreshold = Color.FromArgb(255, 85, 85, 85),
                    MinWidth = 25,
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

                        rowSet.Add(blob.CenterOfGravity);

                        new PointsMarker(rowSet.Points, Color.Orange, 20)
                            .ApplyInPlace(bitmap);
                    }
                }
                else
                {
                    foreach (var point in leftSet.Points)
                    {
                        var rowSet = new Set(p => p.Y);
                        rowSet.Init(point);

                        rowSet.Add(blob.CenterOfGravity);

                        new PointsMarker(rowSet.Points, Color.Orange, 20)
                            .ApplyInPlace(bitmap);
                    }
                }
            }

            bitmap.Save(fileName, ImageFormat.Png);

            return new ScannerResult[] {};
        }

        static Bitmap GetCorrectedBitmap(Stream imageStream)
        {
            var bitmap = new Bitmap(imageStream);

            new Invert().ApplyInPlace(bitmap);

            bitmap = MakeGrayBitmap(bitmap);
            bitmap = DeskewBitmap(bitmap);
            bitmap = ColorBitmap(bitmap);

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
            using (bitmap)
            {
                return new ResizeBilinear(1250, 1900).Apply(bitmap);
            }
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

        public void Add(Point point)
        {
            var value = _getValue(point);

            if (!Value.HasValue
                || _resetIf(this, value))
            {
                Value = value;
                Points = new List<IntPoint>();
            }

            if (Value > value + Threshold
                || Value < value - Threshold) return;

            Points.Add(point.Round());
        }
    }
}