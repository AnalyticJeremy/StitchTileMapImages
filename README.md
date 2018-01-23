# StitchTileMapImages


This is an Azure function (written in C#) that will download a specified section of a Leaflet map and stitch the individual
tiles together to make a single image.  The function takes its input from an Azure queue and notifies a different queue when
it finishes.  This allows the function to be part of a longer map processing pipeline.

## Installation
In the Azure portal, create a new Azure Function App.  Add a new function called "StitchTileMapImages".  Paste the code from the
"run.csx" and "function.json" files in this repository into the corresponding files in the Azure function.

You will need to customize the settings in the "function.json" file to match your environment.  The first binding is the input queue.
Make sure that the "queueName" setting matches the name of your queue.

The second binding is for the Azure blob container where the output image will be stored. The "path" must begin with the blob
container name.  Using the `{outputPath}` binding expression allows the name of the output file to be specified in the input parameters.

The third binding is the name of the queue which should be notified when the function finishes.  A message containing the name of
the output file will be written to this queue.  If you want to perform other tasks on the image file after it is created, you can
write another function that monitors this second queue.

## Executing the Function
To invoke this Azure Function, just put a message on the input queue.  The message should contain all of the input parameters in JSON format.  Here is an example of the input that will pull a map of Charlotte, North Carolina, from openstreetmap.org.

```json
{
  "urlFormat": "http://c.tile.openstreetmap.org/{0}/{1}/{2}.png",
  "zoomLevel": 14,
  "topLeftCorner": {
    "x": 4511,
    "y": 6476
  },
  "bottomRightCorner": {
    "x": 4514,
    "y": 6478
  },
  "outputPath": "images/working/charlotte.png"
}
```

The "urlFormat" parameter contains a standard .NET formatting string that tells the function how each tile URL should be structured.
The `{0}` token will be replaced with the zoom level.  The `{1}` token will be replaced with the X coordinate, and the `{2}` token
will be replaced with the Y coordinate.

