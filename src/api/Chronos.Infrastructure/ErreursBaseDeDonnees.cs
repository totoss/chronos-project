using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Chronos.Infrastructure;

public static class ErreursBaseDeDonnees
{
    public static bool EstViolationUnicite(DbUpdateException exception) => exception.InnerException switch
    {
        PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } => true,
        SqlException { Number: 2601 or 2627 } => true,
        _ => false,
    };
}
