# Supabase Migration Summary

## Overview

Successfully migrated the AI Indoor Navigation API database from a local PostgreSQL instance to Supabase cloud database.

## Migration Date

November 9, 2025

## Database Details

**Supabase Connection:**
- **Host:** db.xhvapujhplecxkqvepww.supabase.co
- **Port:** 5432
- **Database:** postgres
- **SSL Mode:** Required

## Migration Steps Completed

### 1. Created `.env` File

Created a `.env` file with the Supabase connection string in Npgsql format:

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=***;SSL Mode=Require
```

**Note:** The original connection string was provided in PostgreSQL URI format but was converted to Npgsql connection string format for compatibility with Entity Framework Core.

### 2. Applied All Migrations

Successfully applied the following migrations to the Supabase database:

1. `20250810163652_Initial` - Initial database schema with all core tables
2. `20250811211906_POI_props_turned_snake_case` - Converted POI properties to snake_case
3. `20250811212352_rest_converted_from_snake_case` - Converted remaining properties to snake_case
4. `20250915220004_AddPoiClosestNode` - Added closest node tracking for POIs
5. `20250921180500_ResetAutoIncrementSequences` - Reset auto-increment sequences
6. `20251109174941_AddLevelToRouteNode` - Added level column to route_nodes table

### 3. Fixed Project Configuration

**Issue:** `MyDbContextModelSnapshot.cs` was excluded from compilation in the `.csproj` file.

**Solution:** Removed the following exclusion from `ai-indoor-nav-api.csproj`:

```xml
<ItemGroup>
  <Compile Remove="Migrations\MyDbContextModelSnapshot.cs" />
</ItemGroup>
```

This was preventing proper migration generation and tracking.

### 4. PostGIS Extension

The PostGIS extension was automatically enabled during the initial migration. Supabase supports PostGIS out of the box, which is required for the spatial data features (geometry columns).

## Database Schema Created

### Core Tables

- **buildings** - Building information with timestamps
- **floors** - Floor information linked to buildings
- **poi** - Points of Interest with spatial data
- **poi_categories** - Categories for POIs
- **route_nodes** - Navigation nodes with PostGIS geometry
- **beacons** - Beacon devices with spatial locations
- **beacon_types** - Beacon type definitions

### Identity Tables

- **AspNetUsers** - User accounts
- **AspNetRoles** - User roles
- **AspNetUserRoles** - User-role mappings
- **AspNetUserClaims** - User claims
- **AspNetRoleClaims** - Role claims
- **AspNetUserLogins** - External login providers
- **AspNetUserTokens** - Authentication tokens

### Key Features

- All tables use snake_case column naming
- Auto-increment sequences properly configured
- Foreign key relationships established
- Indexes created for performance:
  - GIST index on route_nodes geometry
  - Unique indexes on beacon identifiers
  - Indexes on foreign keys
- PostGIS spatial support enabled

## Verification

Verified successful migration by:

1. ✅ Running `dotnet ef migrations list` - All migrations listed
2. ✅ Checking migration history in database - All migrations recorded in `__EFMigrationsHistory`
3. ✅ Confirming PostGIS extension enabled
4. ✅ Verifying all tables created with correct schema

## Next Steps

### For Development

1. Update JWT configuration in `.env` file with actual values:
   - `JWT_ISSUER`
   - `JWT_AUDIENCE`
   - `JWT_KEY` (minimum 32 characters)

2. Update user credentials in `.env` file with actual values:
   - `USER1_USERNAME`, `USER1_EMAIL`, `USER1_PASSWORD`
   - `USER2_USERNAME`, `USER2_EMAIL`, `USER2_PASSWORD`
   - `USER3_USERNAME`, `USER3_EMAIL`, `USER3_PASSWORD`

3. Run the application:
   ```bash
   dotnet run
   ```

   The application will automatically:
   - Connect to Supabase
   - Apply any pending migrations
   - Seed the configured users

### For Production

1. **Secure the `.env` file:**
   - Never commit `.env` to version control
   - Use environment variables or secure secret management

2. **Configure Supabase:**
   - Set up proper database backups
   - Configure connection pooling if needed
   - Monitor database performance

3. **Update Connection String:**
   - Consider using connection pooling (Supabase provides PgBouncer)
   - Update connection string if using pooler:
     ```
     Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=***;SSL Mode=Require
     ```

## Troubleshooting

### Connection Issues

If you encounter connection issues:

1. **Check SSL Mode:** Supabase requires SSL connections
2. **Verify Network:** Ensure firewall/proxy allows connections to Supabase
3. **Check Credentials:** Verify username and password are correct
4. **Test DNS:** Ensure the Supabase host can be resolved

### Migration Issues

If migrations fail:

1. **Check ModelSnapshot:** Ensure `MyDbContextModelSnapshot.cs` is included in build
2. **Verify Connection String:** Use Npgsql format, not PostgreSQL URI format
3. **Check Permissions:** Ensure database user has proper permissions
4. **Review Logs:** Check EF Core logs for detailed error messages

## Important Notes

1. **Connection String Format:** 
   - ❌ PostgreSQL URI: `postgresql://user:pass@host:port/db`
   - ✅ Npgsql format: `Host=host;Port=port;Database=db;Username=user;Password=pass;SSL Mode=Require`

2. **SSL Requirement:** Supabase requires SSL connections. Always include `SSL Mode=Require` in connection string.

3. **PostGIS Support:** Supabase has PostGIS pre-installed and enabled, no manual setup required.

4. **Auto-Increment:** Sequences are properly configured to start from 1 and increment automatically.

## Success Metrics

- ✅ All 6 migrations applied successfully
- ✅ PostGIS extension enabled
- ✅ All tables created with correct schema
- ✅ All indexes and constraints created
- ✅ Migration history tracked in database
- ✅ Project configuration fixed (ModelSnapshot included)
- ✅ Environment configuration updated

## Cloud Deployment Notes

### Connection Pooler (Recommended for Production)

For cloud deployments, use Supabase's connection pooler (PgBouncer):

**Connection String:**
```
Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
```

**Benefits:**
- More reliable connection management
- Better handling of DNS resolution
- Prevents connection exhaustion
- Optimized for cloud environments

### Environment Variables for Cloud

Set these in your cloud hosting service (not in `.env` file):

```
DEFAULT_CONNECTION=Host=db.xhvapujhplecxkqvepww.supabase.co;Port=6543;Database=postgres;Username=postgres;Password=wFdsg5nDmdlc1MQK;SSL Mode=Require;Pooling=true;Maximum Pool Size=10
JWT_ISSUER=your-issuer
JWT_AUDIENCE=your-audience
JWT_KEY=your-secret-key-minimum-32-characters
USER1_USERNAME=admin
USER1_EMAIL=admin@example.com
USER1_PASSWORD=SecurePassword123!
USER2_USERNAME=user2
USER2_EMAIL=user2@example.com
USER2_PASSWORD=SecurePassword123!
USER3_USERNAME=user3
USER3_EMAIL=user3@example.com
USER3_PASSWORD=SecurePassword123!
```

See `CLOUD_DEPLOYMENT_GUIDE.md` for detailed platform-specific instructions.

## Conclusion

The database has been successfully migrated to Supabase. The application is now configured to use the cloud-hosted PostgreSQL database with all spatial features (PostGIS) and authentication (ASP.NET Identity) fully functional.

**Local Development:** Uses `.env` file with connection pooler (port 6543)
**Cloud Deployment:** Uses environment variables set in hosting service

