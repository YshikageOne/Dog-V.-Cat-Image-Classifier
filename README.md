# Dog vs Cat Classifier by clyde

Windows desktop app that loads a trained ONNX model to distinguish between cat and dog photos. The UI lets you pick an image, runs it through the model, and shows the predicted label with confidence.

## Getting Started
1. Restore NuGet packages (`dotnet restore` or build inside Visual Studio).
2. Open the `DogVCatClassifier.sln` (or `.csproj`) and build.
3. Run the app and choose an image via the file picker.

## Models
Pretrained ONNX models live under `assets/Models`. Update the path in `PetClassifier.cs` if you add new versions.

## Disclaimer
The model is tuned specifically for cat and dog photos. It works well on typical pet images but may become confused or produce inaccurate outputs when the picture does not contain a cat or a dog.
