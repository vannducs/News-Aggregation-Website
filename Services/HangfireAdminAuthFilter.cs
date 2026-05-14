using Hangfire.Dashboard;

namespace NewsAggregator.Services
{
    public class HangfireAdminAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            return httpContext.User.Identity?.IsAuthenticated == true
                && httpContext.User.IsInRole(Models.UserRoles.Admin);
        }
    }
}
