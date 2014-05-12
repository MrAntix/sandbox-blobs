using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using AForge.Math.Geometry;
using Accord.Imaging;
using Accord.Imaging.Filters;

namespace Sandbox.Blobs
{
    public class AccordScanner
    {
        readonly List<Bitmap> _targets = new List<Bitmap>();

        public void AddTarget(Stream imageStream)
        {
            using (var bitmap = new Bitmap(imageStream))
            {
                var surf = new SpeededUpRobustFeaturesDetector(.000001F);

                var points = surf
                    .ProcessImage(bitmap);

                var features = new FeaturesMarker(points);
                features.Apply(bitmap).Save("target.png", ImageFormat.Png);

                //var edgePoints = blobCounter.GetBlobsEdgePoints(blobs.Single());
                //_targets.Add(edgePoints);
            }
        }

        public ScannerResult[] Scan(Stream imageStream)
        {
            var bitmap = new Bitmap(imageStream);

            var surf = new SpeededUpRobustFeaturesDetector(.025F);

            var points = surf
                .ProcessImage(bitmap);

            var shapeChecker = new SimpleShapeChecker();

            foreach (var target in _targets)
            {
                var targetPoints = surf.ProcessImage(target);

                //shapeChecker.CheckIfPointsFitShape(points, targetPoints);
            }

            var features = new FeaturesMarker(points);

            features.Apply(bitmap).Save("it.png", ImageFormat.Png);

            return null;
        }
    }
}