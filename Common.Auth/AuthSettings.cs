namespace Common.Auth;

public class AuthSettings
{
	public const string SectionName = "AuthSettings";

	/// <summary>Identity Server authority URL (e.g. <c>http://localhost:5009</c>).</summary>
	public string Authority { get; set; } = string.Empty;

	/// <summary>Required audience claim. Defaults to <c>tedu.microservices</c>.</summary>
	public string Audience { get; set; } = "tedu.microservices";

	/// <summary>Scope name this API expects on incoming access tokens (e.g. <c>product.api</c>).</summary>
	public string RequiredScope { get; set; } = string.Empty;

	/// <summary>When <c>true</c>, allow HTTP metadata (dev only).</summary>
	public bool RequireHttpsMetadata { get; set; } = false;
}
