# FromToken

A lightweight ASP.NET Core library that binds action method parameters from the claims of the current user principal,
using a custom model binding source.

---

## Overview

`FromToken` provides the `[FromToken]` attribute and the supporting infrastructure needed to inject values carried
inside an authentication/authorization token directly into controller action parameters — without manually reading
`HttpContext.User` in every action.

Typical use cases:

- Injecting a user identifier (`userId`, `sub`, `nameidentifier`) into an action.
- Extracting a tenant, organization, or scope identifier from a claim.
- Passing strongly typed values deserialized from a JSON claim.

The library also integrates with Swashbuckle so that token-bound parameters are **not** advertised in the generated
OpenAPI document — they are resolved server-side and are not part of the client contract.

---

## Requirements

- ASP.NET Core on **.NET 10.0**
- `Microsoft.AspNetCore.App` framework reference (available transitively in ASP.NET Core projects)
- `Swashbuckle.AspNetCore.SwaggerGen` (already referenced by the package)

> The package relies on `Microsoft.AspNetCore.App` and therefore **cannot** be consumed by .NET Framework projects.

---

## Installation

```bash
dotnet add package FromToken
```

## Quick Start

### 1. Register the feature

```csharp
// Program.cs
builder.Services.AddControllers();

builder.Services.AddFromTokenAttribute(options =>
{
    // Optional: extend the default configuration.
    options.AddClaimAlias("tenantId", "tid", "tenant_id");
    options.AddJsonParser<TenantInfo>();
});
```

`AddFromTokenAttribute` performs the following registrations:

Registers `TokenUserOptions` via the options pattern.

Adds the built-in parsers for `string`, `Guid`, `long`, and `int`.

Adds default claim aliases for the user identifier: `userId`, `sub`, and the WS-Federation `nameidentifier` claim URI.

Inserts the model binder provider at the beginning of `MvcOptions.ModelBinderProviders`.

Registers the Swashbuckle operation filter that hides `[FromToken]` parameters from OpenAPI.

### 2. Decorate action parameters

```csharp
using FromToken;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetOrder([FromToken] string tenantId, int id)
    {
        return Ok(new { tenantId, id });
    }
}
```

The `tenantId` parameter is resolved from the user's claims — it is not read from the query string, route, body, form,
or headers.

## How It Works

For a parameter decorated with `[FromToken]`, the binding flow is:

1. **Attribute** - `FromTokenAttribute` implements `IBindingSourceMetadata` and returns BindingSource.Custom. MVC
   therefore skips standard binding sources.

2. **Provider** - `UserIdModelBinderProvider` (registered at position 0) detects `BindingSource.Custom` and returns a
   binder instance.

3. **Binder** - `UserIdModelBinder` resolves the value:

    - Selects a parser by `ModelType`. If none is registered, falls back to `JsonSerializer.Deserialize`.

    - Looks up the parameter name in `TokenUserOptions.ClaimAliases`. If no alias exists, the parameter name itself is
      used as the claim type.

    - Iterates over the claim types in order; the first claim whose value parses successfully wins.

    - Returns `ModelBindingResult.Failed()` if no claim is found or none parse.

4. **Swagger filter** - `FromTokenOperationFilter` removes `[FromToken]` parameters from the OpenAPI operation, so they
   are not documented as client-supplied inputs.

## Configuration

Configuration is expressed through `TokenUserOptions`, passed to `AddFromTokenAttribute`.

## Value Parsers

A parser converts a raw claim value into the parameter's type. Register one per type:

```csharp
builder.Services.AddFromTokenAttribute(options =>
{
    options.AddParser(value =>
        DateTimeOffset.TryParse(value, out var dto) ? dto : null);
});
```

Built-in parsers:

| Type   | 	Behavior on failure   |
|--------|------------------------|
| string | 	Returns the raw value |
| Guid   | 	Returns `Guid.Empty`  |
| long   | 	Returns `0`           |
| int    | 	Returns `0`           |

Registering a parser for a type that already has one replaces the existing parser. User-supplied configuration is
applied after the defaults, so it can override them.

## JSON Parsers

For claim values that contain JSON:

```csharp
builder.Services.AddFromTokenAttribute(options =>
{
    options.AddJsonParser<TenantInfo>();
});
```

Deserialization uses `JsonSerializerOptions.PropertyNameCaseInsensitive = true`.

## Claim Aliases

Map a parameter name to one or more claim types (in priority order):

```csharp
options.AddClaimAlias(
    "userId",
    "sub",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
```

- Parameter names are matched case-insensitively.
- If an alias for a parameter already exists, it is replaced.
- When no alias exists, the parameter name itself is used as the claim type.

Example:

```csharp
[HttpGet]
public IActionResult GetProfile([FromToken] string userId) { /* ... */ }
```

With the default alias, `userId` is resolved from the `sub` claim first, then from the WS-Federation `nameidentifier`
claim.

## OpenAPI Integration

`FromTokenOperationFilter` is registered automatically by `AddFromTokenAttribute`. It removes `[FromToken]` parameters
from the generated OpenAPI operation, because they are resolved server-side from the token and are not supplied by the
client.

No additional configuration is required when using `AddSwaggerGen`:

```csharp
builder.Services.AddSwaggerGen();
```

## Limitations and Notes

**`BindingSource.Custom` is shared.** The model binder provider handles any parameter whose binding source is
`BindingSource.Custom`, not only `[FromToken]`-decorated ones. If another library in the application also uses
`BindingSource.Custom`, its parameters will be routed to this binder. Avoid combining multiple custom binding sources in
the same application.

**Parser failure semantics differ.** Built-in parsers return `Guid.Empty` / `0` on failure, while user-supplied parsers
typically return `null`. Both are treated as "parse succeeded" by the binder. If `null` is returned,
`ModelBindingResult.Success(null)` is produced, which may surface as `null` in a non-nullable parameter — check for this
in actions if strictness is required.

**Unregistered types fall back to JSON.** For types without a registered parser, the binder uses
`JsonSerializer.Deserialize`. Raw claim strings for primitives (e.g. `int`, `Guid`) are not valid JSON without quotes
and will fail; register a parser for such types.

**Exceptions from parsers are swallowed.** All exceptions thrown by a parser during binding are caught and treated as a
failed parse for that claim; the binder then tries the next claim in the alias list. This includes non-format
exceptions, so parser code should not rely on exceptions for control flow.

**Options are captured at first use.** The binder reads `IOptions<TokenUserOptions>` (not `IOptionsSnapshot`), so
configuration changes after the binder is created are not picked up.

**No API to remove default parsers or aliases.** Only additive/overriding configuration is supported. Use `AddParser<T>`
to replace a default parser; there is no `RemoveParser<T>`.

**No support for `FromTokenAttribute.Name`.** The claim type is derived from the parameter name (or an alias), and the
attribute currently has no `Name` property.

## Package Metadata

| Field             | 	Value                                        |
|-------------------|-----------------------------------------------|
| Package ID        | 	FromToken                                    |
| Target framework	 | net10.0                                       |
| Root namespace	   | FromToken                                     |
| License           | 	MIT                                          |
| Tags              | 	aspnetcore, token, authentication, fromtoken |

## License

MIT



