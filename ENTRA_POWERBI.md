# Microsoft Entra ID and Power BI setup

## 1. Register the API

Create a single-tenant Microsoft Entra app registration for `Apcloudpms.API`.

1. Under **Expose an API**, set an Application ID URI (normally `api://<api-client-id>`).
2. Add the delegated scope `access_as_user`.
3. Put the tenant and API client IDs in configuration:

```text
AzureAd__TenantId=<tenant-guid>
AzureAd__ClientId=<api-app-client-guid>
```

The API validates the issuer, signature, expiry, audience, and `access_as_user` scope.

## 2. Register the frontend SPA

Create a separate SPA app registration, configure its redirect URIs, and grant delegated
permission to `api://<api-client-id>/access_as_user`.

```javascript
const token = await msalInstance.acquireTokenSilent({
  account: msalInstance.getActiveAccount(),
  scopes: ["api://<api-client-id>/access_as_user"]
});

await fetch("https://<api-host>/api/module-access/my-modules", {
  headers: { Authorization: `Bearer ${token.accessToken}` }
});
```

Send the access token, not the ID token, to the API.

## 3. Configure Entra user provisioning

The first accepted Entra request links the `(tid, oid)` claims to a local `Users` row.
Email is profile data and is never used as the immutable identity key. The default configuration
assigns the local `User` role. Module and menu access comes from the active assignments configured
for that role.

Set `EntraProvisioning:AutoProvisionUsers` to `false` when accounts will be pre-provisioned by an
administrator or synchronization process.

## 4. Configure Power BI (app owns data)

The backend uses MSAL.NET client credentials to get a Power BI service token, then generates a
short-lived embed token. Configure these values outside source control:

```text
PowerBi__TenantId=<tenant-guid>                 # optional when same as AzureAd tenant
PowerBi__ClientId=<service-principal-client-id>
PowerBi__ClientSecret=<secret-from-secret-store>
PowerBi__WorkspaceId=<workspace-guid>
PowerBi__ReportId=<report-guid>
```

Enable service-principal Power BI API access in the Power BI/Fabric tenant settings and add the
service principal (or its security group) to the target workspace. Production deployments should
prefer a certificate or managed identity over a client secret where the hosting environment and
Power BI authentication scenario support it.

An authorized user assigned to the `POWERBI` module calls:

```http
GET /api/power-bi/embed-config
Authorization: Bearer <token-for-Apcloudpms.API>
```

The frontend passes the returned `embedUrl`, `reportId`, and `embedToken` to the Power BI JavaScript
client. It does not send the Power BI service token to the browser.

## Token boundaries

- The API accepts and validates only an access token issued for `Apcloudpms.API`.
- The backend Power BI service token is accepted only by the Power BI REST API.
- The embed token is accepted only by the Power BI embedded client/service for the configured item.
- A Power BI or Microsoft Graph token must never be accepted as authorization for `Apcloudpms.API`.
