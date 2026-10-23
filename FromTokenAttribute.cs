using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FromToken;

/// <summary>
/// Indicates that an action method parameter should be bound from a token
/// using a custom binding source instead of the default model binding sources
/// (query string, route, body, form, or header).
/// </summary>
/// <remarks>
/// <para>
/// This attribute implements <see cref="IBindingSourceMetadata"/> and returns
/// <see cref="BindingSource.Custom"/> from <see cref="BindingSource"/>. As a result,
/// ASP.NET Core MVC will not attempt to bind the decorated parameter from standard
/// sources and will instead delegate binding to a model binder registered for the
/// custom binding source.
/// </para>
/// <para>
/// A corresponding <c>IModelBinder</c> (or <c>IModelBinderProvider</c>) that handles
/// the custom binding source must be registered; otherwise the parameter will fail
/// to bind at runtime.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ApiController]
/// [Route("api/[controller]")]
/// public sealed class OrdersController : ControllerBase
/// {
///     [HttpGet("{id}")]
///     public IActionResult GetOrder([FromToken] string tenantId, int id)
///     {
///         return Ok(new { tenantId, id });
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromTokenAttribute : Attribute, IBindingSourceMetadata
{
    /// <summary>
    /// Gets the binding source used to resolve the decorated parameter.
    /// </summary>
    /// <value>
    /// Always <see cref="BindingSource.Custom"/>, indicating that a custom
    /// model binder is responsible for resolving the value.
    /// </value>
    public BindingSource BindingSource => BindingSource.Custom;
}