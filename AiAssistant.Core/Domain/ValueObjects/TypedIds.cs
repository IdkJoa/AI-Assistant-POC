namespace AiAssistant.Domain.Domain.ValueObjects;

public readonly record struct DocumentId(Guid Value) {
    public static DocumentId New() => new(Guid.CreateVersion7());
    public static DocumentId From(Guid value) => new(value);
    public static DocumentId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}

public readonly record struct ChunkId(Guid Value)
{
    public static ChunkId New() => new(Guid.CreateVersion7());
    public static ChunkId From(Guid value) => new(value);
    public static ChunkId From(string value) => new(Guid.Parse(value));
    public override string ToString() => Value.ToString();
}