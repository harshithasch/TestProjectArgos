using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using SeleniumScreenshot = OpenQA.Selenium.Screenshot;
using OpenQA.Selenium;

namespace TestProjectArgos.Framework
{
    public static class PptScreenshotReport
    {
        private static readonly object SyncLock = new object();

        private static string? _presentationPath;
        private static string? _screenshotFolder;
        private static string? _templatePath;
        private static bool _initialized;

        public static string PresentationPath
        {
            get
            {
                EnsureInitialized();
                return _presentationPath!;
            }
        }

        /// <summary>
        /// Creates a new PowerPoint report for the current test run.
        /// Example: AutomationRun_001.pptx, AutomationRun_002.pptx.
        /// Call this once from OneTimeSetUp.
        /// </summary>
        public static void Initialize(
            string reportName = "AutomationRun",
            string? outputDirectory = null,
            string? templatePath = null)
        {
            lock (SyncLock)
            {
                if (_initialized)
                {
                    return;
                }

                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

                outputDirectory =
    AppDomain.CurrentDomain.BaseDirectory;

                templatePath ??= Path.Combine(
                    outputDirectory,
                    "ScreenshotTemplate.pptx");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException(
                        $"ScreenshotTemplate.pptx was not found. " +
                        $"Expected location: {templatePath}",
                        templatePath);
                }

                int runNumber = GetNextRunNumber(outputDirectory, reportName);

                string fileName =
                    $"{reportName}_{runNumber:D3}.pptx";

                _presentationPath = Path.Combine(
                    outputDirectory,
                    fileName);

                _screenshotFolder = Path.Combine(
                    outputDirectory,
                    $"{reportName}_{runNumber:D3}_Images");

                Directory.CreateDirectory(_screenshotFolder);

                File.Copy(
                    templatePath,
                    _presentationPath,
                    overwrite: false);

                _templatePath = templatePath;
                _initialized = true;

                UpdateFirstSlideAsCoverPage();

                Console.WriteLine(
                    $"PowerPoint screenshot report created: {_presentationPath}");
            }
        }

        /// <summary>
        /// Captures and adds a Selenium browser screenshot.
        /// </summary>
        public static void CaptureWebScreenshot(
            IWebDriver driver,
            string title,
            string? description = null)
        {
            if (driver == null)
            {
                throw new ArgumentNullException(nameof(driver));
            }

            EnsureInitialized();

            lock (SyncLock)
            {
                string screenshotPath =
                    CreateScreenshotFilePath(title);

                if (driver is not ITakesScreenshot screenshotDriver)
                {
                    throw new InvalidOperationException(
                        "The current WebDriver does not support screenshots.");
                }

                SeleniumScreenshot screenshot =
                    screenshotDriver.GetScreenshot();

                screenshot.SaveAsFile(screenshotPath);

                AddScreenshotSlide(
                    screenshotPath,
                    title,
                    description);
            }
        }

        /// <summary>
        /// Captures the complete Windows desktop.
        /// Useful for FlaUI, Argos simulator, MOVU, Vision Planner,
        /// confirmation popups and other desktop applications.
        /// </summary>
        public static void CaptureDesktopScreenshot(
    string title,
    string? description = null)
        {
            EnsureInitialized();

            lock (SyncLock)
            {
                string screenshotPath =
                    CreateScreenshotFilePath(title);

                System.Windows.Forms.Screen? primaryScreen =
                    System.Windows.Forms.Screen.PrimaryScreen;

                if (primaryScreen == null)
                {
                    throw new InvalidOperationException(
                        "Windows primary screen was not detected.");
                }

                Rectangle screenBounds =
                    primaryScreen.Bounds;

                Console.WriteLine(
                    "Capturing primary screen: " +
                    screenBounds.Width +
                    " x " +
                    screenBounds.Height +
                    ", Left=" +
                    screenBounds.Left +
                    ", Top=" +
                    screenBounds.Top);

                using Bitmap bitmap = new Bitmap(
                    screenBounds.Width,
                    screenBounds.Height,
                    PixelFormat.Format32bppArgb);

                using Graphics graphics =
                    Graphics.FromImage(bitmap);

                graphics.CopyFromScreen(
                    sourceX: screenBounds.Left,
                    sourceY: screenBounds.Top,
                    destinationX: 0,
                    destinationY: 0,
                    blockRegionSize: screenBounds.Size,
                    copyPixelOperation:
                        CopyPixelOperation.SourceCopy);

                bitmap.Save(
                    screenshotPath,
                    ImageFormat.Png);

                Console.WriteLine(
                    "Full-screen screenshot saved: " +
                    screenshotPath);

                AddScreenshotSlide(
                    screenshotPath,
                    title,
                    description);
            }
        }


        /// <summary>
        /// Adds an already available image to the PowerPoint report.
        /// </summary>
        public static void AddExistingScreenshot(
            string screenshotPath,
            string title,
            string? description = null)
        {
            EnsureInitialized();

            if (!File.Exists(screenshotPath))
            {
                throw new FileNotFoundException(
                    "Screenshot file was not found.",
                    screenshotPath);
            }

            lock (SyncLock)
            {
                AddScreenshotSlide(
                    screenshotPath,
                    title,
                    description);
            }
        }

        /// <summary>
        /// Captures a Selenium screenshot when a test fails.
        /// </summary>
        public static void CaptureFailureScreenshot(
            IWebDriver? driver,
            string testName,
            string failureMessage)
        {
            try
            {
                if (driver != null)
                {
                    CaptureWebScreenshot(
                        driver,
                        $"{testName} - FAILED",
                        failureMessage);
                }
                else
                {
                    CaptureDesktopScreenshot(
                        $"{testName} - FAILED",
                        failureMessage);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(
                    $"Unable to capture failure screenshot: {exception.Message}");
            }
        }

        /// <summary>
        /// Finishes the report. The Open XML document is saved
        /// after every screenshot, so this method mainly logs the path.
        /// </summary>
        public static void CompleteReport()
        {
            lock (SyncLock)
            {
                if (!_initialized)
                {
                    return;
                }

                Console.WriteLine(
                    $"Screenshot PowerPoint report saved: {_presentationPath}");
            }
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                Initialize();
            }
        }

        private static int GetNextRunNumber(
            string outputDirectory,
            string reportName)
        {
            string searchPattern = $"{reportName}_*.pptx";

            List<int> existingNumbers = Directory
                .GetFiles(outputDirectory, searchPattern)
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name =>
                {
                    string prefix = $"{reportName}_";

                    if (!name!.StartsWith(
                            prefix,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return 0;
                    }

                    string numberPart =
                        name.Substring(prefix.Length);

                    return int.TryParse(
                        numberPart,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int result)
                            ? result
                            : 0;
                })
                .Where(number => number > 0)
                .ToList();

            return existingNumbers.Count == 0
                ? 1
                : existingNumbers.Max() + 1;
        }

        private static string CreateScreenshotFilePath(
            string title)
        {
            string safeTitle = MakeSafeFileName(title);

            string timestamp =
                DateTime.Now.ToString(
                    "yyyyMMdd_HHmmss_fff",
                    CultureInfo.InvariantCulture);

            return Path.Combine(
                _screenshotFolder!,
                $"{timestamp}_{safeTitle}.png");
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "Screenshot";
            }

            foreach (char invalidCharacter
                     in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(
                    invalidCharacter,
                    '_');
            }

            return value.Length > 80
                ? value.Substring(0, 80)
                : value;
        }

        private static void UpdateFirstSlideAsCoverPage()
        {
            using PresentationDocument presentationDocument =
                PresentationDocument.Open(
                    _presentationPath!,
                    true);

            PresentationPart presentationPart =
                presentationDocument.PresentationPart
                ?? throw new InvalidOperationException(
                    "Presentation part was not found.");

            P.SlideId? firstSlideId =
                presentationPart.Presentation
                    .SlideIdList?
                    .Elements<P.SlideId>()
                    .FirstOrDefault();

            if (firstSlideId == null)
            {
                throw new InvalidOperationException(
                    "The screenshot template must contain one blank slide.");
            }

            SlidePart firstSlidePart =
                (SlidePart)presentationPart.GetPartById(
                    firstSlideId.RelationshipId!);

            P.ShapeTree shapeTree =
                firstSlidePart.Slide.CommonSlideData.ShapeTree;

            shapeTree.RemoveAllChildren<P.Shape>();

            AddTextBox(
                shapeTree,
                id: GetNextShapeId(shapeTree),
                text: "Automation Execution Report",
                x: 685800,
                y: 1600000,
                width: 10800000,
                height: 900000,
                fontSize: 2800,
                bold: true,
                color: "1F4E78");

            AddTextBox(
                shapeTree,
                id: GetNextShapeId(shapeTree),
                text:
                    $"Created: {DateTime.Now:dd-MMM-yyyy HH:mm:ss}" +
                    Environment.NewLine +
                    $"Report: {Path.GetFileName(_presentationPath)}",
                x: 685800,
                y: 2800000,
                width: 10800000,
                height: 1200000,
                fontSize: 1600,
                bold: false,
                color: "404040");

            firstSlidePart.Slide.Save();
            presentationPart.Presentation.Save();
        }

        private static void AddScreenshotSlide(
            string screenshotPath,
            string title,
            string? description)
        {
            using PresentationDocument presentationDocument =
                PresentationDocument.Open(
                    _presentationPath!,
                    true);

            PresentationPart presentationPart =
                presentationDocument.PresentationPart
                ?? throw new InvalidOperationException(
                    "Presentation part was not found.");

            P.Presentation presentation =
                presentationPart.Presentation;

            P.SlideIdList slideIdList =
                presentation.SlideIdList
                ?? presentation.AppendChild(
                    new P.SlideIdList());

            SlidePart templateSlidePart =
                presentationPart.SlideParts.First();

            SlideLayoutPart slideLayoutPart =
                templateSlidePart.SlideLayoutPart
                ?? throw new InvalidOperationException(
                    "The blank template slide does not contain a slide layout.");

            SlidePart newSlidePart =
                presentationPart.AddNewPart<SlidePart>();

            newSlidePart.Slide =
                CreateBlankSlide();

            newSlidePart.AddPart(slideLayoutPart);

            AddTitleAndDescription(
                newSlidePart,
                title,
                description);

            AddImageToSlide(
                newSlidePart,
                screenshotPath);

            uint newSlideId = slideIdList
                .Elements<P.SlideId>()
                .Select(slideId => slideId.Id?.Value ?? 255U)
                .DefaultIfEmpty(255U)
                .Max() + 1U;

            string relationshipId =
                presentationPart.GetIdOfPart(newSlidePart);

            slideIdList.Append(
                new P.SlideId
                {
                    Id = newSlideId,
                    RelationshipId = relationshipId
                });

            newSlidePart.Slide.Save();
            presentation.Save();
        }

        private static P.Slide CreateBlankSlide()
        {
            P.ShapeTree shapeTree = new P.ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties
                    {
                        Id = 1U,
                        Name = string.Empty
                    },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.GroupShapeProperties(
                    new A.TransformGroup()));

            return new P.Slide(
                new P.CommonSlideData(shapeTree),
                new P.ColorMapOverride(
                    new A.MasterColorMapping()));
        }

        private static void AddTitleAndDescription(
            SlidePart slidePart,
            string title,
            string? description)
        {
            P.ShapeTree shapeTree =
                slidePart.Slide.CommonSlideData.ShapeTree;

            AddTextBox(
                shapeTree,
                id: GetNextShapeId(shapeTree),
                text: title,
                x: 350000,
                y: 120000,
                width: 11400000,
                height: 500000,
                fontSize: 1900,
                bold: true,
                color: "1F4E78");

            if (!string.IsNullOrWhiteSpace(description))
            {
                AddTextBox(
                    shapeTree,
                    id: GetNextShapeId(shapeTree),
                    text: description,
                    x: 350000,
                    y: 6100000,
                    width: 11400000,
                    height: 500000,
                    fontSize: 1000,
                    bold: false,
                    color: "595959");
            }

            AddTextBox(
                shapeTree,
                id: GetNextShapeId(shapeTree),
                text: DateTime.Now.ToString(
                    "dd-MMM-yyyy HH:mm:ss",
                    CultureInfo.InvariantCulture),
                x: 9400000,
                y: 120000,
                width: 2400000,
                height: 400000,
                fontSize: 900,
                bold: false,
                color: "808080");
        }

        private static void AddImageToSlide(
            SlidePart slidePart,
            string screenshotPath)
        {
            ImagePart imagePart =
                slidePart.AddImagePart(ImagePartType.Png);

            using FileStream imageStream =
                File.OpenRead(screenshotPath);

            imagePart.FeedData(imageStream);

            string imageRelationshipId =
                slidePart.GetIdOfPart(imagePart);

            const long boxX = 350000;
            const long boxY = 750000;
            const long boxWidth = 11400000;
            const long boxHeight = 5200000;

            long imageX = boxX;
            long imageY = boxY;
            long imageWidth = boxWidth;
            long imageHeight = boxHeight;

            using (Image image = Image.FromFile(screenshotPath))
            {
                double imageRatio =
                    (double)image.Width / image.Height;

                double boxRatio =
                    (double)boxWidth / boxHeight;

                if (imageRatio > boxRatio)
                {
                    imageWidth = boxWidth;
                    imageHeight =
                        (long)(boxWidth / imageRatio);

                    imageY =
                        boxY + ((boxHeight - imageHeight) / 2);
                }
                else
                {
                    imageHeight = boxHeight;
                    imageWidth =
                        (long)(boxHeight * imageRatio);

                    imageX =
                        boxX + ((boxWidth - imageWidth) / 2);
                }
            }

            P.ShapeTree shapeTree =
                slidePart.Slide.CommonSlideData.ShapeTree;

            uint pictureId =
                GetNextShapeId(shapeTree);

            P.Picture picture = new P.Picture(
                new P.NonVisualPictureProperties(
                    new P.NonVisualDrawingProperties
                    {
                        Id = pictureId,
                        Name = Path.GetFileName(screenshotPath)
                    },
                    new P.NonVisualPictureDrawingProperties(
                        new A.PictureLocks
                        {
                            NoChangeAspect = true
                        }),
                    new P.ApplicationNonVisualDrawingProperties()),
                new P.BlipFill(
                    new A.Blip
                    {
                        Embed = imageRelationshipId,
                        CompressionState =
                            A.BlipCompressionValues.Print
                    },
                    new A.Stretch(
                        new A.FillRectangle())),
                new P.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset
                        {
                            X = imageX,
                            Y = imageY
                        },
                        new A.Extents
                        {
                            Cx = imageWidth,
                            Cy = imageHeight
                        }),
                    new A.PresetGeometry(
                        new A.AdjustValueList())
                    {
                        Preset =
                            A.ShapeTypeValues.Rectangle
                    }));

            shapeTree.Append(picture);
        }

        private static void AddTextBox(
            P.ShapeTree shapeTree,
            uint id,
            string text,
            long x,
            long y,
            long width,
            long height,
            int fontSize,
            bool bold,
            string color)
        {
            P.Shape shape = new P.Shape();

            shape.NonVisualShapeProperties =
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties
                    {
                        Id = id,
                        Name = $"TextBox {id}"
                    },
                    new P.NonVisualShapeDrawingProperties(
                        new A.ShapeLocks
                        {
                            NoGrouping = true
                        }),
                    new P.ApplicationNonVisualDrawingProperties());

            shape.ShapeProperties =
                new P.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset
                        {
                            X = x,
                            Y = y
                        },
                        new A.Extents
                        {
                            Cx = width,
                            Cy = height
                        }),
                    new A.PresetGeometry(
                        new A.AdjustValueList())
                    {
                        Preset =
                            A.ShapeTypeValues.Rectangle
                    },
                    new A.NoFill(),
                    new A.Outline(
                        new A.NoFill()));

            A.RunProperties runProperties =
                new A.RunProperties
                {
                    Language = "en-US",
                    FontSize = fontSize,
                    Bold = bold
                };

            runProperties.Append(
                new A.SolidFill(
                    new A.RgbColorModelHex
                    {
                        Val = color
                    }));

            A.Paragraph paragraph =
                new A.Paragraph(
                    new A.Run(
                        runProperties,
                        new A.Text(text)),
                    new A.EndParagraphRunProperties
                    {
                        Language = "en-US"
                    });

            shape.TextBody =
                new P.TextBody(
                    new A.BodyProperties
                    {
                        Wrap =
                            A.TextWrappingValues.Square
                    },
                    new A.ListStyle(),
                    paragraph);

            shapeTree.Append(shape);
        }

        private static uint GetNextShapeId(
            P.ShapeTree shapeTree)
        {
            IEnumerable<uint> shapeIds =
                shapeTree
                    .Descendants<P.NonVisualDrawingProperties>()
                    .Where(item => item.Id != null)
                    .Select(item => item.Id!.Value);

            return shapeIds.Any()
                ? shapeIds.Max() + 1U
                : 2U;
        }
    }
}