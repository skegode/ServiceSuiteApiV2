using System.Data;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using ServiceSuiteApiV2.Controllers;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2
{
    public class LoanService : ILoanService
    {
        private readonly IConfiguration _config;
        private string ConnectionString => _config.GetConnectionString("DefaultConnection")!;

        public LoanService(IConfiguration config)
        {
            _config = config;
        }

        private static bool TryParseOrgId(string entityId, out int orgId) =>
            int.TryParse(entityId, out orgId);

        private static bool TryParseLoanId(string loanId, out int id) =>
            int.TryParse(loanId, out id);

        // ─── Shared SQL fragments ────────────────────────────────────────────────

        private const string LoanColumns = """
            CAST(l.id AS NVARCHAR)                                        AS id,
            CAST(l.BorrowerId AS NVARCHAR)                                AS BorrowerId,
            ISNULL(b.firstName, '')                                       AS firstName,
            ISNULL(b.otherName, '')                                       AS otherName,
            ISNULL(b.PhoneNumber, '')                                     AS PhoneNumber,
            ISNULL(b.EmailAddress, '')                                    AS EmailAddress,
            ISNULL(b.NationalID, '')                                      AS NationalID,
            l.AmountToDisburse                                            AS AmountToDisburse,
            ISNULL(p.ProductName, '')                                     AS repaymentperiod,
            l.LoanBalance                                                 AS LoanBalance,
            ISNULL(l.Penalty, 0)                                          AS Penalty,
            ISNULL(p.ProductName, '')                                     AS ProductName,
            ''                                                            AS Branch,
            ISNULL(u.FirstName + ' ' + u.OtherName, '')                  AS Agent,
            ISNULL(CAST(u.ID AS NVARCHAR), '')                            AS AgentId,
            SUM(CASE WHEN ls.ExpectedDueDate < CAST(GETDATE() AS DATE)
                          AND ls.amounttopay > 0
                     THEN ls.amounttopay ELSE 0 END)                      AS Arrears,
            DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE())             AS DaysInArrears,
            SUM(ls.amounttopay)                                           AS OutsourcedAmount
            """;

        private const string LoanJoins = """
            FROM loanSchedule ls
            INNER JOIN Loans l        ON l.id  = ls.Loanid
            LEFT  JOIN Borrowers b    ON b.ID  = l.BorrowerId
            LEFT  JOIN Products p     ON p.ID  = l.ProductId
            LEFT  JOIN UserMaster u   ON u.ID  = l.collectionAgentID
            """;

        private const string LoanGroupBy = """
            GROUP BY l.id, l.BorrowerId, b.firstName, b.otherName, b.PhoneNumber,
                     b.EmailAddress, b.NationalID, l.AmountToDisburse, p.ProductName,
                     l.LoanBalance, l.Penalty, u.FirstName, u.OtherName, u.ID
            """;

        // ─── Public methods ──────────────────────────────────────────────────────

        public async Task<LoanResponse> GetLoansAsync(LoanFilterRequest filter)
        {
            if (!TryParseOrgId(filter.EntityId, out int orgId))
                return new LoanResponse { Count = 0, Data = new List<LoanDto>() };

            var loans = new List<LoanDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = new StringBuilder($"SELECT {LoanColumns} {LoanJoins}");
            sql.Append(" WHERE l.LoanBalance > 0 AND ls.amounttopay > 0 AND l.EntityId = @EntityId");

            await using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.AddWithValue("@EntityId", orgId);

            if (filter.MinAmount.HasValue)
            {
                sql.Append(" AND l.AmountToDisburse >= @MinAmount");
                cmd.Parameters.AddWithValue("@MinAmount", filter.MinAmount.Value);
            }
            if (filter.MaxAmount.HasValue)
            {
                sql.Append(" AND l.AmountToDisburse <= @MaxAmount");
                cmd.Parameters.AddWithValue("@MaxAmount", filter.MaxAmount.Value);
            }
            if (filter.MinOlb.HasValue)
            {
                sql.Append(" AND l.LoanBalance >= @MinOlb");
                cmd.Parameters.AddWithValue("@MinOlb", filter.MinOlb.Value);
            }
            if (filter.MaxOlb.HasValue)
            {
                sql.Append(" AND l.LoanBalance <= @MaxOlb");
                cmd.Parameters.AddWithValue("@MaxOlb", filter.MaxOlb.Value);
            }

            sql.Append($" {LoanGroupBy}");

            if (filter.MinDays.HasValue && filter.MaxDays.HasValue)
            {
                sql.Append(" HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) BETWEEN @MinDays AND @MaxDays");
                cmd.Parameters.AddWithValue("@MinDays", filter.MinDays.Value);
                cmd.Parameters.AddWithValue("@MaxDays", filter.MaxDays.Value);
            }
            else if (filter.MinDays.HasValue)
            {
                sql.Append(" HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) >= @MinDays");
                cmd.Parameters.AddWithValue("@MinDays", filter.MinDays.Value);
            }
            else if (filter.MaxDays.HasValue)
            {
                sql.Append(" HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) <= @MaxDays");
                cmd.Parameters.AddWithValue("@MaxDays", filter.MaxDays.Value);
            }

            cmd.CommandText = sql.ToString();
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                loans.Add(MapToLoanDto(reader));

            return new LoanResponse { Count = loans.Count, Data = loans };
        }

        public async Task<LoanResponse> GetActiveLoansAsync(string entityId, string? searchTerm, int top = 20)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return new LoanResponse { Count = 0, Data = new List<LoanDto>() };

            var loans = new List<LoanDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = new StringBuilder($"""
                SELECT TOP (@Top)
                {LoanColumns}
                {LoanJoins}
                WHERE l.LoanBalance > 0
                  AND ls.amounttopay > 0
                  AND l.EntityId = @EntityId
                """);

            await using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.AddWithValue("@Top", top);
            cmd.Parameters.AddWithValue("@EntityId", orgId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                sql.Append("""
                   AND (b.PhoneNumber  LIKE @Search
                     OR b.firstName   LIKE @Search
                     OR b.otherName   LIKE @Search
                     OR CAST(l.id AS NVARCHAR) LIKE @Search)
                   """);
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");
            }

            sql.Append($" {LoanGroupBy}");
            cmd.CommandText = sql.ToString();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                loans.Add(MapToLoanDto(reader));

            return new LoanResponse { Count = loans.Count, Data = loans };
        }

        public async Task<LoanDto?> GetLoanByIdAsync(string entityId, string loanId)
        {
            if (!TryParseOrgId(entityId, out int orgId) || !TryParseLoanId(loanId, out int id))
                return null;

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = $"""
                SELECT {LoanColumns}
                {LoanJoins}
                WHERE l.id = @LoanId AND l.EntityId = @EntityId AND ls.amounttopay > 0
                {LoanGroupBy}
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LoanId", id);
            cmd.Parameters.AddWithValue("@EntityId", orgId);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapToLoanDto(reader) : null;
        }

        public async Task<List<LoanDetailDto>> GetLoanDetailsAsync(string entityId, string loanId)
        {
            if (!TryParseOrgId(entityId, out int orgId) || !TryParseLoanId(loanId, out int id))
                return new List<LoanDetailDto>();

            var details = new List<LoanDetailDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = """
                SELECT bd.ID,
                       CAST(l.id AS NVARCHAR)   AS LoanId,
                       fi.itemName               AS ItemName,
                       ISNULL(bd.stringValue,
                              CAST(ISNULL(bd.numericValue, 0) AS VARCHAR)) AS ItemValue
                FROM BorrowerDetails  bd
                INNER JOIN BorrowerFormItems fi ON fi.ID = bd.itemId
                INNER JOIN Loans              l  ON l.BorrowerId = bd.borrowerId
                WHERE l.id = @LoanId AND l.EntityId = @EntityId
                ORDER BY bd.ID DESC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LoanId", id);
            cmd.Parameters.AddWithValue("@EntityId", orgId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                details.Add(new LoanDetailDto
                {
                    Id = Convert.ToInt32(reader["ID"]),
                    LoanId = reader["LoanId"]?.ToString() ?? "",
                    ItemName = reader["ItemName"]?.ToString() ?? "",
                    ItemValue = reader["ItemValue"] == DBNull.Value ? "" : reader["ItemValue"].ToString()!
                });
            }
            return details;
        }

        public async Task<bool> ManageLoanAsync(LoanManagementRequest request)
        {
            if (!TryParseOrgId(request.EntityId, out int orgId) || !int.TryParse(request.UserId, out int userId))
                throw new Exception("EntityId and UserId must be numeric.");

            var approvalStatus = string.Equals(request.ActionType, "Approve", StringComparison.OrdinalIgnoreCase)
                ? "APPROVED" : "PENDING";

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var loanNumbers = request.LoanIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var loanNumber in loanNumbers)
            {
                if (!TryParseLoanId(loanNumber, out int loanId)) continue;

                var currentBalance = await conn.QueryFirstOrDefaultAsync<decimal?>(
                    "SELECT LoanBalance FROM Loans WHERE id = @LoanId AND EntityId = @OrgId",
                    new { LoanId = loanId, OrgId = orgId });

                if (currentBalance == null) continue;

                var effectedAmount = request.ValueType == 1
                    ? currentBalance.Value * (request.Value / 100)   // percentage
                    : request.Value;                                   // fixed amount

                var newOlb = Math.Max(0, currentBalance.Value - effectedAmount);

                const string insertSql = """
                    INSERT INTO ManagedLoans
                        (LoanId, InitialOlb, EffectedAmount, NewOlb, DoneBy, DateDone, TransType, EntityId)
                    VALUES
                        (@LoanId, @InitialOlb, @EffectedAmount, @NewOlb, @DoneBy, GETDATE(), @TransType, @EntityId)
                    """;

                await conn.ExecuteAsync(insertSql, new
                {
                    LoanId = loanId,
                    InitialOlb = currentBalance.Value,
                    EffectedAmount = effectedAmount,
                    NewOlb = newOlb,
                    DoneBy = userId,
                    request.TransType,
                    EntityId = orgId
                });

                if (approvalStatus == "APPROVED")
                {
                    await conn.ExecuteAsync(
                        "UPDATE Loans SET LoanBalance = @NewOlb WHERE id = @LoanId AND EntityId = @OrgId",
                        new { NewOlb = newOlb, LoanId = loanId, OrgId = orgId });
                }
            }

            return true;
        }

        public async Task<bool> InitiateWriteoffAsync(WriteoffRequest request)
        {
            if (!TryParseOrgId(request.EntityId, out int orgId)
                || !int.TryParse(request.UserId, out int userId)
                || !TryParseLoanId(request.LoanId, out int loanId))
                throw new Exception("EntityId, UserId, and LoanId must be numeric.");

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var transaction = conn.BeginTransaction();

            try
            {
                var currentBalance = await conn.QueryFirstOrDefaultAsync<decimal?>(
                    "SELECT LoanBalance FROM Loans WHERE id = @LoanId AND EntityId = @OrgId",
                    new { LoanId = loanId, OrgId = orgId }, transaction);

                if (currentBalance == null || currentBalance <= 0)
                    throw new Exception("Loan not found or already has a zero balance.");

                // 1. Record in ManagedLoans (TransType 6 = write-off)
                const string insertManagedSql = """
                    INSERT INTO ManagedLoans
                        (LoanId, InitialOlb, EffectedAmount, NewOlb, DoneBy, DateDone, TransType, EntityId, Reason)
                    VALUES
                        (@LoanId, @Balance, @Balance, 0, @UserId, GETDATE(), 6, @EntityId, @Reason)
                    """;
                await conn.ExecuteAsync(insertManagedSql,
                    new { LoanId = loanId, Balance = currentBalance.Value, UserId = userId, EntityId = orgId, request.Reason },
                    transaction);

                // 2. Zero out loan balance, mark as cleared
                await conn.ExecuteAsync("""
                    UPDATE Loans
                    SET LoanBalance = 0, LoanCleared = 6, ExpectedClearDate = CAST(GETDATE() AS DATE)
                    WHERE id = @LoanId AND EntityId = @OrgId
                    """,
                    new { LoanId = loanId, OrgId = orgId }, transaction);

                // 3. Zero out unpaid schedule items
                await conn.ExecuteAsync("""
                    UPDATE loanSchedule
                    SET amounttopay = 0, status = 6
                    WHERE Loanid = @LoanId AND amounttopay > 0 AND status = 0
                    """,
                    new { LoanId = loanId }, transaction);

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                if (transaction.Connection != null) await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<LoanBalanceDto?> GetLoanBalanceAsync(string entityId, string loanId)
        {
            if (!TryParseOrgId(entityId, out int orgId) || !TryParseLoanId(loanId, out int id))
                return null;

            const string sql = """
                SELECT CAST(l.id AS NVARCHAR) AS Id, l.LoanBalance AS LoanBalance
                FROM Loans l
                WHERE l.id = @LoanId AND l.EntityId = @OrgId
                """;

            await using var conn = new SqlConnection(ConnectionString);
            return await conn.QueryFirstOrDefaultAsync<LoanBalanceDto>(sql, new { LoanId = id, OrgId = orgId });
        }

        public async Task<List<DisbursedLoanDto>> GetDisbursedLoansAsync(string entityId, DateTime startDate, DateTime endDate)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return new List<DisbursedLoanDto>();

            var endExclusive = endDate.Date.AddDays(1);

            const string sql = """
                SELECT CAST(l.id AS NVARCHAR)                                   AS LoanId,
                       CAST(l.BorrowerId AS NVARCHAR)                           AS BorrowerId,
                       ISNULL(b.firstName + ' ' + b.otherName, '')              AS BorrowerName,
                       ISNULL(b.PhoneNumber, '')                                AS PhoneNumber,
                       ISNULL(l.LoanAmount, 0)                                  AS LoanAmount,
                       ISNULL(l.AmountToDisburse, 0)                            AS AmountToDisburse,
                       l.LoanDisbursmentDate                                    AS DisbursementDate,
                       ISNULL(p.ProductName, '')                                AS ProductName,
                       ISNULL(l.LoanBalance, 0)                                 AS CurrentBalance
                FROM Loans l
                LEFT JOIN Borrowers b ON b.ID  = l.BorrowerId
                LEFT JOIN Products  p ON p.ID  = l.ProductId
                WHERE l.EntityId            = @EntityId
                  AND l.LoanDisbursmentDate >= @StartDate
                  AND l.LoanDisbursmentDate <  @EndExclusive
                ORDER BY l.LoanDisbursmentDate DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            var rows = await conn.QueryAsync<DisbursedLoanDto>(sql,
                new { EntityId = orgId, StartDate = startDate.Date, EndExclusive = endExclusive });
            return rows.AsList();
        }

        public async Task<List<PaymentDto>> GetPaymentsAsync(string entityId, DateTime startDate, DateTime endDate)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return new List<PaymentDto>();

            var endExclusive = endDate.Date.AddDays(1);

            const string sql = """
                SELECT p.ID                                                                 AS Id,
                       ISNULL(p.TransID, '')                                               AS TransId,
                       ISNULL(p.TransAmount, 0)                                            AS TransAmount,
                       ISNULL(p.BillRefNumber, '')                                         AS BillRefNumber,
                       LTRIM(RTRIM(ISNULL(p.FirstName, '') + ' ' +
                             ISNULL(p.MiddleName, '') + ' ' +
                             ISNULL(p.LastName, '')))                                      AS PayerName,
                       p.DateDone,
                       ISNULL(p.isPosted, 0)                                               AS IsPosted,
                       ISNULL(p.TransactionType, '')                                       AS TransactionType,
                       p.LoanId
                FROM Transactions.dbo.Payments p
                WHERE p.EntityId  = @EntityId
                  AND p.DateDone >= @StartDate
                  AND p.DateDone <  @EndExclusive
                ORDER BY p.DateDone DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            var rows = await conn.QueryAsync<PaymentDto>(sql,
                new { EntityId = orgId, StartDate = startDate.Date, EndExclusive = endExclusive });
            return rows.AsList();
        }

        public async Task<BorrowerDto?> GetBorrowerAsync(string entityId, string search)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return null;

            const string sql = """
                SELECT TOP 1
                    CAST(b.ID AS NVARCHAR)   AS BorrowerId,
                    ISNULL(b.firstName, '')  AS FirstName,
                    ISNULL(b.otherName, '')  AS OtherName,
                    ISNULL(b.NationalID, '') AS NationalID,
                    ISNULL(b.PhoneNumber, '') AS PhoneNumber,
                    ISNULL(b.EmailAddress, '') AS EmailAddress,
                    ISNULL(b.AccountNo, '')  AS AccountNo,
                    ISNULL(b.AccountStatus, 0) AS AccountStatus
                FROM Borrowers b
                WHERE b.EntityId = @EntityId
                  AND (b.PhoneNumber = @Search
                    OR b.NationalID  = @Search
                    OR CAST(b.ID AS NVARCHAR) = @Search)
                """;

            await using var conn = new SqlConnection(ConnectionString);
            return await conn.QueryFirstOrDefaultAsync<BorrowerDto>(sql, new { EntityId = orgId, Search = search });
        }

        public async Task<LoanResponse> GetOverdueLoansAsync(string entityId, int minDays, int top)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return new LoanResponse { Count = 0, Data = new List<LoanDto>() };

            var loans = new List<LoanDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = $"""
                SELECT TOP (@Top)
                {LoanColumns}
                {LoanJoins}
                WHERE l.LoanBalance > 0
                  AND ls.amounttopay > 0
                  AND ls.ExpectedDueDate < CAST(GETDATE() AS DATE)
                  AND l.EntityId = @EntityId
                {LoanGroupBy}
                HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) >= @MinDays
                ORDER BY DaysInArrears DESC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Top", top);
            cmd.Parameters.AddWithValue("@EntityId", orgId);
            cmd.Parameters.AddWithValue("@MinDays", minDays);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                loans.Add(MapToLoanDto(reader));

            return new LoanResponse { Count = loans.Count, Data = loans };
        }

        public async Task<BorrowerStatementDto?> GetBorrowerStatementAsync(string entityId, string search)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return null;

            // Resolve borrower by phone, national ID, or borrower ID
            const string borrowerSql = """
                SELECT TOP 1
                    CAST(b.ID AS NVARCHAR)        AS BorrowerId,
                    ISNULL(b.firstName, '')        AS FirstName,
                    ISNULL(b.otherName, '')        AS OtherName,
                    ISNULL(b.PhoneNumber, '')      AS PhoneNumber
                FROM Borrowers b
                WHERE b.EntityId = @EntityId
                  AND (b.PhoneNumber = @Search
                    OR b.NationalID  = @Search
                    OR CAST(b.ID AS NVARCHAR) = @Search)
                """;

            await using var conn = new SqlConnection(ConnectionString);
            var borrower = await conn.QueryFirstOrDefaultAsync(borrowerSql, new { EntityId = orgId, Search = search });

            if (borrower == null) return null;

            string borrowerId = borrower.BorrowerId;

            const string statementSql = """
                SELECT cs.id                                    AS Id,
                       ISNULL(CAST(cs.LoanId AS NVARCHAR), '') AS LoanId,
                       ISNULL(cs.Amount, 0)                    AS Amount,
                       ISNULL(cs.TransType, 0)                 AS TransType,
                       ISNULL(cs.Narration, '')                AS Narration,
                       ISNULL(cs.MpesaRef, '')                 AS MpesaRef,
                       ISNULL(cs.LoanBalance, 0)               AS LoanBalance,
                       ISNULL(cs.AccountBalance, 0)            AS AccountBalance,
                       cs.TransactedDate
                FROM customerstatement cs
                WHERE cs.UserId = @BorrowerId
                ORDER BY cs.TransactedDate, cs.id
                """;

            var lines = (await conn.QueryAsync<BorrowerStatementLineDto>(statementSql, new { BorrowerId = borrowerId })).AsList();

            return new BorrowerStatementDto
            {
                BorrowerId = borrowerId,
                BorrowerName = $"{borrower.FirstName} {borrower.OtherName}".Trim(),
                PhoneNumber = borrower.PhoneNumber,
                TotalLines = lines.Count,
                Statement = lines
            };
        }

        public async Task<LoanResponse> GetBorrowerLoansAsync(string entityId, string search)
        {
            if (!TryParseOrgId(entityId, out int orgId))
                return new LoanResponse { Count = 0, Data = new List<LoanDto>() };

            var loans = new List<LoanDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = $"""
                SELECT {LoanColumns}
                {LoanJoins}
                WHERE l.EntityId = @EntityId
                  AND (b.PhoneNumber = @Search
                    OR b.NationalID  = @Search
                    OR CAST(l.BorrowerId AS NVARCHAR) = @Search)
                {LoanGroupBy}
                ORDER BY l.id DESC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@EntityId", orgId);
            cmd.Parameters.AddWithValue("@Search", search);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                loans.Add(MapToLoanDto(reader));

            return new LoanResponse { Count = loans.Count, Data = loans };
        }

        // ─── Mapping ─────────────────────────────────────────────────────────────

        private static LoanDto MapToLoanDto(SqlDataReader reader) => new()
        {
            Id = reader["id"]?.ToString() ?? "",
            BorrowerId = reader["BorrowerId"]?.ToString() ?? "",
            FirstName = reader["firstName"]?.ToString() ?? "",
            OtherName = reader["otherName"]?.ToString() ?? "",
            PhoneNumber = reader["PhoneNumber"]?.ToString() ?? "",
            EmailAddress = reader["EmailAddress"]?.ToString() ?? "",
            NationalId = reader["NationalID"]?.ToString() ?? "",
            AmountToDisburse = Convert.ToDecimal(reader["AmountToDisburse"] == DBNull.Value ? 0 : reader["AmountToDisburse"]),
            RepaymentPeriod = reader["repaymentperiod"]?.ToString() ?? "",
            Arrears = Convert.ToDecimal(reader["Arrears"] == DBNull.Value ? 0 : reader["Arrears"]),
            DaysInArrears = Convert.ToInt32(reader["DaysInArrears"] == DBNull.Value ? 0 : reader["DaysInArrears"]),
            LoanBalance = Convert.ToDecimal(reader["LoanBalance"] == DBNull.Value ? 0 : reader["LoanBalance"]),
            Branch = reader["Branch"]?.ToString() ?? "",
            OutsourcedAmount = Convert.ToDecimal(reader["OutsourcedAmount"] == DBNull.Value ? 0 : reader["OutsourcedAmount"]),
            Penalty = Convert.ToDecimal(reader["Penalty"] == DBNull.Value ? 0 : reader["Penalty"]),
            ProductName = reader["ProductName"]?.ToString() ?? "",
            Agent = reader["Agent"]?.ToString() ?? "",
            AgentId = reader["AgentId"]?.ToString() ?? ""
        };
    }
}
