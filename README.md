# Envy

A utility library for loading environment variables into strongly-typed C# objects.

Inspired by [the Rust crate of the same name](https://github.com/softprops/envy).

### Get it on [NuGet](https://www.nuget.org/packages/Envy/).

### Example

```csharp
// typically these would already be set in your environment
// but for the sake of this example, we'll set them here
// we'll set them all with the DB_ prefix
Env.Vars["DB_HOST"] = "example.com";
Env.Vars["DB_USER"] = "admin";
Env.Vars["DB_PASS"] = "hunter2";

// the underscore separator is inserted automatically.
// prefixes are optional.
var connInfo = Env.ToModel<DatabaseConnectionInfo>( "DB" );
// connInfo now looks like this:
// ╭──────────┬───────────────╮
// │ Name     │ Value         │
// ├──────────┼───────────────┤
// │ Host     │ "example.com" │
// │ Port     │ 27017         │
// │ User     │ "admin"       │
// │ Pass     │ "hunter2"     │
// │ Database │ null          │
// ╰──────────┴───────────────╯

// connect to MongoDB

public sealed record DatabaseConnectionInfo
{
    public required string  Host     { get; init; }
    public          ushort  Port     { get; init; } = 27017;
    public          string? User     { get; init; }
    public          string? Pass     { get; init; }
    public          string? Database { get; init; }
}


```