# SCEAMS – Phase 136 submission package

## Package contents

- `SCEAMS.sln` with exactly three runnable projects.
- `SCEAMS.API`, `SCEAMS.MVC`, `SCEAMS.NotificationService` source code.
- API migrations and idempotent Development seed fixtures.
- `docs/architecture/SCEAMS_ERD.svg` and Mermaid source.
- `docs/TECHNICAL_DOCUMENTATION.md` and Phase 134–135 DOCX.
- `docs/RUNBOOK.md`, `README.md` and demo dataset manifest.
- Postman functional, security, E2E and OData collections plus environment examples.
- `SCEAMS.API/appsettings.Example.json` with placeholders only.

## Clean-checkout gate

```bash
dotnet restore SCEAMS.sln
dotnet build SCEAMS.sln
dotnet test SCEAMS.sln --no-build --verbosity minimal
dotnet sln SCEAMS.sln list
bash tools/package-audit.sh
```

Do not include `bin/`, `obj/`, `.vs/`, local appsettings, database files, token,
password or API key. Local developer changes to tracked appsettings are kept
outside the package and must not be staged.
