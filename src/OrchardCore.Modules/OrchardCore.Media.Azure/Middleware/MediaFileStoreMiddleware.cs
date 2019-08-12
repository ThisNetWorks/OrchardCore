using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp.Web.Caching;
using SixLabors.ImageSharp.Web.Commands;
using SixLabors.ImageSharp.Web.Helpers;
using SixLabors.ImageSharp.Web.Middleware;
using SixLabors.ImageSharp.Web.Processors;
using SixLabors.ImageSharp.Web.Providers;

namespace OrchardCore.Media.Azure.Middleware
{
    /// <summary>
    /// Enables serving media assets from Blob Store, or local file system cache.
    /// </summary>
    public class MediaFileStoreMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IMediaFileStore _mediaFileStore;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly IMediaImageCache _mediaImageCache;
        private readonly ICacheHash _cacheHash;
        private readonly IRequestParser _requestParser;
        private readonly IEnumerable<IImageProvider> _imageProviders;
        private readonly FormatUtilities _formatUtilities;

        private readonly PathString _assetsRequestPath;
        private readonly string[] _allowedFileExtensions;

        private readonly ImageSharpMiddlewareOptions _isOptions;

        /// <summary>
        /// The collection of known commands gathered from the processors.
        /// </summary>
        private readonly List<string> _isKnownCommands = new List<string>();

        public MediaFileStoreMiddleware(
            RequestDelegate next,
            IMediaFileStore mediaFileStore,
            IContentTypeProvider contentTypeProvider,
            IMediaImageCache mediaImageCache,
            ICacheHash cacheHash,
            IRequestParser requestParser,
            IEnumerable<IImageProvider> imageProviders,
            IOptions<ImageSharpMiddlewareOptions> isOptions,
            IEnumerable<IImageWebProcessor> processors,
            IOptions<MediaOptions> mediaOptions,
            ILogger<MediaFileStoreMiddleware> logger
            )
        {
            _next = next;
            _mediaFileStore = mediaFileStore;
            _contentTypeProvider = contentTypeProvider;
            _mediaImageCache = mediaImageCache;
            _cacheHash = cacheHash;
            _requestParser = requestParser;
            _imageProviders = imageProviders;
            _isOptions = isOptions.Value;

            foreach (var processor in processors)
            {
                _isKnownCommands.AddRange(processor.Commands);
            }

            _formatUtilities = new FormatUtilities(_isOptions.Configuration);

            _assetsRequestPath = mediaOptions.Value.AssetsRequestPath;
            _allowedFileExtensions = mediaOptions.Value.AllowedFileExtensions;

            Logger = logger;
        }

        public ILogger Logger { get; }

        /// <summary>
        /// Processes a request to determine if it matches a known file, and if so, serves it.
        /// </summary>
        /// <param name="context"></param>
        public async Task Invoke(HttpContext context)
        {
            //TODO for 3.0 and endpoint routing, validate it is not an endpoint

            if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            {
                await _next(context);
                return;
            }

            var validatePath = context.Request.Path.StartsWithSegments(_assetsRequestPath, StringComparison.OrdinalIgnoreCase, out var subPath);
            if (!validatePath)
            {
                Logger.LogDebug("Request path {Path} does not match the assets request path {RequestPath}", subPath, _assetsRequestPath);
                await _next(context);
                return;
            }

            // This will return a 404 if the file extension is not supported.
            var fileExtension = GetExtension(subPath);
            if (!_allowedFileExtensions.Contains(fileExtension, StringComparer.OrdinalIgnoreCase))
            {
                Logger.LogDebug("File extension not supported for request path {Path}", subPath);
                await _next(context);
                return;
            }

            var cacheKey = GetCacheKey(context);

            // Serve unknown file types, allowed file extensions is the test to see if we should serve this content.
            _contentTypeProvider.TryGetContentType(subPath.Value, out var contentType);

            // Create context that will try to serve file, if it exists in cache.
            var mediaProviderFileContext = new MediaFileCacheContext(context, Logger,
                _mediaImageCache, _formatUtilities, _isOptions.MaxBrowserCacheDays, contentType, cacheKey, fileExtension, subPath);

            if (await mediaProviderFileContext.LookupFileInfo())
            {
                // If file exists in cache try to serve it.
                Logger.LogDebug("Request path {Path} found in cache with key {Cachekey}", subPath, cacheKey);
                await mediaProviderFileContext.ServeFile(context, _next);
                return;
            }

            // File was not in cache, test if ImageSharp should handle this request.
            var provider = _imageProviders.FirstOrDefault(r => r.Match(context));
            if (provider?.IsValidRequest(context) == true)
            {
                // Pass to ImageSharp middleware.
                Logger.LogDebug("Request path {Path} not found in image cache with key {CacheKey}, is valid for resizing, passing to ImageSharp middleware", subPath, cacheKey);
                await _next(context);
                return;
            }

            var mediaFileStoreContext = new MediaFileStoreContext(context, Logger, _mediaFileStore, _mediaImageCache,
                _isOptions.MaxBrowserCacheDays, contentType, cacheKey, fileExtension, subPath);

            if (await mediaFileStoreContext.LookupFileStoreInfo())
            {
                // If file exists in file store try to serve it.
                Logger.LogDebug("Request path {Path} found in file store, serving and caching with key {CacheKey}", subPath, cacheKey);
                await mediaFileStoreContext.ServeFile(context, _next);
                return;
            }

            // If we get here, the file should not be served.
            await _next(context);
            return;
        }

        // Generate the same cache key as ImageSharp.
        private string GetCacheKey(HttpContext context)
        {
            var commands = _requestParser.ParseRequestCommands(context)
                .Where(kvp => _isKnownCommands.Contains(kvp.Key))
                .ToDictionary(p => p.Key, p => p.Value);

            _isOptions.OnParseCommands?.Invoke(new ImageCommandContext(context, commands, CommandParser.Instance));

            var uri = GetUri(context, commands);
            var key = _cacheHash.Create(uri, _isOptions.CachedNameLength);
            return key;
        }

        // Generate the same Uri as ImageSharp, noting that only valid commands are included in the Uri.
        private static string GetUri(HttpContext context, IDictionary<string, string> commands)
        {
            var sb = new StringBuilder(context.Request.Host.ToString());

            string pathBase = context.Request.PathBase.ToString();
            if (!string.IsNullOrWhiteSpace(pathBase))
            {
                sb.AppendFormat("{0}/", pathBase);
            }

            string path = context.Request.Path.ToString();
            if (!string.IsNullOrWhiteSpace(path))
            {
                sb.Append(path);
            }

            sb.Append(QueryString.Create(commands));

            return sb.ToString().ToLowerInvariant();
        }

        private static string GetExtension(string path)
        {
            // Don't use Path.GetExtension as that may throw an exception if there are
            // invalid characters in the path. Invalid characters should be handled
            // by the FileProviders

            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var index = path.LastIndexOf('.');
            if (index < 0)
            {
                return null;
            }

            return path.Substring(index);
        }
    }
}
