using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.FileStorage.AzureBlob;
using SixLabors.ImageSharp.Web;
using SixLabors.ImageSharp.Web.Resolvers;

namespace OrchardCore.Media.Azure.Processing
{
    public class MediaBlobFileResolverFactory : IMediaFileResolverFactory
    {
        private readonly IMediaFileStore _mediaStore;

        public MediaBlobFileResolverFactory(IMediaFileStore mediaStore)
        {
            _mediaStore = mediaStore;
        }
        public async Task<IImageResolver> GetAsync(HttpContext context)
        {
            //TODO consider this. It causes a HEAD request every time, checking if the files exists. Cache? Hmm BlobFile is CQRS so possible.
            // Or the suggestion from Sebastien is a middleware cache in front of imagesharp. also sounds better

            // Path has already been correctly parsed before here.
            var filePath = _mediaStore.MapRequestPathToFileStorePath(context.Request.PathBase + context.Request.Path.Value);

            // Check to see if the file exists.
            var file = await _mediaStore.GetFileInfoAsync(filePath);

            var blobFile = file as BlobFile;
            if (blobFile == null || blobFile.BlobReference == null)
            {
                return null;
            }

            var metadata = new ImageMetaData(file.LastModifiedUtc);
            return new MediaBlobFileResolver(_mediaStore, blobFile, metadata);
        }
    }
}
