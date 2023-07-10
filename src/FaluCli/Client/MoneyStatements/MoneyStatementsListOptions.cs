using Falu.Core;

namespace Falu.Client.MoneyStatements;

/// <summary>Options for filtering and pagination of money statements.</summary>
public record MoneyStatementsListOptions : BasicListOptions
{
    /// <summary>Filter options for <see cref="MoneyStatement.Provider"/> property.</summary>
    public List<string>? Provider { get; set; }

    /// <summary>Filter options for <see cref="MoneyStatement.ObjectsKind"/> property.</summary>
    public List<string>? ObjectsKind { get; set; }

    /// <summary>Filter options for <see cref="MoneyStatement.Uploaded"/> property.</summary>
    public bool? Uploaded { get; set; }

    /// <inheritdoc/>
    protected override void Populate(QueryValues values)
    {
        base.Populate(values);
        values.Add("provider", Provider);
        values.Add("objects_kind", ObjectsKind);
        values.Add("uploaded", Uploaded);
    }
}
