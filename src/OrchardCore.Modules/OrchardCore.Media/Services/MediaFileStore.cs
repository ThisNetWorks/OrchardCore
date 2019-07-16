using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using OrchardCore.Abstractions.Modules.FileProviders;
using OrchardCore.FileStorage;

namespace OrchardCore.Media.Services
{
    public class MediaFileStore : IMediaFileStore, ICdnPathProvider
    {
        private readonly IFileStore _fileStore;
        private readonly string _publicUrlBase;
        private readonly string _cdnBaseUrl;

        public MediaFileStore(IFileStore fileStore, string publicUrlBase, string cdnBaseUrl = "")
        {
            _fileStore = fileStore;
            _publicUrlBase = publicUrlBase;
            _cdnBaseUrl = cdnBaseUrl;
        }

        public MediaFileStore(IFileStore fileStore)
        {
            _fileStore = fileStore;
        }


        public bool MatchCdnPath(string path)
        {
            return !String.IsNullOrEmpty(_cdnBaseUrl) && path.StartsWith(_cdnBaseUrl);
        }

        public string RemoveCdnPath(string path)
        {
            return path.Substring(_cdnBaseUrl.Length);
        }

        public Task<IFileStoreEntry> GetFileInfoAsync(string path)
        {
            return _fileStore.GetFileInfoAsync(path);
        }

        public Task<IFileStoreEntry> GetDirectoryInfoAsync(string path)
        {
            return _fileStore.GetDirectoryInfoAsync(path);
        }

        public Task<IEnumerable<IFileStoreEntry>> GetDirectoryContentAsync(string path = null, bool includeSubDirectories = false)
        {
            return _fileStore.GetDirectoryContentAsync(path, includeSubDirectories);
        }

        public Task<bool> TryCreateDirectoryAsync(string path)
        {
            return _fileStore.TryCreateDirectoryAsync(path);
        }

        public Task<bool> TryDeleteFileAsync(string path)
        {
            return _fileStore.TryDeleteFileAsync(path);
        }

        public Task<bool> TryDeleteDirectoryAsync(string path)
        {
            return _fileStore.TryDeleteDirectoryAsync(path);
        }

        public Task MoveFileAsync(string oldPath, string newPath)
        {
            return _fileStore.MoveFileAsync(oldPath, newPath);
        }

        //public Task MoveDirectoryAsync(string oldPath, string newPath)
        //{
        //    return _fileStore.MoveDirectoryAsync(oldPath, newPath);
        //}

        public Task CopyFileAsync(string srcPath, string dstPath)
        {
            return _fileStore.CopyFileAsync(srcPath, dstPath);
        }

        public Task<Stream> GetFileStreamAsync(string path)
        {
            return _fileStore.GetFileStreamAsync(path);
        }

        public Task CreateFileFromStream(string path, Stream inputStream, bool overwrite = false)
        {
            return _fileStore.CreateFileFromStream(path, inputStream, overwrite);
        }

        public string MapPathToPublicUrl(string path)
        {
            return _cdnBaseUrl + _publicUrlBase.TrimEnd('/') + "/" + this.NormalizePath(path);
        }

        public string MapPublicUrlToPath(string publicUrl)
        {
            if (publicUrl.StartsWith(_cdnBaseUrl))
            {
                var resolvedPath = publicUrl.Substring(_cdnBaseUrl.Length);
                if (resolvedPath.StartsWith(_publicUrlBase))
                {
                    return resolvedPath.Substring(_publicUrlBase.Length);
                } else
                {
                    return resolvedPath;
                }
            }
            else if (publicUrl.StartsWith(_publicUrlBase, StringComparison.OrdinalIgnoreCase))
            {
                return publicUrl.Substring(_publicUrlBase.Length);
            } else
            {
                throw new ArgumentOutOfRangeException(nameof(publicUrl), "The specified URL is not inside the URL scope of the file store.");
            }
        }
    }
}
