using System.Threading.Tasks;
using OrchardCore.FileStorage;

namespace OrchardCore.Media
{
    /// <summary>
    /// Cache a media file store.
    /// </summary>
    public interface IMediaFileStoreCache : IFileStoreCache
    {
        dynamic GetDisplayModel();
    }
}
