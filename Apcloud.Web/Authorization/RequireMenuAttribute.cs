using Apcloud.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Apcloud.Web.Authorization;

[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method,AllowMultiple=true,Inherited=true)]
public sealed class RequireMenuAttribute : TypeFilterAttribute
{
    public RequireMenuAttribute(string moduleCode,string controller,string action) : base(typeof(RequireMenuFilter)) =>
        Arguments=[moduleCode,controller,action];
}

public sealed class RequireMenuFilter(
    string moduleCode,string controller,string action,
    ApcloudApiClient apiClient,ILogger<RequireMenuFilter> logger) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if(context.HttpContext.User.Identity?.IsAuthenticated!=true){context.Result=new ChallengeResult();return;}
        try
        {
            if(!await apiClient.HasMenuAccessAsync(moduleCode,controller,action,context.HttpContext.RequestAborted))
                context.Result=new ForbidResult();
        }
        catch(Exception exception) when(!context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogWarning(exception,"Could not verify menu access for {ModuleCode}/{Controller}/{Action}.",moduleCode,controller,action);
            context.Result=new ForbidResult();
        }
    }
}
