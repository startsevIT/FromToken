using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FromToken;

/// <summary>
/// Provides extension methods for registering the <c>FromToken</c> feature with an
/// <see cref="IServiceCollection"/> and for extending <see cref="TokenUserOptions"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all services required by the <c>FromToken</c> feature: options,
    /// the default value parsers and claim aliases, the model binder provider for
    /// <see cref="FromTokenAttribute"/>-decorated parameters, and the Swagger
    /// operation filter that hides token-bound parameters from the OpenAPI document.
    /// </summary>
    /// <param name="services">The service collection to add the feature to.</param>
    /// <param name="configure">
    /// An optional delegate to further configure <see cref="TokenUserOptions"/>.
    /// It is applied after the library's defaults, so it can override or extend them.
    /// </param>
    /// <returns>The same <see cref="IServiceCollection"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// <para>The method performs the following registrations:</para>
    /// <list type="bullet">
    ///   <item><description>
    ///     Registers <see cref="TokenUserOptions"/> via the options pattern.
    ///   </description></item>
    ///   <item><description>
    ///     Adds the built-in parsers for <see cref="string"/>, <see cref="Guid"/>,
    ///     <see cref="long"/>, and <see cref="int"/>. Parsers are safe: on failure
    ///     they return <see cref="Guid.Empty"/> or <c>0</c> rather than throwing.
    ///   </description></item>
    ///   <item><description>
    ///     Adds the default claim aliases for the user identifier:
    ///     <c>userId</c>, <c>sub</c>, and the WS-Federation
    ///     <c>nameidentifier</c> claim URI.
    ///   </description></item>
    ///   <item><description>
    ///     Inserts <c>UserIdModelBinderProvider</c> at the beginning of
    ///     <see cref="MvcOptions.ModelBinderProviders"/> so it is consulted first.
    ///   </description></item>
    ///   <item><description>
    ///     Registers <c>FromTokenOperationFilter</c> with Swashbuckle so that
    ///     token-bound parameters are excluded from the generated OpenAPI document.
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Program.cs
    /// builder.Services.AddControllers();
    /// builder.Services.AddFromTokenAttribute(options =>
    /// {
    ///     options.AddClaimAlias("tenantId", "tid");
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddFromTokenAttribute(
        this IServiceCollection services,
        Action<TokenUserOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<TokenUserOptions>();

        services.Configure<TokenUserOptions>(options =>
        {
            options.AddParser<string>(value => value);
            options.AddParser(value => Guid.TryParse(value, out var id) ? id : Guid.Empty);
            options.AddParser(value => long.TryParse(value, out var id) ? id : 0);
            options.AddParser(value => int.TryParse(value, out var id) ? id : 0);

            options.AddClaimAlias(
                "userId",
                "sub",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        });

        if (configure is not null)
            services.Configure(configure);

        services.Configure<MvcOptions>(options =>
        {
            options.ModelBinderProviders.Insert(0, new UserIdModelBinderProvider());
        });

        services.Configure<SwaggerGenOptions>(options =>
        {
            options.OperationFilter<FromTokenOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Registers a <see cref="TokenUserOptions"/> parser that deserializes a token
    /// claim value into <typeparamref name="T"/> using <see cref="System.Text.Json"/>.
    /// </summary>
    /// <typeparam name="T">The target type to deserialize the claim value into.</typeparam>
    /// <param name="options">The options instance to add the parser to.</param>
    /// <returns>The same <see cref="TokenUserOptions"/> instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// Deserialization uses <see cref="JsonSerializerOptions.PropertyNameCaseInsensitive"/>
    /// set to <see langword="true"/>. The parser does not catch exceptions: malformed JSON
    /// will propagate to the caller (typically the model binder).
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddFromTokenAttribute(options =>
    /// {
    ///     options.AddJsonParser&lt;TenantInfo&gt;();
    /// });
    /// </code>
    /// </example>
    public static TokenUserOptions AddJsonParser<T>(this TokenUserOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddParser<T>(value =>
            JsonSerializer.Deserialize<T>(value, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }));

        return options;
    }
}