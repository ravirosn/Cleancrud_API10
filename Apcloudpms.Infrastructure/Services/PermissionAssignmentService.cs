using System.Data;
using Apcloudpms.Application.DTOs;
using Apcloudpms.Application.Interfaces;
using Apcloudpms.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Apcloudpms.Infrastructure.Services;

public sealed class PermissionAssignmentService(AppDbContext context, IAuditContext auditContext) : IPermissionAssignmentService
{
    public Task<PermissionAssignmentPagedResponseDto> GetAsync(PermissionAssignmentQueryDto query, CancellationToken cancellationToken) =>
        WithConnectionAsync(async connection =>
        {
            await using var command=Command(connection,"dbo.SPPermissionAssignmentGet");
            Add(command,"@PageNumber",SqlDbType.Int,query.PageNumber); Add(command,"@PageSize",SqlDbType.Int,query.PageSize);
            Add(command,"@SearchTerm",SqlDbType.NVarChar,Normalize(query.SearchTerm),200); Add(command,"@SortBy",SqlDbType.NVarChar,query.SortBy,30);
            Add(command,"@SortDirection",SqlDbType.VarChar,query.SortDirection,4); Add(command,"@IncludeInactive",SqlDbType.Bit,query.IncludeInactive);
            await using var reader=await command.ExecuteReaderAsync(cancellationToken);
            if(!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Permission assignment count was not returned.");
            var count=reader.GetInt64(reader.GetOrdinal("TotalRecords")); await reader.NextResultAsync(cancellationToken);
            var data=new List<PermissionAssignmentDto>(); while(await reader.ReadAsync(cancellationToken)) data.Add(Map(reader));
            var pages=count==0?0:(count+query.PageSize-1)/query.PageSize;
            return new PermissionAssignmentPagedResponseDto(data,count,pages,query.PageNumber,query.PageSize,count>0&&query.PageNumber>1,query.PageNumber<pages);
        },cancellationToken);

    public Task<IReadOnlyList<PermissionRoleOptionDto>> GetRolesAsync(CancellationToken cancellationToken) => ReadAsync(
        "dbo.SPPermissionRoleDdl",null,r=>new PermissionRoleOptionDto(r.GetInt32(r.GetOrdinal("Id")),r.GetString(r.GetOrdinal("Name"))),cancellationToken);

    public Task<IReadOnlyList<PermissionPolicyOptionDto>> GetPoliciesAsync(int roleId,CancellationToken cancellationToken) => ReadAsync(
        "dbo.SPPermissionPolicyDdl",c=>Add(c,"@RoleId",SqlDbType.Int,roleId),r=>new PermissionPolicyOptionDto(
            r.GetInt32(r.GetOrdinal("Id")),r.GetString(r.GetOrdinal("Code")),r.GetString(r.GetOrdinal("Name")),
            r.GetString(r.GetOrdinal("ModuleCode")),r.GetString(r.GetOrdinal("MenuName")),r.GetBoolean(r.GetOrdinal("IsAssigned")),r.GetBoolean(r.GetOrdinal("CanAssign"))),cancellationToken);

    public Task<PermissionAssignmentDto> CreateAsync(PermissionAssignmentRequestDto request,CancellationToken cancellationToken) => WriteAsync("dbo.SPPermissionAssignmentIns",null,null,request,cancellationToken);
    public async Task<PermissionAssignmentDto?> UpdateAsync(int roleId,int permissionPolicyId,PermissionAssignmentRequestDto request,CancellationToken cancellationToken)
    { try{return await WriteAsync("dbo.SPPermissionAssignmentUpd",roleId,permissionPolicyId,request,cancellationToken);}catch(KeyNotFoundException){return null;} }
    public Task<bool> DeleteAsync(int roleId,int permissionPolicyId,CancellationToken cancellationToken) => WithConnectionAsync(async connection=>
    { await using var c=Command(connection,"dbo.SPPermissionAssignmentDel"); Add(c,"@RoleId",SqlDbType.Int,roleId); Add(c,"@PermissionPolicyId",SqlDbType.Int,permissionPolicyId); Audit(c);
      try{return Convert.ToInt32(await c.ExecuteScalarAsync(cancellationToken))==1;}catch(SqlException e){throw Translate(e);} },cancellationToken);

    public async Task<IReadOnlyList<string>> GetGrantedCodesAsync(int userId,string moduleCode,string menuController,string menuAction,CancellationToken cancellationToken)
    {
        if(userId<=0)return [];
        if(await context.Users.AsNoTracking().AnyAsync(u=>u.Id==userId&&u.UserRoles.Any(ur=>ur.IsActive&&ur.Role.IsActive&&ur.Role.NormalizedName=="SUPERADMIN"),cancellationToken))
            return await context.PermissionPolicies.AsNoTracking().Where(p=>p.IsActive&&p.ModuleCode==moduleCode&&p.MenuController==menuController&&p.MenuAction==menuAction).Select(p=>p.Code).ToListAsync(cancellationToken);
        return await context.RolePermissions.AsNoTracking().Where(rp=>rp.IsActive&&rp.PermissionPolicy.IsActive&&rp.PermissionPolicy.ModuleCode==moduleCode&&rp.PermissionPolicy.MenuController==menuController&&rp.PermissionPolicy.MenuAction==menuAction&&rp.Role.IsActive&&rp.Role.UserRoles.Any(ur=>ur.UserId==userId&&ur.IsActive)&&rp.Role.RoleModules.Any(rm=>rm.IsActive&&rm.ApplicationModule.IsActive&&rm.ApplicationModule.Code==moduleCode&&rm.RoleModuleMenus.Any(rmm=>rmm.IsActive&&rmm.ModuleMenu.IsActive&&rmm.ModuleMenu.ControllerName==menuController&&rmm.ModuleMenu.ActionName==menuAction))).Select(rp=>rp.PermissionPolicy.Code).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<bool> HasMenuAccessAsync(int userId,string moduleCode,string menuController,string menuAction,CancellationToken cancellationToken)
    {
        if(userId<=0)return false;
        if(await context.Users.AsNoTracking().AnyAsync(u=>u.Id==userId&&u.UserRoles.Any(ur=>ur.IsActive&&ur.Role.IsActive&&ur.Role.NormalizedName=="SUPERADMIN"),cancellationToken))return true;
        return await context.RoleModuleMenus.AsNoTracking().AnyAsync(rmm=>rmm.IsActive&&rmm.RoleModule.IsActive&&rmm.RoleModule.ApplicationModule.IsActive&&rmm.RoleModule.ApplicationModule.Code==moduleCode&&rmm.ModuleMenu.IsActive&&rmm.ModuleMenu.ControllerName==menuController&&rmm.ModuleMenu.ActionName==menuAction&&rmm.RoleModule.Role.IsActive&&rmm.RoleModule.Role.UserRoles.Any(ur=>ur.UserId==userId&&ur.IsActive),cancellationToken);
    }

    private Task<PermissionAssignmentDto> WriteAsync(string procedure,int? originalRoleId,int? originalPolicyId,PermissionAssignmentRequestDto request,CancellationToken token)=>WithConnectionAsync(async connection=>
    { await using var c=Command(connection,procedure); if(originalRoleId.HasValue){Add(c,"@OriginalRoleId",SqlDbType.Int,originalRoleId);Add(c,"@OriginalPermissionPolicyId",SqlDbType.Int,originalPolicyId);}
      Add(c,"@RoleId",SqlDbType.Int,request.RoleId);Add(c,"@PermissionPolicyId",SqlDbType.Int,request.PermissionPolicyId);Add(c,"@IsActive",SqlDbType.Bit,request.IsActive);Audit(c);
      try{await using var r=await c.ExecuteReaderAsync(token);if(!await r.ReadAsync(token))throw new KeyNotFoundException();return Map(r);}catch(SqlException e){throw Translate(e);} },token);
    private Task<IReadOnlyList<T>> ReadAsync<T>(string procedure,Action<SqlCommand>? setup,Func<SqlDataReader,T> map,CancellationToken token)=>WithConnectionAsync<IReadOnlyList<T>>(async connection=>
    {await using var c=Command(connection,procedure);setup?.Invoke(c);var data=new List<T>();await using var r=await c.ExecuteReaderAsync(token);while(await r.ReadAsync(token))data.Add(map(r));return data;},token);
    private static PermissionAssignmentDto Map(SqlDataReader r){var active=r.GetBoolean(r.GetOrdinal("IsActive"));return new($"{r.GetInt32(r.GetOrdinal("RoleId"))}:{r.GetInt32(r.GetOrdinal("PermissionPolicyId"))}",r.GetInt32(r.GetOrdinal("RoleId")),r.GetString(r.GetOrdinal("RoleName")),r.GetInt32(r.GetOrdinal("PermissionPolicyId")),r.GetString(r.GetOrdinal("PermissionCode")),r.GetString(r.GetOrdinal("PermissionName")),r.GetString(r.GetOrdinal("ModuleCode")),r.GetString(r.GetOrdinal("MenuController")),r.GetString(r.GetOrdinal("MenuAction")),active,active?"Active":"Inactive",r.GetDateTime(r.GetOrdinal("AssignedAtUtc")),NullableString(r,"AssignedBy"),NullableDate(r,"ModifiedAtUtc"),NullableString(r,"ModifiedBy"));}
    private void Audit(SqlCommand c){Add(c,"@ActorUserId",SqlDbType.Int,auditContext.UserId);Add(c,"@ActorName",SqlDbType.NVarChar,Normalize(auditContext.UserName),256);Add(c,"@TraceId",SqlDbType.NVarChar,Normalize(auditContext.TraceId),100);Add(c,"@IpAddress",SqlDbType.NVarChar,Normalize(auditContext.IpAddress),45);}
    private async Task<T> WithConnectionAsync<T>(Func<SqlConnection,Task<T>> action,CancellationToken token){var c=(SqlConnection)context.Database.GetDbConnection();var close=c.State==ConnectionState.Closed;if(close)await context.Database.OpenConnectionAsync(token);try{return await action(c);}finally{if(close)await context.Database.CloseConnectionAsync();}}
    private static SqlCommand Command(SqlConnection c,string name)=>new(name,c){CommandType=CommandType.StoredProcedure};
    private static void Add(SqlCommand c,string n,SqlDbType t,object? v,int? s=null){var p=s.HasValue?c.Parameters.Add(n,t,s.Value):c.Parameters.Add(n,t);p.Value=v??DBNull.Value;}
    private static Exception Translate(SqlException e)=>e.Number>=50000?new ArgumentException(e.Message,e):e;
    private static string? Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private static string? NullableString(SqlDataReader r,string n){var o=r.GetOrdinal(n);return r.IsDBNull(o)?null:r.GetString(o);}
    private static DateTime? NullableDate(SqlDataReader r,string n){var o=r.GetOrdinal(n);return r.IsDBNull(o)?null:r.GetDateTime(o);}
}
