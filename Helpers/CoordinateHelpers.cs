using Google.Cloud.Vision.V1;
using LineSegmentationAlgorithmToGCPVision.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LineSegmentationAlgorithmToGCPVision.Helpers
{
    /// <summary>
    /// Coordinate helpers
    /// </summary>
    public static class CoordinateHelpers
    {
        /// <summary>
        /// Computes the maximum y coordinate from the identified text blob
        /// </summary>
        /// <param name="data">Google vision api AnnotateImage response</param>
        /// <returns>Google vision api AnnotateImage response</returns>
        public static int GetYMax(AnnotateImageResponse data)
        {
            var v = data.TextAnnotations[0].BoundingPoly.Vertices;
            var yArray = new int[4];
            for (int i = 0; i < 4; i++)
                yArray[i] = v[i].Y;

            return yArray.Max();
        }

        /// <summary>
        /// Inverts the y axis coordinates for easier computation as the google vision starts the y axis from the bottom
        /// </summary>
        /// <param name="data">Google vision api AnnotateImage response</param>
        /// <param name="yMax">Maximum y coordinate from the identified text blob</param>
        /// <returns>Google vision api AnnotateImage response</returns>
        public static AnnotateImageResponse InvertAxis(AnnotateImageResponse data, int yMax)
        {
            for (int i = 1; i < data.TextAnnotations.Count; i++)
                for (int j = 0; j < 4; j++)
                    data.TextAnnotations[i].BoundingPoly.Vertices[j].Y = yMax - data.TextAnnotations[i].BoundingPoly.Vertices[j].Y; 

            return data;
        }

        public static IEnumerable<BoundingPolygon> GetBoundingPolygon(Google.Protobuf.Collections.RepeatedField<EntityAnnotation> mergedArray)
        {
            var result = new List<BoundingPolygon>();

            for(int i = 0; i < mergedArray.Count; i++)
            {
                List<Vertex> arr = new List<Vertex>();
                var h1 = mergedArray[i].BoundingPoly.Vertices[0].Y - mergedArray[i].BoundingPoly.Vertices[3].Y;
                var h2 = mergedArray[i].BoundingPoly.Vertices[1].Y - mergedArray[i].BoundingPoly.Vertices[2].Y;
                var h = h1;
                if (h2 > h1)
                    h = h2;

                var avgHeight = h * 0.6;

                arr.Add(mergedArray[i].BoundingPoly.Vertices[1]);
                arr.Add(mergedArray[i].BoundingPoly.Vertices[0]);

                var line1 = GetRectangle(arr, true, avgHeight, true);
                
                arr.Clear();
                arr.Add(mergedArray[i].BoundingPoly.Vertices[2]);
                arr.Add(mergedArray[i].BoundingPoly.Vertices[3]);

                var line2 = GetRectangle(arr, true, avgHeight, false);

                result.Add(new BoundingPolygon()
                {
                    EntityAnnotation = mergedArray[i],
                    Matched = false,
                    Match = new List<(int, int)>(),
                    LineNum = i,
                    BigBb = CreateRectCoordinates(line1, line2)
                });
            }

            return result;
        }

        public static IEnumerable<BoundingPolygon> CombineBoundingPolygon(IEnumerable<BoundingPolygon> mergedList)
        {
            var mergedArray = mergedList.ToArray();
            // select one word from the array
            for (int i = 0; i < mergedArray.Length; i++)
            {
                var bigBb = mergedArray[i].BigBb;
                // iterate through all the array to find the match
                for (int k = i; k < mergedArray.Length; k++)
                {
                    // Do not compare with the own bounding box and which was not matched with a line
                    if(k != i && !mergedArray[k].Matched)
                    {
                        var insideCount = 0;
                        for(int j = 0; j < 4; j++)
                        {
                            var coordinate = mergedArray[k].EntityAnnotation.BoundingPoly.Vertices[j];
                            if (IsInside((coordinate.X, coordinate.Y), new (double X, double Y)[] { bigBb.corner1, bigBb.corner2, bigBb.corner3, bigBb.corner4 }))
                                insideCount++;
                        }

                        if(insideCount == 4)
                        {
                            var match = (matchCount: insideCount, matchLineNum: k);
                            mergedArray[i].Match.Add(match);
                            mergedArray[k].Matched = true;
                        }
                    }
                }
            }

            return mergedArray.ToList();
        }

        public static (double xMin, double xMax, double yMin, double yMax) GetRectangle(List<Vertex> vertices, bool isRoundValues, 
                                                                                        double avgHeight, bool isAdd)
        {
            double vertices1Y = 0.0;
            double vertices0Y = 0.0;
            if (isAdd)
            {
                vertices1Y = vertices[1].Y + avgHeight;
                vertices0Y = vertices[0].Y + avgHeight;
            }
            else
            {
                vertices1Y = vertices[1].Y - avgHeight;
                vertices0Y = vertices[0].Y - avgHeight;
            }

            var yDiff = vertices1Y - vertices0Y;
            var xDiff = vertices[1].X - vertices[0].X;

            var gradient = yDiff / xDiff;

            var xThreshMin = 1;
            var xThreshMax = 2000;

            var yMin = 0.0;
            var yMax = 0.0;
            if(gradient == 0)
            {
                // Extend the lines
                yMin = vertices0Y;
                yMax = vertices0Y;
            }
            else
            {
                yMin = vertices0Y - (gradient * (vertices[0].X - xThreshMin));
                yMax = vertices0Y + (gradient * (xThreshMax - vertices[0].X));
            }

            if (isRoundValues)
            {
                yMin = Math.Round(yMin);
                yMax = Math.Round(yMax);
            }

            return (xMin: xThreshMin, xMax: xThreshMax, yMin, yMax);
        }

        public static ((double X, double Y) corner1, (double X, double Y) corner2, (double X, double Y) corner3, (double X, double Y) corner4)
            CreateRectCoordinates((double xMin, double xMax, double yMin, double yMax) line1, (double xMin, double xMax, double yMin, double yMax) line2)
        {
            return ((line1.xMin, line1.yMin), (line1.xMax, line1.yMax), (line2.xMax, line2.yMax), (line2.xMin, line2.yMin));
        }

        // ray-casting algorithm based on
        // http://www.ecse.rpi.edu/Homepages/wrf/Research/Short_Notes/pnpoly.html
        public static bool IsInside((double X, double Y) point, (double X, double Y)[] vs)
        {
            var x = point.X;
            var y = point.Y;

            var inside = false;

            for(int i = 0, j = vs.Length - 1; i < vs.Length; j = i++)
            {
                var xi = vs[i].X;
                var yi = vs[i].Y;

                var xj = vs[j].X;
                var yj = vs[j].Y;

                var intersect = ((yi > y) != (yj > y)) && (x < ((xj - xi) * (y - yi) / (yj - yi) + xi));
                if (intersect)
                    inside = !inside;
            }

            return inside;
        }
    }
}
