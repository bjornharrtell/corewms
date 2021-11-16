namespace CoreWms;

public readonly struct Option<T>
{
    public readonly T Value { get; }

    public readonly bool IsSome { get; }
    public readonly bool IsNone => !IsSome;

    public Option(T value) => (Value, IsSome) = (value, true);

    public void Deconstruct(out T value) => (value) = (Value);
}
