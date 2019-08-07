using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using OrchardCore.FileStorage;

namespace OrchardCore.Media.Azure.Middleware
{
    // Adapted under the apache 2.0 license from AspNetCore.StaticFileMiddleware.

    /// <summary>
    /// Base class to provide shared code between contexts.
    /// </summary>
    public abstract class BaseFileContext
    {
        protected readonly HttpContext _context;
        protected readonly HttpRequest _request;
        protected readonly HttpResponse _response;
        protected readonly ILogger _logger;
        protected readonly string _method;
        protected readonly string _contentType;
        protected readonly TimeSpan _maxBrowserCacheDays;

        protected EntityTagHeaderValue _etag;
        protected RequestHeaders _requestHeaders;
        protected ResponseHeaders _responseHeaders;

        protected long _length;
        protected DateTimeOffset _lastModified;

        protected PreconditionState _ifMatchState;
        protected PreconditionState _ifNoneMatchState;
        protected PreconditionState _ifModifiedSinceState;
        protected PreconditionState _ifUnmodifiedSinceState;

        protected readonly bool _isHeadRequest;

        public BaseFileContext(
            HttpContext context,
            ILogger logger,
            int maxBrowserCacheDays,
            string contentType
            )
        {
            _context = context;
            _request = context.Request;
            _response = context.Response;
            _logger = logger;
            _method = _request.Method;
            _maxBrowserCacheDays = TimeSpan.FromDays(maxBrowserCacheDays);
            _contentType = contentType;
            _etag = null;
            _requestHeaders = _request.GetTypedHeaders();
            _responseHeaders = _response.GetTypedHeaders();

            _length = 0;
            _lastModified = new DateTimeOffset();
            _ifMatchState = PreconditionState.Unspecified;
            _ifNoneMatchState = PreconditionState.Unspecified;
            _ifModifiedSinceState = PreconditionState.Unspecified;
            _ifUnmodifiedSinceState = PreconditionState.Unspecified;
            if (HttpMethods.IsHead(_method))
            {
                _isHeadRequest = true;
            }
        }

        public void ComprehendRequestHeaders()
        {
            ComputeIfMatch();

            ComputeIfModifiedSince();
        }

        private void ComputeIfMatch()
        {
            // 14.24 If-Match
            var ifMatch = _requestHeaders.IfMatch;
            if (ifMatch != null && ifMatch.Any())
            {
                _ifMatchState = PreconditionState.PreconditionFailed;
                foreach (var etag in ifMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(_etag, useStrongComparison: true))
                    {
                        _ifMatchState = PreconditionState.ShouldProcess;
                        break;
                    }
                }
            }

            // 14.26 If-None-Match
            var ifNoneMatch = _requestHeaders.IfNoneMatch;
            if (ifNoneMatch != null && ifNoneMatch.Any())
            {
                _ifNoneMatchState = PreconditionState.ShouldProcess;
                foreach (var etag in ifNoneMatch)
                {
                    if (etag.Equals(EntityTagHeaderValue.Any) || etag.Compare(_etag, useStrongComparison: true))
                    {
                        _ifNoneMatchState = PreconditionState.NotModified;
                        break;
                    }
                }
            }
        }

        private void ComputeIfModifiedSince()
        {
            var now = DateTimeOffset.UtcNow;

            // 14.25 If-Modified-Since
            var ifModifiedSince = _requestHeaders.IfModifiedSince;
            if (ifModifiedSince.HasValue && ifModifiedSince <= now)
            {
                bool modified = ifModifiedSince < _lastModified;
                _ifModifiedSinceState = modified ? PreconditionState.ShouldProcess : PreconditionState.NotModified;
            }

            // 14.28 If-Unmodified-Since
            var ifUnmodifiedSince = _requestHeaders.IfUnmodifiedSince;
            if (ifUnmodifiedSince.HasValue && ifUnmodifiedSince <= now)
            {
                bool unmodified = ifUnmodifiedSince >= _lastModified;
                _ifUnmodifiedSinceState = unmodified ? PreconditionState.ShouldProcess : PreconditionState.PreconditionFailed;
            }
        }

        public void ApplyResponseHeaders(int statusCode)
        {
            _response.StatusCode = statusCode;
            if (statusCode < 400)
            {
                // these headers are returned for 200, 206, and 304
                // they are not returned for 412 and 416
                if (!string.IsNullOrEmpty(_contentType))
                {
                    _response.ContentType = _contentType;
                }

                _responseHeaders.LastModified = _lastModified;
                _responseHeaders.ETag = _etag;
                _responseHeaders.Headers[HeaderNames.AcceptRanges] = "bytes";

                // Apply the same cache control headers as ImageSharp.Web.
                _responseHeaders.CacheControl = new CacheControlHeaderValue
                {
                    Public = true,
                    MaxAge = _maxBrowserCacheDays,
                    MustRevalidate = true
                };
            }
            if (statusCode == StatusCodes.Status200OK)
            {
                // this header is only returned here for 200
                // it already set to the returned range for 206
                // it is not returned for 304, 412, and 416
                _response.ContentLength = _length;
            }
        }

        public PreconditionState GetPreconditionState()
            => GetMaxPreconditionState(_ifMatchState, _ifNoneMatchState, _ifModifiedSinceState, _ifUnmodifiedSinceState);

        private static PreconditionState GetMaxPreconditionState(params PreconditionState[] states)
        {
            var max = PreconditionState.Unspecified;
            for (var i = 0; i < states.Length; i++)
            {
                if (states[i] > max)
                {
                    max = states[i];
                }
            }
            return max;
        }

        public Task SendStatusAsync(int statusCode)
        {
            ApplyResponseHeaders(statusCode);

            //_logger.Handled(statusCode, SubPath);
            return Task.CompletedTask;
        }

        public async Task ServeFile(HttpContext context, RequestDelegate next)
        {
            ComprehendRequestHeaders();
            switch (GetPreconditionState())
            {
                case PreconditionState.Unspecified:
                case PreconditionState.ShouldProcess:
                    if (_isHeadRequest)
                    {
                        await SendStatusAsync(StatusCodes.Status200OK);
                        return;
                    }

                    try
                    {
                        await SendAsync();
                        //_logger.FileServed(SubPath, PhysicalPath);
                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        context.Response.Clear();
                    }

                    catch (FileStoreException)
                    {
                        context.Response.Clear();
                    }
                    await next(context);
                    return;
                case PreconditionState.NotModified:
                    //_logger.FileNotModified(SubPath);
                    await SendStatusAsync(StatusCodes.Status304NotModified);
                    return;
                case PreconditionState.PreconditionFailed:
                    //_logger.PreconditionFailed(SubPath);
                    await SendStatusAsync(StatusCodes.Status412PreconditionFailed);
                    return;
                default:
                    var exception = new NotImplementedException(GetPreconditionState().ToString());
                    Debug.Fail(exception.ToString());
                    throw exception;
            }
        }

        public abstract Task SendAsync();

        public enum PreconditionState : byte
        {
            Unspecified,
            NotModified,
            ShouldProcess,
            PreconditionFailed
        }
    }
}
