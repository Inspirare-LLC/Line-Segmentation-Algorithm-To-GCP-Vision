## Line Segmentation Algorithm To GCP Vision
Port of [Line segmentation algorithm to GCP vision](https://github.com/sshniro/line-segmentation-algorithm-to-gcp-vision) in C# language as .NET standard library

## Usage
Firstly use Google Vision API to get annotation results as such:
````
using LineSegmentationAlgorithmToGCPVision;
...
var result = client.Annotate(annotateImageRequest);
````
Where `client` is `ImageAnnotatorClient` and `annotateImageRequest` is `AnnotateImageRequest`
After that use this library to process the result as such:

````
var text = LineSegmentation.InitLineSegmentation(result);
````
This will return an `IEnumerable<string>` with lines of connected text.

## More info
For more information about the algorithm itself please visit [Original repository](https://github.com/sshniro/line-segmentation-algorithm-to-gcp-vision) 

## Future Work
- Maintain the port
- Make the project into a nuget package
- Improve and refactor algorithm with C# syntax to perform better
- Fix bugs