using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using OrchardCore.FileStorage;

namespace OrchardCore.Media.Services
{
    public class MediaFileStore : IMediaFileStore
    {
        private readonly IFileStore _fileStore;
        private readonly IMediaFileStorePathProvider _pathProvider;

        public MediaFileStore(IFileStore fileStore, IMediaFileStorePathProvider pathProvider)
        {
            _fileStore = fileStore;
            _pathProvider = pathProvider;
            VirtualPathBase = MediaOptions.AssetsRequestPath;
        }

        public PathString VirtualPathBase { get; }
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

        public Task<Stream> GetFileStreamAsync(IFileStoreEntry fileStoreEntry)
        {
            return _fileStore.GetFileStreamAsync(fileStoreEntry);
        }

        public Task CreateFileFromStream(string path, Stream inputStream, bool overwrite = false)
        {
            return _fileStore.CreateFileFromStream(path, inputStream, overwrite);
        }

        public string MapPathToPublicUrl(string path)
        {
            return _pathProvider.MapPathToPublicUrl(path);
        }

        public string MapPublicUrlToPath(string publicUrl)
        {
            return _pathProvider.MapPublicUrlToPath(publicUrl);
        }

        public bool MatchCdnPath(string path)
        {
            return _pathProvider.MatchCdnPath(path);
        }

        public string RemoveCdnPath(string path)
        {
            return _pathProvider.RemoveCdnPath(path);
        }
    }
}
