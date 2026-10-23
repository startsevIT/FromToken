using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace FromToken;

/// <summary>
/// Model binder that resolves a <see cref="FromTokenAttribute"/>-decorated action
/// parameter from the claims of the current user principal.
/// </summary>
/// <remarks>
/// <para>
/// Resolution strategy:
/// </para>
/// <list type="number">
///   <item><description>
///     A value parser is selected by the parameter's <see cref="ModelBindingContext.ModelType"/>.
///     If no parser is registered, <see cref="JsonSerializer.Deserialize(string, Type, JsonSerializerOptions?)"/>
///     is used as a fallback.
///   </description></item>
///   <item><description>
///     Claim types are determined by the parameter's <see cref="ModelBindingContext.FieldName"/>
///     looked up in <see cref="TokenUserOptions.ClaimAliases"/>. If no alias exists,
///     the field name itself is used as the claim type.
///   </description></item>
///   <item><description>
///     The user's claims are searched in order; the first claim whose value parses
///     successfully wins.
///   </description></item>
///   <item><description>
///     If no claim is found or none of them parse, the binding result is
///     <see cref="ModelBindingResult.Failed()"/>.
///   </description></item>
/// </list>
/// <para>
/// Exceptions thrown by a parser are swallowed and treated as a failed parse for
/// that claim; the binder then tries the next claim in the alias list.
/// </para>
/// </remarks>
internal sealed class UserIdModelBinder(IOptions<TokenUserOptions> options) : IModelBinder
{
    private readonly TokenUserOptions _options = options.Value;

    /// <summary>
    /// Attempts to bind the action parameter from the user's claims.
    /// </summary>
    /// <param name="bindingContext">The model binding context.</param>
    /// <returns>A completed task; the result is written to
    /// <see cref="ModelBindingContext.Result"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bindingContext"/> is <see langword="null"/>.
    /// </exception>
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        if (!_options.Parsers.TryGetValue(bindingContext.ModelType, out var parser))
            parser = value => JsonSerializer.Deserialize(value, bindingContext.ModelType);

        var claimTypes = _options.ClaimAliases.TryGetValue(
            bindingContext.FieldName,
            out var aliases)
            ? aliases
            : [bindingContext.FieldName];

        var user = bindingContext.HttpContext.User;

        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;

            if (value is null)
                continue;

            try
            {
                var parsed = parser(value);
                bindingContext.Result = ModelBindingResult.Success(parsed);
                return Task.CompletedTask;
            }
            catch
            {
                // Try the next claim.
            }
        }

        bindingContext.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }
}