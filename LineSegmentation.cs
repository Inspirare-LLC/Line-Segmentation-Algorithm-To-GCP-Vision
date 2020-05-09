using Google.Cloud.Vision.V1;
using LineSegmentationAlgorithmToGCPVision.Helpers;
using LineSegmentationAlgorithmToGCPVision.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LineSegmentationAlgorithmToGCPVision
{
    public static class LineSegmentation
    {
        public static IEnumerable<string> InitLineSegmentation(AnnotateImageResponse data)
        {
            var yMax = CoordinateHelpers.GetYMax(data);
            data = CoordinateHelpers.InvertAxis(data, yMax);

            // The first index refers to the auto identified words which belongs to a sings line
            var lines = data.TextAnnotations[0].Description.Split('\n');

            // gcp vision full text
            var rawText = new List<EntityAnnotation>();
            for (int i = 1; i < data.TextAnnotations.Count; i++)
                rawText.Add(data.TextAnnotations[i]);

            // reverse to use lifo, because array.shift() will consume 0(n)
            lines = lines.Reverse().ToArray();
            rawText.Reverse();

            var mergedArray = GetMergedLines(lines, rawText);

            var boundingPolygon = CoordinateHelpers.GetBoundingPolygon(mergedArray);

            var combinedPolygon = CoordinateHelpers.CombineBoundingPolygon(boundingPolygon);

            return ConstructLineWithBoundingPolygon(combinedPolygon);
        }

        public static IEnumerable<string> ConstructLineWithBoundingPolygon(IEnumerable<BoundingPolygon> mergedList)
        {
            var finalArray = new List<string>();
            var mergedArray = mergedList.ToArray();

            for (int i = 0; i < mergedArray.Length; i++)
            {
                if (!mergedArray[i].Matched)
                {
                    if(mergedArray[i].Match.Count == 0)
                    {
                        finalArray.Add(mergedArray[i].EntityAnnotation.Description);
                    }
                    else
                    {
                        // arrangeWordsInOrder(mergedArray, i);
                        // let index = mergedArray[i]['match'][0]['matchLineNum'];
                        // let secondPart = mergedArray[index].description;
                        // finalArray.push(mergedArray[i].description + ' ' +secondPart);
                        finalArray.Add(ArrangeWordsInOrder(mergedArray, i));
                    }
                }
            }

            return finalArray;
        }

        public static string ArrangeWordsInOrder(BoundingPolygon[] mergedArray, int k)
        {
            var mergedLine = "";
            var line = mergedArray[k].Match;

            // [0]['matchLineNum']
            for(int i = 0; i < line.Count; i++)
            {
                var index = line[i].MatchLineNum;
                var matchedWordForLine = mergedArray[index].EntityAnnotation.Description;

                var mainX = mergedArray[k].EntityAnnotation.BoundingPoly.Vertices[0].X;
                var compareX = mergedArray[index].EntityAnnotation.BoundingPoly.Vertices[0].X;

                if(compareX > mainX)
                    mergedLine = $"{mergedArray[k].EntityAnnotation.Description} {matchedWordForLine}";
                else
                    mergedLine = $"{matchedWordForLine} {mergedArray[k].EntityAnnotation.Description}";
            }

            return mergedLine;
        }

        public static Google.Protobuf.Collections.RepeatedField<EntityAnnotation> GetMergedLines(string[] lines, List<EntityAnnotation> rawText)
        {
            var mergedArray = new Google.Protobuf.Collections.RepeatedField<EntityAnnotation>();
            var linesList = lines.ToList();

            while (linesList.Count != 1)
            {
                var l = linesList.Last();
                linesList.RemoveAt(linesList.Count - 1);

                if (String.IsNullOrEmpty(l))
                    continue;

                var ll = l;
                var status = true;

                EntityAnnotation mergedElement = new EntityAnnotation();

                while (true)
                {
                    var wElement = rawText.Last();
                    rawText.RemoveAt(rawText.Count - 1);
                    var w = wElement.Description;

                    var index = l.IndexOf(w);

                    // check if the word is inside
                    l = l.Substring(index + w.Length);
                    if (status)
                    {
                        status = false;
                        // set starting coordinates
                        mergedElement = wElement;
                    }

                    if (String.IsNullOrEmpty(l))
                    {
                        // set ending coordinates
                        mergedElement.Description = ll;
                        mergedElement.BoundingPoly.Vertices[1] = wElement.BoundingPoly.Vertices[1];
                        mergedElement.BoundingPoly.Vertices[2] = wElement.BoundingPoly.Vertices[2];
                        mergedArray.Add(mergedElement);
                        break;
                    }
                }
            }

            return mergedArray;
        }
    }
}
