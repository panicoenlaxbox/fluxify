using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;

namespace Fluxify.Playground;

public class Document
{
    [VectorStoreKey(StorageName = "id")]
    [TextSearchResultLink]
    public ulong Id { get; init; }

    [VectorStoreData(StorageName = "name")]
    [TextSearchResultName]
    public string Name { get; init; } = null!;

    [VectorStoreData(StorageName = "content")]
    [TextSearchResultValue]
    public string Content { get; init; } = null!;

    [VectorStoreData(StorageName = "path")]
    public string Path { get; init; } = null!;

    [VectorStoreData(StorageName = "title")]
    public string? Title { get; init; }

    [VectorStoreData(StorageName = "description")]
    public string? Description { get; init; }

    [VectorStoreData(StorageName = "categories")]
    public List<string>? Categories { get; init; }

    [VectorStoreData(StorageName = "tags")]
    public List<string>? Tags { get; init; }

    [VectorStoreData(StorageName = "app")]
    public string? App { get; init; }

    [VectorStoreData(StorageName = "user_guide")]
    public List<string>? UserGuide { get; init; }

    [VectorStoreData(StorageName = "related_content")]
    public List<string>? RelatedContent { get; init; }

    [VectorStoreData(StorageName = "tokens")]
    public int Tokens { get; init; }

    [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw)]
    public ReadOnlyMemory<float> ContentEmbedding { get; init; }
}