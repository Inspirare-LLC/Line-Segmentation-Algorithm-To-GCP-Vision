## Line Segmentation Algorithm To GCP Vision
Port of [Line segmentation algorithm to GCP vision](https://github.com/sshniro/line-segmentation-algorithm-to-gcp-vision) in C# language as .NET standard library

Nuget package: [Inspirare.Google.Vision.API.LineSegmentation](https://www.nuget.org/packages/Inspirare.Google.Vision.API.LineSegmentation/)

## Usage
Firstly use Google Vision API to get annotation results as such:
````
using LineSegmentationAlgorithmToGCPVision;
...
var result = client.Annotate(annotateImageRequest);
````
Where `client` is `ImageAnnotatorClient` and `annotateImageRequest` is `AnnotateImageRequest`
Remember to include a `Feature.Types.Type.DocumentTextDetection` feature in the `AnnotateImageRequest`.

After that use this library to process the result as such:

````
var text = LineSegmentation.InitLineSegmentation(result);
````
This will return an `IEnumerable<string>` with lines of connected text.

## More info
For more information about the algorithm itself please visit [Original repository](https://github.com/sshniro/line-segmentation-algorithm-to-gcp-vision) 

## Issues
File issues in the `Issue` section

## Contributions
Any contributions are welcome in the form of a Pull Request

## Future Work
- Maintain the port
~~- Make the project into a nuget package~~
- Improve and refactor algorithm with C# syntax to perform better
- Fix bugs
- Add CI
