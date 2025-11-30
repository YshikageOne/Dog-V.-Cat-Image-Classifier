using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace DogVCatClassifier.Functionality
{
    public class ClassificationResults
    {
        public bool IsDog { get; set; }
        public float Confidence { get; set; }
        
        //flags uncertain result when confidence stays near fifty
        public bool IsUncertain { get; set; }
        
        //limit where classification becomes uncertain for mixed images
        public const float UncertaintyThreshold = 0.65f;
    }

    public class PetClassifier : IDisposable
    {
        private string currentModel = "v2";

        private readonly string modelV2Path;
        private readonly string modelV3Path;

        //onnx runtime sessions for each model
        private InferenceSession? sessionV2;
        private InferenceSession? sessionV3;
        
        private bool modelV2Loaded = false;
        private bool modelV3Loaded = false;

        //model input dimensions expected by training
        private const int ImageWidth = 150;
        private const int ImageHeight = 150;
        private const int Channels = 3;

        //constructor locates and loads models
        public PetClassifier()
        {
            //search common folders for models
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            string[] possiblePaths = new[]
            {
                Path.Combine(baseDirectory, "Models"),
                Path.Combine(baseDirectory, "assets", "Models"),
                Path.Combine(baseDirectory, "..", "..", "..", "assets", "Models"),
            };

            string? modelsFolder = null;
            foreach (var path in possiblePaths)
            {
                if (Directory.Exists(path))
                {
                    modelsFolder = path;
                    break;
                }
            }

            if (modelsFolder == null)
            {
                modelsFolder = Path.Combine(baseDirectory, "Models");
                Directory.CreateDirectory(modelsFolder);
            }

            //pick onnx model file paths
            modelV2Path = Path.Combine(modelsFolder, "version2.onnx");
            modelV3Path = Path.Combine(modelsFolder, "version3.onnx");

            //load sessions immediately
            LoadModels();
        }

        private void LoadModels()
        {
            var missingModels = new List<string>();

            //attempt to load model v2
            if (File.Exists(modelV2Path))
            {
                try
                {
                    Console.WriteLine($"Loading ONNX model: {modelV2Path}");
                    sessionV2 = new InferenceSession(modelV2Path);
                    modelV2Loaded = true;
                    Console.WriteLine("Model V2 loaded successfully!");
                    PrintModelInfo(sessionV2, "V2");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading Model V2: {ex.Message}");
                    missingModels.Add("version2.onnx");
                }
            }
            else
            {
                missingModels.Add("version2.onnx");
            }

            //attempt to load model v3
            if (File.Exists(modelV3Path))
            {
                try
                {
                    Console.WriteLine($"Loading ONNX model: {modelV3Path}");
                    sessionV3 = new InferenceSession(modelV3Path);
                    modelV3Loaded = true;
                    Console.WriteLine("Model V3 loaded successfully!");
                    PrintModelInfo(sessionV3, "V3");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading Model V3: {ex.Message}");
                    missingModels.Add("version3.onnx");
                }
            }
            else
            {
                missingModels.Add("version3.onnx");
            }

            //notify user when any model is missing
            if (missingModels.Count > 0)
            {
                string message = "ONNX model files not found:\n\n";
                foreach (var model in missingModels)
                {
                    message += $"• {model}\n";
                }
                message += $"\nExpected location: {Path.GetDirectoryName(modelV2Path)}\n\n";
                message += "Please ensure the ONNX model files are in the Models folder.\n\n";
                message += "The application will use simulated classification until models are available.";

                System.Windows.Forms.MessageBox.Show(
                    message,
                    "ONNX Models Required",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information
                );
            }
        }

        private void PrintModelInfo(InferenceSession session, string modelName)
        {
            Console.WriteLine($"\n--- Model {modelName} Info ---");
            try
            {
                Console.WriteLine("Inputs:");
                foreach (var input in session.InputMetadata)
                {
                    var dims = string.Join(", ", input.Value.Dimensions);
                    Console.WriteLine($"  {input.Key}: [{dims}] ({input.Value.ElementType})");
                }
                
                Console.WriteLine("Outputs:");
                foreach (var output in session.OutputMetadata)
                {
                    var dims = string.Join(", ", output.Value.Dimensions);
                    Console.WriteLine($"  {output.Key}: [{dims}] ({output.Value.ElementType})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not print model info: {ex.Message}");
            }
            Console.WriteLine("-------------------\n");
        }

        //select active model identifier
        public void SetModel(string modelVersion)
        {
            currentModel = modelVersion.ToLower();
        }

        //expose current model identifier
        public string GetCurrentModel()
        {
            return currentModel;
        }

        //provide async classification wrapper
        public async Task<ClassificationResults> ClassifyImageAsync(string imagePath)
        {
            return await Task.Run(() => ClassifyImage(imagePath));
        }

        //perform sync classification
        public ClassificationResults ClassifyImage(string imagePath)
        {
            try
            {
                using (var bitmap = new Bitmap(imagePath))
                {
                    using (var resized = ResizeImage(bitmap, ImageWidth, ImageHeight))
                    {
                        if (currentModel == "v2" && modelV2Loaded && sessionV2 != null)
                        {
                            return RunInference(sessionV2, resized);
                        }
                        else if (currentModel == "v3" && modelV3Loaded && sessionV3 != null)
                        {
                            return RunInference(sessionV3, resized);
                        }
                        else
                        {
                            //fallback to simulated scoring
                            return SimulateClassification(resized);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during classification: {ex.Message}");
                return new ClassificationResults { IsDog = false, Confidence = 0.5f, IsUncertain = true };
            }
        }

        private ClassificationResults RunInference(InferenceSession session, Bitmap image)
        {
            try
            {
                //gather input metadata for tensor creation
                var inputMeta = session.InputMetadata.First();
                string inputName = inputMeta.Key;
                var inputDims = inputMeta.Value.Dimensions;
                
                //prepare tensor matching expected shape
                DenseTensor<float> inputTensor;
                
                //decide between nchw and nhwc layout
                bool isNchw = inputDims.Length == 4 && inputDims[1] == Channels;
                
                if (isNchw)
                {
                    //nchw layout batch channels height width
                    inputTensor = PreprocessImageNchw(image);
                }
                else
                {
                    //nhwc layout batch height width channels
                    inputTensor = PreprocessImageNhwc(image);
                }

                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
                };

                //execute inference pass
                using var results = session.Run(inputs);
                
                //extract tensor outputs
                var output = results.First();
                var outputTensor = output.AsTensor<float>();
                float[] probabilities = outputTensor.ToArray();
                
                Console.WriteLine($"Raw output: [{string.Join(", ", probabilities.Select(p => p.ToString("F4")))}]");

                //interpret probabilities according to output shape
                bool isDog;
                float confidence;

                if (probabilities.Length == 1)
                {
                    //single sigmoid output higher than half means dog
                    float value = probabilities[0];
                    isDog = value > 0.5f;
                    confidence = isDog ? value : (1 - value);
                }
                else if (probabilities.Length == 2)
                {
                    //two outputs cat then dog choose higher entry
                    isDog = probabilities[1] > probabilities[0];
                    confidence = isDog ? probabilities[1] : probabilities[0];
                }
                else
                {
                    //unknown layout fallback to first entry
                    isDog = probabilities[0] > 0.5f;
                    confidence = Math.Abs(probabilities[0] - 0.5f) * 2;
                }

                //clamp confidence into valid window
                float clampedConfidence = Math.Max(0.5f, Math.Min(1.0f, confidence));
                
                return new ClassificationResults
                {
                    IsDog = isDog,
                    Confidence = clampedConfidence,
                    //mark uncertain when confidence under threshold
                    IsUncertain = clampedConfidence < ClassificationResults.UncertaintyThreshold
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Inference error: {ex.Message}");
                throw;
            }
        }

        private DenseTensor<float> PreprocessImageNhwc(Bitmap image)
        {
            //build tensor with nhwc layout
            var tensor = new DenseTensor<float>(new[] { 1, ImageHeight, ImageWidth, Channels });

            BitmapData? bitmapData = null;
            try
            {
                bitmapData = image.LockBits(
                    new Rectangle(0, 0, ImageWidth, ImageHeight),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb
                );

                int bytesPerRow = bitmapData.Stride;
                byte[] pixelData = new byte[bytesPerRow * ImageHeight];
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

                for (int y = 0; y < ImageHeight; y++)
                {
                    for (int x = 0; x < ImageWidth; x++)
                    {
                        int pixelIndex = y * bytesPerRow + x * 3;
                        
                        //convert bgr bytes to normalized rgb
                        float r = pixelData[pixelIndex + 2] / 255.0f;
                        float g = pixelData[pixelIndex + 1] / 255.0f;
                        float b = pixelData[pixelIndex + 0] / 255.0f;

                        //write values in nhwc order
                        tensor[0, y, x, 0] = r;
                        tensor[0, y, x, 1] = g;
                        tensor[0, y, x, 2] = b;
                    }
                }
            }
            finally
            {
                if (bitmapData != null)
                {
                    image.UnlockBits(bitmapData);
                }
            }

            return tensor;
        }

        private DenseTensor<float> PreprocessImageNchw(Bitmap image)
        {
            //build tensor with nchw layout
            var tensor = new DenseTensor<float>(new[] { 1, Channels, ImageHeight, ImageWidth });

            BitmapData? bitmapData = null;
            try
            {
                bitmapData = image.LockBits(
                    new Rectangle(0, 0, ImageWidth, ImageHeight),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb
                );

                int bytesPerRow = bitmapData.Stride;
                byte[] pixelData = new byte[bytesPerRow * ImageHeight];
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixelData, 0, pixelData.Length);

                for (int y = 0; y < ImageHeight; y++)
                {
                    for (int x = 0; x < ImageWidth; x++)
                    {
                        int pixelIndex = y * bytesPerRow + x * 3;
                        
                        //convert bgr bytes to normalized rgb
                        float r = pixelData[pixelIndex + 2] / 255.0f;
                        float g = pixelData[pixelIndex + 1] / 255.0f;
                        float b = pixelData[pixelIndex + 0] / 255.0f;

                        //write values in nchw order
                        tensor[0, 0, y, x] = r;
                        tensor[0, 1, y, x] = g;
                        tensor[0, 2, y, x] = b;
                    }
                }
            }
            finally
            {
                if (bitmapData != null)
                {
                    image.UnlockBits(bitmapData);
                }
            }

            return tensor;
        }

        private Bitmap ResizeImage(Bitmap image, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.DrawImage(image, 0, 0, width, height);
            }
            return resized;
        }

        //fallback simulation when onnx sessions missing
        private ClassificationResults SimulateClassification(Bitmap image)
        {
            //rough color heuristic not real inference
            //derives pseudo random confidence from average color
            BitmapData? bitmapData = null;
            try
            {
                bitmapData = image.LockBits(
                    new Rectangle(0, 0, image.Width, image.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb
                );

                long totalR = 0, totalG = 0, totalB = 0;
                int pixelCount = image.Width * image.Height;
                byte[] pixels = new byte[bitmapData.Stride * image.Height];
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, pixels, 0, pixels.Length);

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        int idx = y * bitmapData.Stride + x * 3;
                        totalB += pixels[idx];
                        totalG += pixels[idx + 1];
                        totalR += pixels[idx + 2];
                    }
                }

                float avgR = totalR / (float)pixelCount / 255f;
                float avgG = totalG / (float)pixelCount / 255f;
                float avgB = totalB / (float)pixelCount / 255f;

                //simple heuristic combining color channels
                float score = avgR * 0.4f + avgG * 0.35f + avgB * 0.25f;
                bool isDog = score > 0.45f;
                
                var random = new Random((int)(score * 10000));
                float confidence = 0.6f + (float)random.NextDouble() * 0.25f;

                return new ClassificationResults
                {
                    IsDog = isDog,
                    Confidence = confidence,
                    IsUncertain = confidence < ClassificationResults.UncertaintyThreshold
                };
            }
            finally
            {
                if (bitmapData != null)
                {
                    image.UnlockBits(bitmapData);
                }
            }
        }

        public void Dispose()
        {
            sessionV2?.Dispose();
            sessionV3?.Dispose();
            sessionV2 = null;
            sessionV3 = null;
        }
    }
}
