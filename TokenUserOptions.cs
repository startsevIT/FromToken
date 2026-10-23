namespace FromToken;

/// <summary>
/// Represents configuration options for the <c>FromToken</c> feature, including
/// the set of value parsers and claim aliases used to resolve token-bound parameters.
/// </summary>
/// <remarks>
/// <para>
/// An instance is normally configured via
/// <see cref="ServiceCollectionExtensions.AddFromTokenAttribute"/>:
/// </para>
/// <code>
/// builder.Services.AddFromTokenAttribute(options =>
/// {
///     options.AddClaimAlias("tenantId", "tid");
///     options.AddJsonParser&lt;TenantInfo&gt;();
/// });
/// </code>
/// <para>
/// The library pre-registers parsers for <see cref="string"/>, <see cref="Guid"/>,
/// <see cref="long"/>, and <see cref="int"/>, plus default claim aliases for the
/// user identifier. User-supplied configuration is applied after the defaults and
/// therefore overrides them when the same key is used.
/// </para>
/// </remarks>
public sealed class TokenUserOptions
{
    /// <summary>
    /// Gets the registered value parsers, keyed by the target type.
    /// </summary>
    /// <remarks>
    /// Each parser receives the raw claim value as a <see cref="string"/> and returns
    /// the parsed value as <see cref="object"/> (or <see langword="null"/> on failure).
    /// A single parser is registered per target type; re-registering the same type
    /// replaces the previous parser.
    /// </remarks>
    internal Dictionary<Type, Func<string, object?>> Parsers { get; } = new();

    /// <summary>
    /// Gets the claim aliases, mapping a parameter name to one or more claim types
    /// that may supply its value.
    /// </summary>
    /// <remarks>
    /// Keys are compared using <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </remarks>
    internal Dictionary<string, string[]> ClaimAliases { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a parser that converts a raw claim value into the specified type
    /// <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type the claim value is parsed into.</typeparam>
    /// <param name="parser">
    /// A delegate that receives the raw claim value and returns the parsed result.
    /// It may return <see langword="null"/> (or <c>default(T)</c> for value types)
    /// to signal a failed parse.
    /// </param>
    /// <returns>The same <see cref="TokenUserOptions"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="parser"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Registering a parser for a type that already has one replaces the existing
    /// parser.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.AddParser(value => DateTimeOffset.TryParse(value, out var dto) ? dto : null);
    /// </code>
    /// </example>
    public TokenUserOptions AddParser<T>(Func<string, T?> parser)
    {
        ArgumentNullException.ThrowIfNull(parser);
        Parsers[typeof(T)] = value => parser(value);
        return this;
    }

    /// <summary>
    /// Maps an action parameter name to one or more claim types that may supply
    /// its value.
    /// </summary>
    /// <param name="parameterName">
    /// The name of the action parameter (case-insensitive). Must not be
    /// <see langword="null"/>, empty, or whitespace.
    /// </param>
    /// <param name="claimTypes">
    /// The claim types to look up, in priority order. Must not be <see langword="null"/>;
    /// an empty array is allowed and effectively disables the alias.
    /// </param>
    /// <returns>The same <see cref="TokenUserOptions"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="parameterName"/> is <see langword="null"/>, empty,
    /// or consists only of white-space characters.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="claimTypes"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// If an alias for <paramref name="parameterName"/> already exists, it is replaced.
    /// </remarks>
    /// <example>
    /// <code>
    /// options.AddClaimAlias(
    ///     "userId",
    ///     "sub",
    ///     "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
    /// </code>
    /// </example>
    public TokenUserOptions AddClaimAlias(string parameterName, params string[] claimTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameterName);
        ArgumentNullException.ThrowIfNull(claimTypes);

        ClaimAliases[parameterName] = claimTypes;
        return this;
    }
}