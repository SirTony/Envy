# Envy

A utility library for loading environment variables into strongly-typed C# objects.

Inspired by [the Rust crate of the same name](https://github.com/softprops/envy).

### Get it on [NuGet](https://www.nuget.org/packages/Envy/).

### Example

```csharp
using Envy;

// create a special Env instance with the prefix "DB"
// now all variable accesses will be prefixed with "DB_" automatically
var env = Env.Vars.WithPrefix( "DB" );

// set some environment variables
// typically these would already be set in your environment
// but for the sake of this example, we'll set them here
// because of the prefix on env, the variables will be set as DB_HOST, DB_USER, and DB_PASS
env["HOST"] = "example.com";
env["USER"] = "admin";

// Env will automatically convert the case of the variable names to upper case
// so on our prefixed env, the variable name passed here is 'Pass' but it will internally resolve to 'DB_PASS'
env[nameof( DatabaseConnectionInfo.Pass )] = "hunter2";

// the underscore separator (or whatever separator you specify on Env.PrefixSeparator) is inserted automatically.
// DB is our prefix that goes before the variable name, separated by an Env.PrefixSeparator
// the prefix argument is optional and defaults to null for no prefix.
var connInfo = Env.Bind<DatabaseConnectionInfo>( "DB" );
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
    public required   string  Host     { get; init; }
    [Optional] public ushort  Port     { get; init; } = 27017;
    public            string? User     { get; init; }
    public            string? Pass     { get; init; }
    public            string? Database { get; init; }
}
```