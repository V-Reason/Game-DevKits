using System;
using System.Threading;
using System.Threading.Tasks;
using GameDevKit.AssetLifecycle;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class ResourcesAssetSource : IAssetSource<string, Object>
{
    public Task<AssetLoadResult<Object>> LoadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource<AssetLoadResult<Object>>();
        ResourceRequest request = Resources.LoadAsync(key);

        request.completed += _ =>
        {
            if (cancellationToken.IsCancellationRequested)
            {
                completion.TrySetCanceled(cancellationToken);
                return;
            }

            Object asset = request.asset;
            completion.TrySetResult(asset != null
                ? AssetLoadResult<Object>.Success(asset)
                : AssetLoadResult<Object>.Missing($"Resources asset not found: {key}"));
        };

        return completion.Task;
    }
}
