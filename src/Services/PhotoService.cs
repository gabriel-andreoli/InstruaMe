using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace InstruaMe.Services
{
    public class PhotoService
    {
        private const int ThumbnailSize = 96;

        public string ResizeToThumbnail(string base64)
        {
            var data = StripDataUriPrefix(base64);
            var imageBytes = Convert.FromBase64String(data);

            using var inputStream = new MemoryStream(imageBytes);
            using var image = Image.Load(inputStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailSize, ThumbnailSize),
                Mode = ResizeMode.Crop
            }));

            using var outputStream = new MemoryStream();
            image.Save(outputStream, new JpegEncoder { Quality = 80 });

            return Convert.ToBase64String(outputStream.ToArray());
        }

        private static string StripDataUriPrefix(string base64)
        {
            var commaIndex = base64.IndexOf(',');
            return commaIndex >= 0 ? base64[(commaIndex + 1)..] : base64;
        }
    }
}
