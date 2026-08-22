# JWT authentication and refresh tokens

## Configuration

Set secrets outside committed JSON files in production. ASP.NET Core maps these environment variables to configuration:

```text
ConnectionStrings__DefaultConnection=<SQL Server connection string>
Jwt__Key=<at least 32 cryptographically random bytes>
Jwt__Issuer=ApcloudpmsAPI
Jwt__Audience=ApcloudpmsClient
Jwt__AccessTokenMinutes=15
Jwt__RefreshTokenDays=7
Jwt__RefreshTokenAbsoluteDays=30
```

All API instances must share the signing key, issuer, audience, and database. Rotate the signing key through a managed secret store as part of a planned deployment.

## Endpoints

- `POST /api/auth/register` creates a normal `User`; public registration cannot assign the `Admin` role.
- `POST /api/auth/login` accepts `userName` and `password` and returns an access/refresh token pair.
- `POST /api/auth/refresh` accepts `refreshToken` and returns a completely new pair. Discard the old refresh token immediately.
- `POST /api/auth/revoke` accepts `refreshToken`, revokes its whole login session, and always returns `204`.

Send the access token as `Authorization: Bearer <access-token>`. Access tokens are stateless and expire after 15 minutes by default. Refresh tokens are rotating, stored as SHA-256 hashes, limited to a seven-day sliding lifetime, and cannot extend a login session beyond 30 days.

For browser applications, keep the refresh token in a Secure, HttpOnly, SameSite cookie managed by the backend-for-frontend; do not place it in local storage. Native clients should use the platform's secure credential storage.

## Database deployment

Back up the database, confirm migration `20260617175007_AddUserTable` is already applied, and run:

```text
Database/Scripts/20260815_AddJwtRefreshTokens.sql
```

The script renames `Users.Password` to `PasswordHash`, adds user security fields, creates `RefreshTokens`, adds lookup indexes, and records the EF migration. It intentionally fails before changing anything if usernames would be truncated or collide after normalization.

The equivalent EF command is:

```powershell
dotnet ef database update --project Apcloudpms.Infrastructure --startup-project Apcloudpms.API
```

## Roles and organization structure

After applying the JWT migration, run `Database/Scripts/20260815_AddRolesBranchesDepartments.sql`. It creates:

- `dbo.Role`, including active `User` and `Admin` roles;
- `dbo.UserRole`, with a composite key for many-to-many assignments;
- `dbo.OfficeBranch`, with a filtered unique index allowing only one active head office;
- `dbo.Department`, where every department has a required office-branch foreign key.

The migration copies every legacy `Users.Role` value into `dbo.Role`, creates the corresponding active `dbo.UserRole` assignment, and only then removes `Users.Role`. Empty legacy roles receive `User`.

Role and organization management endpoints require an `Admin` access token:

```text
GET/POST/PUT  /api/roles
PUT           /api/roles/user-assignment
GET/POST/PUT  /api/organization/branches
GET/POST/PUT  /api/organization/departments
```

The first active branch must be submitted with `isHeadOffice: true`. Assigning another active branch as head office automatically removes that designation from the previous branch. An active head office cannot be disabled directly, and a branch cannot be disabled while it owns active departments.

If no legacy user was assigned `Admin`, edit the username placeholder and run `Database/Scripts/AssignInitialAdmin.sql` once. Log in again afterward so the newly issued access token contains the `Admin` role claim.

## Operations at scale

Access-token validation does not query SQL Server. Only login, refresh, registration, and revocation use the database. Refresh lookups use a unique fixed-length hash index, rotation uses SQL Server row-version concurrency, EF contexts are pooled, and transient database operations are retried.

Schedule this bounded cleanup through SQL Server Agent or the deployment platform (for example, hourly) so expired token history does not grow forever:

```sql
WHILE 1 = 1
BEGIN
    DELETE TOP (10000)
    FROM dbo.RefreshTokens
    WHERE ExpiresAtUtc < DATEADD(DAY, -1, SYSUTCDATETIME());

    IF @@ROWCOUNT < 10000 BREAK;
END;
```

If the API runs behind a reverse proxy, configure ASP.NET Core forwarded headers with explicit trusted proxies/networks before using the included per-IP login limiter. For multi-instance deployments, enforce additional distributed rate limits at the API gateway. Monitor SQL connection-pool saturation, failed logins, refresh-token reuse, 401/429 rates, and cleanup duration under expected load.
