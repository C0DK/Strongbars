namespace Strongbars.Abstractions;

public class Variable : IEquatable<Variable?>
{
    public String Name { get; }
    public VariableType Type { get; }
    public bool Optional { get; }
    public bool Array { get; }

    public Variable(string name, VariableType type, bool array, bool optional)
    {
        Name = name;
        Type = type;
        Array = array;
        Optional = optional;
    }

    public Variable AsType(VariableType t) =>
        new(name: Name, type: t, optional: Optional, array: Array);

    public bool Equals(Variable? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name
            && Type.Equals(other.Type)
            && Optional == other.Optional
            && Array == other.Array;
    }

    public override bool Equals(object? obj) => Equals(obj as Variable);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Name.GetHashCode();
            hash = hash * 113 + Type.GetHashCode();
            hash = hash * 569 + Optional.GetHashCode();
            hash = hash * 701 + Array.GetHashCode();
            return hash;
        }
    }

    public override string ToString() =>
        $"[{Name} ({Type}{(Array ? "[]" : "")}{(Optional ? "?" : "")})]";

    public static bool operator ==(Variable? left, Variable? right) => Equals(left, right);

    public static bool operator !=(Variable? left, Variable? right) => !Equals(left, right);
}
