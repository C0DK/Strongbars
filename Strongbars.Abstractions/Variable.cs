namespace Strongbars.Abstractions;

public class Variable : IEquatable<Variable?>
{
    public String Name { get; }
    public VariableType Type { get; }

    public Variable(string name, VariableType type)
    {
        Name = name;
        Type = type;
    }

    public bool Equals(Variable? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return Name == other.Name && Type.Equals(other.Type);
    }

    public override bool Equals(object? obj) => Equals(obj as Variable);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + Name.GetHashCode();
            hash = hash * 23 + Type.GetHashCode();
            return hash;
        }
    }

    public override string ToString() => $"[{Name} ({Type})]";

    public static bool operator ==(Variable? left, Variable? right) => Equals(left, right);

    public static bool operator !=(Variable? left, Variable? right) => !Equals(left, right);
}
