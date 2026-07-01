var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services
	.AddHealthChecksUI(setup =>
	{
		setup.SetEvaluationTimeInSeconds(30);
		setup.MaximumHistoryEntriesPerEndpoint(50);
		setup.SetMinimumSecondsBetweenFailureNotifications(60);
	})
	.AddInMemoryStorage();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.MapHealthChecksUI(options =>
{
	options.UIPath = "/healthchecks-ui";
	options.ApiPath = "/healthchecks-api";
});

app.Run();
