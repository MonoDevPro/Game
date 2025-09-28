using System.Collections.ObjectModel;

namespace GameWeb.Domain.Common;

public abstract class DomainException : Exception
{
    public string Code { get; }
    public DateTimeOffset OccurredOn { get; }
    public IReadOnlyDictionary<string, object>? Metadata { get; }

    protected DomainException(string message, string code = "domain.error", IDictionary<string, object>? metadata = null)
        : base(message)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        OccurredOn = DateTimeOffset.UtcNow;
        Metadata = metadata is null ? null : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(metadata));
    }

    protected DomainException(string message, Exception innerException, string code = "domain.error", IDictionary<string, object>? metadata = null)
        : base(message, innerException)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        OccurredOn = DateTimeOffset.UtcNow;
        Metadata = metadata is null ? null : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(metadata));
    }

    public override string ToString()
    {
        var meta = Metadata is null ? string.Empty : $" | metadata: {string.Join(", ", Metadata.Select(kv => $"{kv.Key}={kv.Value}"))}";
        return $"{GetType().FullName}: {Message} | code: {Code} | occurredOn: {OccurredOn:o}{meta}";
    }
}
