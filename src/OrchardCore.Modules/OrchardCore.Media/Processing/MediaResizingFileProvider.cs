using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.Helpers;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.Providers;
using SixLabors.ImageSharp.Web.Resolvers;

namespace OrchardCore.Media.Processing
{
    public class MediaResizingFileProvider : IImageProvider
    {
        private readonly IMediaFileResolverFactory _mediafileResolveFactory;
        private readonly FormatUtilities _formatUtilities;
        private readonly int[] _supportedSizes;
        private readonly PathString _assetsRequestPath;

        public MediaResizingFileProvider(
            IMediaFileResolverFactory mediaFileResolverFactory,
            IOptions<ImageSharpMiddlewareOptions> imageSharpOptions,
            IOptions<MediaOptions> mediaOptions
            )
        {
            _mediafileResolveFactory = mediaFileResolverFactory;
            _formatUtilities = new FormatUtilities(imageSharpOptions.Value.Configuration);
            _supportedSizes = mediaOptions.Value.SupportedSizes;
            _assetsRequestPath = mediaOptions.Value.AssetsRequestPath;
        }

        /// <inheritdoc/>
        public Func<HttpContext, bool> Match { get; set; } = _ => true;

        /// <inheritdoc/>
        public IDictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        /// <inheritdoc/>
        public bool IsValidRequest(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments(_assetsRequestPath))
            {
                return false;
            }

            if (_formatUtilities.GetExtensionFromUri(context.Request.GetDisplayUrl()) == null)
            {
                return false;
            }

            if (!context.Request.Query.ContainsKey(ResizeWebProcessor.Width) &&
                !context.Request.Query.ContainsKey(ResizeWebProcessor.Height))
            {
                return false;
            }

            if (context.Request.Query.TryGetValue(ResizeWebProcessor.Width, out var widthString))
            {
                var width = CommandParser.Instance.ParseValue<int>(widthString);

                if (Array.BinarySearch<int>(_supportedSizes, width) < 0)
                {
                    return false;
                }
            }

            if (context.Request.Query.TryGetValue(ResizeWebProcessor.Height, out var heightString))
            {
                var height = CommandParser.Instance.ParseValue<int>(heightString);

                if (Array.BinarySearch<int>(_supportedSizes, height) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public async Task<IImageResolver> GetAsync(HttpContext context)
        {
            return await _mediafileResolveFactory.GetAsync(context);
        }
    }
}
