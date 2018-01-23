#r "System.Drawing"
#r "Microsoft.WindowsAzure.Storage"

using System;
using System.Drawing;
using System.Net;
using Microsoft.WindowsAzure.Storage.Blob;

public static string Run(StitchTileMapImageParameters parameters, CloudBlockBlob outputBlob, TraceWriter log) {
    int[] xValues = GetSequence(parameters.topLeftCorner.x, parameters.bottomRightCorner.x);
    int[] yValues = GetSequence(parameters.topLeftCorner.y, parameters.bottomRightCorner.y);

    // Compute how many tiles will be in the output image
    int tilesPerRow = xValues.Length;
    int tilesPerColumn = yValues.Length;

    // ASSUMPTION:  All tiles will be the same size.
    // Get the first tile image and use it's size to compute how big the output image will be.
    var firstImage = GetTileImage(parameters, xValues[0], yValues[0]);
    int tileWidth = firstImage.Width;
    int tileHeight = firstImage.Height;
    int outputWidth = tilesPerRow * tileWidth;
    int outputHeight = tilesPerColumn * tileHeight;

    log.Info($"Output image will be {outputWidth} x {outputHeight}");

    var outputBitmap = new Bitmap(outputWidth, outputHeight);
    using(var graphics = Graphics.FromImage(outputBitmap)) {
        int outputX = 0;
        int outputY = 0;

        // Loop through x,y coordinate pairs and download the tile at each point
        foreach (int y in yValues) {
            foreach (int x in xValues) {
                log.Info($"Downloading tile [{x}, {y}]...");
                var tileImage = GetTileImage(parameters, x, y);

                // Add the tile to our output image
                graphics.DrawImage(tileImage, outputX, outputY);

                outputX += tileWidth;
            }

            outputX = 0;
            outputY += tileHeight;

            // Brief pause so we don't overload the mapping web server
            System.Threading.Thread.Sleep(250);
        }
    }

    UploadBitmapToBlob(outputBitmap, outputBlob);

    return parameters.outputPath;
}

public static Image GetTileImage(StitchTileMapImageParameters parameters, int x, int y) {
    string tileUrl = string.Format(parameters.urlFormat, parameters.zoomLevel, x, y);

    using (var webClient = new WebClient()) {
        byte[] bytes = webClient.DownloadData(tileUrl);
        var memoryStream = new MemoryStream(bytes);
        return Image.FromStream(memoryStream);
    }
}

public static int[] GetSequence(int start, int end) {
    bool isReversed = false;

    // In some coordinate systems, the y-axis will decrease in
    // value as you move north to south instead of increasing,
    // like in a typical Cartesian coordinate system
    if (start > end) {
        isReversed = true;
        int x = end;
        end = start;
        start = x;
    }

    var output = System.Linq.Enumerable.Range(start, end - start + 1);

    if (isReversed == true) {
        output = output.Reverse();
    }

    return output.ToArray();
}

public static void UploadBitmapToBlob(Bitmap bitmap, CloudBlockBlob outputBlob) {
    var stream = new MemoryStream();
    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
    stream.Seek(0, SeekOrigin.Begin);

    // set the content type of the blob so it will be served out
    // to web browsers properly
    outputBlob.Properties.ContentType = "image/png";
    outputBlob.UploadFromStream(stream);
}

// a POCO to hold the values passed in as JSON
public class StitchTileMapImageParameters {
    public string urlFormat { get; set; }
    public int zoomLevel { get; set; }
    public Point topLeftCorner { get; set; }
    public Point bottomRightCorner { get; set; }
    public string outputPath { get; set; }

    public class Point {
        public int x { get; set; }
        public int y { get; set; }
    }
}
