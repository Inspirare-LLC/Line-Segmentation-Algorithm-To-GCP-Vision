using Google.Cloud.Vision.V1;
using System;
using System.Collections.Generic;
using System.Text;

namespace LineSegmentationAlgorithmToGCPVision.Models
{
    public class BoundingPolygon
    {
        public ((double X, double Y) corner1, (double X, double Y) corner2, (double X, double Y) corner3, (double X, double Y) corner4) BigBb { get; set; }
        public int LineNum { get; set; }
        public List<(int MatchCount, int MatchLineNum)> Match { get; set; }
        public bool Matched { get; set; }
        public EntityAnnotation EntityAnnotation { get; set; }
    }
}
