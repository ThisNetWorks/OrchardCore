using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace OrchardCore.Media.Azure.Services
{
    public class MediaBlobMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMediaFileStore _mediaFileStore;
        private readonly PathString _assetsRequestPath;

        public MediaBlobMiddleware(
            RequestDelegate next,
            IMediaFileStore mediaFileStore,
            IOptions<MediaOptions> mediaOptions
            )
        {
            _next = next;
            _mediaFileStore = mediaFileStore;
            _assetsRequestPath = mediaOptions.Value.AssetsRequestPath;
            
        }

        //TODO very early experiment, but in a working state
        // might look at sharing ImageSharp cache for this, as the whole idea falls over without a cdn in front of it
        // which seems like something we shouldn't force people into, but encourage them to
        // needs a lot of validation, dealing with bad requests, etc
        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(_assetsRequestPath))
            {
                await _next.Invoke(context);
                return;
            }
            // TODO this needs supported files/extensions for validation
            var mappedPath = _mediaFileStore.MapPublicUrlToPath(context.Request.PathBase + context.Request.Path);
  
            var fileInfo = await _mediaFileStore.GetFileInfoAsync(mappedPath);
            if (fileInfo == null)
            {
                await _next.Invoke(context);
                return;
            }

            var fileStream = await _mediaFileStore.GetFileStreamAsync(fileInfo);
            if (fileStream.CanSeek)
            {
                fileStream.Position = 0;
            }

            await fileStream.CopyToAsync(context.Response.Body);
            if (context.Response.Body.CanSeek)
            {
                context.Response.Body.Position = 0;
            }
            context.Response.StatusCode = 200;

        }
    }
}

