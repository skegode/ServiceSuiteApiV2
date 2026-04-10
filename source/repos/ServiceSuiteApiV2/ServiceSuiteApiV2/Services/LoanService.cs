using System.Text;
using System.Data;
using Microsoft.Data.SqlClient;
using ServiceSuiteApiV2.Models;
using ServiceSuiteApiV2.Controllers;
using Dapper;

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

        public async Task<LoanResponse> GetLoansAsync(LoanFilterRequest filter)
        {
            var loans = new List<LoanDto>();

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

        
            var sql = new StringBuilder("""
        SELECT
            l.id,
            b.Id As BorrowerId,
            b.firstName,
            b.otherName,
            b.PhoneNumber,
            b.EmailAddress,
            b.NationalID,
            l.AmountToDisburse,
            p.repaymentperiod,
            l.LoanBalance,
            l.Penalty,
            p.ProductName,
            o.UnitTitle                                       AS Branch,
            u.FirstName + ' ' + u.OtherName                   AS Agent,
            u.ID                                              AS AgentId,
          SUM(CASE 
           WHEN CAST(ls.ExpectedDueDate AS DATE) < CAST(GETDATE() AS DATE) 
           THEN ls.amounttopay 
           ELSE 0 
        END) AS Arrears,
            DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) AS DaysInArrears,
            SUM(ls.amounttopay)   AS OutsourcedAmount
        FROM loanSchedule ls
        INNER JOIN Loans l               ON l.id       = ls.loanid
        INNER JOIN Borrowers b            ON b.id       = l.borrowerid
        INNER JOIN usermaster u          ON u.id       = b.entityagent
        INNER JOIN organizationunits o   ON o.unitid   = b.entityunit
        INNER JOIN Products p            ON p.id       = l.productid
        WHERE l.LoanBalance > 0
        AND ls.amounttopay > 0 
        AND b.EntityId = @EntityId 
        """);

            await using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.AddWithValue("@EntityId", filter.EntityId);

            // 2. ADDITIONAL FILTERS (WHERE CLAUSE)
            if (filter.MinAmount.HasValue)
            {
                sql.Append(" AND l.AmountToDisburse >= @MinAmount ");
                cmd.Parameters.AddWithValue("@MinAmount", filter.MinAmount.Value);
            }
            if (filter.MaxAmount.HasValue)
            {
                sql.Append(" AND l.AmountToDisburse <= @MaxAmount ");
                cmd.Parameters.AddWithValue("@MaxAmount", filter.MaxAmount.Value);
            }
            if (filter.MinOlb.HasValue)
            {
                sql.Append(" AND l.LoanBalance >= @MinOlb ");
                cmd.Parameters.AddWithValue("@MinOlb", filter.MinOlb.Value);
            }
            if (filter.MaxOlb.HasValue)
            {
                sql.Append(" AND l.LoanBalance <= @MaxOlb ");
                cmd.Parameters.AddWithValue("@MaxOlb", filter.MaxOlb.Value);
            }

            // 3. GROUP BY (Added explicit leading space to fix the @EntityIdGROUP error)
            sql.Append("""
         GROUP BY
            l.id, b.id, b.firstName, b.otherName, b.PhoneNumber, b.EmailAddress, 
            b.NationalID, l.AmountToDisburse, p.repaymentperiod, l.LoanBalance, 
            o.UnitTitle, l.Penalty, p.ProductName, u.FirstName, u.OtherName, u.ID 
        """);

            // 4. RANGE FILTER (HAVING CLAUSE)
            // This looks at the OLDEST unpaid installment to determine the category.
            if (filter.MinDays.HasValue && filter.MaxDays.HasValue)
            {
                sql.Append(" HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) BETWEEN @MinDays AND @MaxDays ");
                cmd.Parameters.AddWithValue("@MinDays", filter.MinDays.Value);
                cmd.Parameters.AddWithValue("@MaxDays", filter.MaxDays.Value);
            }
            else if (filter.MinDays.HasValue)
            {
                sql.Append(" HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) >= @MinDays ");
                cmd.Parameters.AddWithValue("@MinDays", filter.MinDays.Value);
            }
            else if (filter.MaxDays.HasValue)
            {
                sql.Append(" HAVING DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) <= @MaxDays ");
                cmd.Parameters.AddWithValue("@MaxDays", filter.MaxDays.Value);
            }

            cmd.CommandText = sql.ToString();

            // 5. EXECUTION
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                loans.Add(MapToLoanDto(reader));
            }

            return new LoanResponse { Count = loans.Count, Data = loans };
        }
        /// <summary>
        /// Fetches active loans (Balance > 0) with a search term, restricted by EntityId.
        /// </summary>
        public async Task<LoanResponse> GetActiveLoansAsync(string entityId, string? searchTerm, int top = 20)
        {
            var loans = new List<LoanDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = new StringBuilder(@"
        SELECT TOP (@Top)
            l.id, b.firstName, b.otherName, b.PhoneNumber, b.EmailAddress, b.NationalID,
            l.AmountToDisburse, p.repaymentperiod, l.LoanBalance, l.Penalty, p.ProductName,
            o.UnitTitle AS Branch, u.FirstName + ' ' + u.OtherName AS Agent, u.ID AS AgentId,
            SUM(ls.amounttopay) AS Arrears,
            DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) AS DaysInArrears,
            SUM(ls.amounttopay) AS OutsourcedAmount
        FROM loanSchedule ls
        INNER JOIN Loans l      ON l.id    = ls.loanid
        INNER JOIN Borrowers b  ON b.id    = l.borrowerid
        INNER JOIN usermaster u ON u.id    = b.entityagent
        INNER JOIN organizationunits o ON o.unitid = b.entityunit
        INNER JOIN Products p   ON p.id    = l.productid
        WHERE l.LoanBalance > 0
          AND b.EntityId = @EntityId");
        
    await using var cmd = new SqlCommand();
            cmd.Connection = conn;
            cmd.Parameters.AddWithValue("@Top", top);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Append with a leading space so it never touches the previous token
                sql.Append(" AND (b.PhoneNumber LIKE @Search OR b.firstName LIKE @Search OR CAST(l.id AS NVARCHAR) LIKE @Search)");
                cmd.Parameters.AddWithValue("@Search", $"%{searchTerm}%");
            }

            // ← leading space/newline before GROUP BY — this was the bug
            sql.Append(@"
        GROUP BY
            l.id, b.firstName, b.otherName, b.PhoneNumber, b.EmailAddress, b.NationalID,
            l.AmountToDisburse, p.repaymentperiod, l.LoanBalance, o.UnitTitle,
            l.Penalty, p.ProductName, u.FirstName, u.OtherName, u.ID");

            cmd.CommandText = sql.ToString();

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) loans.Add(MapToLoanDto(reader));

            return new LoanResponse { Count = loans.Count, Data = loans };
        }

        /// <summary>
        /// Fetches a specific loan by its ID, verified against the user's EntityId.
        /// </summary>
        public async Task<LoanDto?> GetLoanByIdAsync(string entityId, string loanId)
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            var sql = """
                SELECT 
                    l.id, b.firstName, b.otherName, b.PhoneNumber, b.EmailAddress, b.NationalID,
                    l.AmountToDisburse, p.repaymentperiod, l.LoanBalance, l.Penalty, p.ProductName,
                    o.UnitTitle AS Branch, u.FirstName + ' ' + u.OtherName AS Agent, u.ID AS AgentId,
                    SUM(ls.amounttopay) AS Arrears,
                    DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) AS DaysInArrears,
                    SUM(ls.amounttopay) AS OutsourcedAmount
                FROM loanSchedule ls
                INNER JOIN Loans l ON l.id = ls.loanid
                INNER JOIN Borrowers b ON b.id = l.borrowerid
                INNER JOIN usermaster u ON u.id = b.entityagent
                INNER JOIN organizationunits o ON o.unitid = b.entityunit
                INNER JOIN Products p ON p.id = l.productid
                WHERE l.id = @LoanId AND b.EntityId = @EntityId
                GROUP BY 
                    l.id, b.firstName, b.otherName, b.PhoneNumber, b.EmailAddress, b.NationalID,
                    l.AmountToDisburse, p.repaymentperiod, l.LoanBalance, o.UnitTitle, 
                    l.Penalty, p.ProductName, u.FirstName, u.OtherName, u.ID
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LoanId", loanId);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? MapToLoanDto(reader) : null;
        }

        /// <summary>
        /// Fetches custom form details for a loan, verified against EntityId.
        /// </summary>
        public async Task<List<LoanDetailDto>> GetLoanDetailsAsync(string entityId, string loanId)
        {
            var details = new List<LoanDetailDto>();
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = """
                SELECT LD.ID, LD.loanId, I.itemName, LD.stringValue AS ItemValue 
                FROM LoanDetails LD 
                INNER JOIN LoanFormItems I ON I.ID = LD.itemId 
                INNER JOIN Loans L ON L.id = LD.loanId
                INNER JOIN Borrowers B ON B.id = L.borrowerid
                WHERE LD.loanId = @LoanId AND B.EntityId = @EntityId
                ORDER BY LD.ID DESC
                """;

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@LoanId", loanId);
            cmd.Parameters.AddWithValue("@EntityId", entityId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                details.Add(new LoanDetailDto
                {
                    Id = Convert.ToInt32(reader["ID"]),
                    LoanId = reader["loanId"]?.ToString() ?? "",
                    ItemName = reader["itemName"]?.ToString() ?? "",
                    ItemValue = reader["ItemValue"] == DBNull.Value ? "" : reader["ItemValue"].ToString()!
                });
            }
            return details;
        }

        /// <summary>
        /// Manages (Write-off, Waiver, etc.) a loan using stored procedures within a transaction.
        /// </summary>
        public async Task<bool> ManageLoanAsync(LoanManagementRequest request)
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var transaction = conn.BeginTransaction();

            try
            {
                // 1. Log Management Request
                await using var cmd = new SqlCommand("[dbo].[sp_NewLoanManagement]", conn, transaction);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userid", request.UserId);
                cmd.Parameters.AddWithValue("@Value", request.Value);
                cmd.Parameters.AddWithValue("@ValueType", request.ValueType);
                cmd.Parameters.AddWithValue("@loanIds", request.LoanIds);
                cmd.Parameters.AddWithValue("@TransType", request.TransType);
                cmd.Parameters.AddWithValue("@Narration", (object?)request.Narration ?? DBNull.Value);

                SqlParameter outputIdParam = new SqlParameter("@TransId", SqlDbType.Int) { Direction = ParameterDirection.Output };
                cmd.Parameters.Add(outputIdParam);
                await cmd.ExecuteNonQueryAsync();

                int transId = (int)outputIdParam.Value;

                // 2. Approve Action
                await using var approveCmd = new SqlCommand("[dbo].[sp_NewLoanManagementApproval]", conn, transaction);
                approveCmd.CommandType = CommandType.StoredProcedure;
                approveCmd.Parameters.AddWithValue("@userid", request.UserId);
                approveCmd.Parameters.AddWithValue("@TransId", transId);
                approveCmd.Parameters.AddWithValue("@ActionType", request.ActionType);

                await using var reader = await approveCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    if (!reader.GetBoolean(reader.GetOrdinal("Success")))
                    {
                        await transaction.RollbackAsync();
                        throw new Exception(reader.GetString(reader.GetOrdinal("Message")));
                    }
                }
                await reader.CloseAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                if (transaction.Connection != null) await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> InitiateWriteoffAsync(WriteoffRequest request)
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var transaction = conn.BeginTransaction();

            try
            {
            
                var currentBalance = await conn.QueryFirstOrDefaultAsync<decimal>(
                    "SELECT loanbalance FROM Loans WHERE id = @LoanId AND EntityId = @EntityId",
                    new { request.LoanId, request.EntityId },
                    transaction);

                if (currentBalance <= 0)
                {
                    throw new Exception("This loan already has a zero balance or was not found.");
                }

             

                // 2. Insert into ManagedLoans
                const string insertManagedSql = """
            INSERT INTO ManagedLoans (
                LoanId, InitialOlb, EffectedAmount, NewOlb, DoneBy, 
                DateDone, TransType, isApproved, ApprovedBy, ApprovedDate, 
                Reason, EntityId
            )
            OUTPUT INSERTED.id
            VALUES (
                @LoanId, @Olb, @Olb, 0, @UserId, 
                GETDATE(), 6, 1, @UserId, GETDATE(), 
                @Reason, @EntityId
            );
            """;

                await using var cmdManaged = new SqlCommand(insertManagedSql, conn, transaction);
                cmdManaged.Parameters.AddWithValue("@LoanId", request.LoanId);
                cmdManaged.Parameters.AddWithValue("@Olb", currentBalance); // Use the DB balance for accuracy
                cmdManaged.Parameters.AddWithValue("@UserId", request.UserId);
                cmdManaged.Parameters.AddWithValue("@Reason", request.Reason);
                cmdManaged.Parameters.AddWithValue("@EntityId", request.EntityId);

                var transId = (int)await cmdManaged.ExecuteScalarAsync()!;

                // 3. Update Loans Table
                const string updateLoanSql = "UPDATE Loans SET LoanBalance = 0, LoanCleared = 6, DateCleared = GETDATE() WHERE id = @LoanId AND EntityId = @EntityId";
                await using var cmdLoan = new SqlCommand(updateLoanSql, conn, transaction);
                cmdLoan.Parameters.AddWithValue("@LoanId", request.LoanId);
                cmdLoan.Parameters.AddWithValue("@EntityId", request.EntityId);
                await cmdLoan.ExecuteNonQueryAsync();

                // 4. Update LoanSchedule Table
                const string updateScheduleSql = "UPDATE loanSchedule SET AMOUNTTOPAY = 0, STATUS = 6 WHERE Loanid = @LoanId AND amounttopay > 0 AND status = 0";
                await using var cmdSchedule = new SqlCommand(updateScheduleSql, conn, transaction);
                cmdSchedule.Parameters.AddWithValue("@LoanId", request.LoanId);
                await cmdSchedule.ExecuteNonQueryAsync();

                // 5. Insert into CustomerStatement
                const string insertStatementSql = """
            INSERT INTO CustomerStatement (
                UserId, LoanId, Amount, TransType, MpesaRef, 
                Narration, EntityId, LoanBalance, AccountBalance, TransactedDate
            )
            SELECT B.ID, L.id, @Olb, 1, @Ref, 
                   @Narration, L.EntityId, 0, 0, GETDATE()
            FROM Loans L
            INNER JOIN Borrowers B ON B.ID = L.BorrowerId
            WHERE L.id = @LoanId AND L.EntityId = @EntityId;
            """;

                await using var cmdStatement = new SqlCommand(insertStatementSql, conn, transaction);
                cmdStatement.Parameters.AddWithValue("@LoanId", request.LoanId);
                cmdStatement.Parameters.AddWithValue("@Olb", currentBalance);
                cmdStatement.Parameters.AddWithValue("@Ref", request.Ref ?? "WRITE-OFF");
                cmdStatement.Parameters.AddWithValue("@Narration", $"Write-off: {request.Reason}");
                cmdStatement.Parameters.AddWithValue("@EntityId", request.EntityId);

                await cmdStatement.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                if (transaction.Connection != null) await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<LoanBalanceDto?> GetLoanBalanceAsync(string entityId, string loanId)
        {
            const string sql = @"
        SELECT 
            ID, 
            loanbalance 
        FROM Loans 
        WHERE id = @loanId 
          AND EntityId = @entityId";

            using (var connection = new SqlConnection(ConnectionString))
            {
                return await connection.QueryFirstOrDefaultAsync<LoanBalanceDto>(sql, new
                {
                    loanId = loanId,
                    entityId = entityId
                });
            }
        }

        private LoanDto MapToLoanDto(SqlDataReader reader) => new LoanDto
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