using Dapper;
using Microsoft.Data.SqlClient;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2.Services
{
    public class FraudService : IFraudService
    {
        private readonly IConfiguration _config;
        private string ConnectionString => _config.GetConnectionString("DefaultConnection")!;

        public FraudService(IConfiguration config) => _config = config;

        private static bool TryParseOrgId(string entityId, out int orgId) =>
            int.TryParse(entityId, out orgId);

        // ─── Full Report (all signals in parallel) ───────────────────────────────

        public async Task<FraudAnalyticsReport> GetFraudReportAsync(string entityId)
        {
            var agentTask = GetAgentFraudSignalsAsync(entityId);
            var shopTask  = GetShopFraudSignalsAsync(entityId);
            var dupTask   = GetDuplicateIdentitiesAsync(entityId);
            var stackTask = GetLoanStackingAsync(entityId);
            var suspTask  = GetSuspiciousLoansAsync(entityId);
            var payTask   = GetPaymentFraudAsync(entityId);

            await Task.WhenAll(agentTask, shopTask, dupTask, stackTask, suspTask, payTask);

            var agents    = agentTask.Result;
            var shops     = shopTask.Result;
            var dups      = dupTask.Result;
            var stacking  = stackTask.Result;
            var suspicious = suspTask.Result;
            var payments  = payTask.Result;

            var fraudExposure = dups.Sum(d => d.TotalActiveBalance)
                              + stacking.Sum(s => s.TotalBalance)
                              + suspicious.Where(l => l.PaymentCount == 0).Sum(l => l.LoanBalance);

            return new FraudAnalyticsReport
            {
                Summary = new FraudSummary
                {
                    HighRiskAgentCount      = agents.Count(a => a.RiskLevel == "High"),
                    HighRiskShopCount       = shops.Count(s => s.RiskLevel == "High"),
                    DuplicateIdentityCount  = dups.Count,
                    LoanStackingCount       = stacking.Count,
                    ZeroPaymentLoanCount    = suspicious.Count(l => l.PaymentCount == 0),
                    UnpostedPaymentCount    = payments.UnpostedCount,
                    UnpostedPaymentAmount   = payments.UnpostedAmount,
                    EstimatedFraudExposure  = fraudExposure,
                    GeneratedAt             = DateTime.UtcNow
                },
                AgentSignals      = agents,
                ShopSignals       = shops,
                DuplicateIdentities = dups,
                LoanStacking      = stacking,
                SuspiciousLoans   = suspicious,
                PaymentFraud      = payments
            };
        }

        // ─── Agent / Onboarder Fraud ─────────────────────────────────────────────
        // Detects: high default rates, write-off abuse, agents whose portfolios are
        // mostly in arrears — signals an agent is approving fictitious or colluding loans.

        public async Task<List<AgentFraudSignal>> GetAgentFraudSignalsAsync(string entityId)
        {
            if (!TryParseOrgId(entityId, out int orgId)) return new();

            const string sql = """
                WITH AgentLoans AS (
                    SELECT l.collectionAgentID,
                           l.id                AS LoanId,
                           l.LoanBalance,
                           l.AmountToDisburse,
                           SUM(CASE WHEN ls.ExpectedDueDate < CAST(GETDATE() AS DATE)
                                         AND ls.amounttopay > 0
                                    THEN ls.amounttopay ELSE 0 END) AS LoanArrears
                    FROM Loans l
                    LEFT JOIN loanSchedule ls ON ls.Loanid = l.id
                    WHERE l.EntityId = @EntityId
                    GROUP BY l.collectionAgentID, l.id, l.LoanBalance, l.AmountToDisburse
                ),
                AgentStats AS (
                    SELECT al.collectionAgentID,
                           COUNT(al.LoanId)                                    AS TotalLoans,
                           COUNT(CASE WHEN al.LoanBalance > 0 THEN 1 END)      AS ActiveLoans,
                           COUNT(CASE WHEN al.LoanArrears  > 0 THEN 1 END)     AS DefaultedLoans,
                           SUM(al.LoanArrears)                                 AS TotalArrears,
                           SUM(ISNULL(al.AmountToDisburse, 0))                 AS TotalDisbursed
                    FROM AgentLoans al
                    GROUP BY al.collectionAgentID
                ),
                WriteoffStats AS (
                    SELECT ml.DoneBy,
                           COUNT(*)               AS WriteoffCount,
                           SUM(ml.EffectedAmount) AS TotalWrittenOff
                    FROM ManagedLoans ml
                    WHERE ml.EntityId = @EntityId AND ml.TransType = 6
                    GROUP BY ml.DoneBy
                )
                SELECT CAST(u.ID AS NVARCHAR)                                          AS AgentId,
                       ISNULL(u.FirstName + ' ' + ISNULL(u.OtherName, ''), 'Unknown') AS AgentName,
                       ast.TotalLoans,
                       ast.ActiveLoans,
                       ast.DefaultedLoans,
                       CASE WHEN ast.TotalLoans > 0
                            THEN CAST(ast.DefaultedLoans * 100.0 / ast.TotalLoans AS DECIMAL(5,2))
                            ELSE 0 END                                                 AS DefaultRate,
                       ast.TotalArrears,
                       ast.TotalDisbursed,
                       ISNULL(wof.WriteoffCount,   0)                                  AS WriteoffCount,
                       ISNULL(wof.TotalWrittenOff, 0)                                  AS TotalWrittenOff
                FROM AgentStats ast
                INNER JOIN UserMaster u ON u.ID = ast.collectionAgentID
                LEFT  JOIN WriteoffStats wof ON wof.DoneBy = ast.collectionAgentID
                ORDER BY ast.TotalArrears DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            var rows = await conn.QueryAsync<AgentFraudSignal>(sql, new { EntityId = orgId });

            return rows.Select(r =>
            {
                if (r.DefaultRate > 50)                              r.Flags.Add("HighDefaultRate");
                if (r.WriteoffCount >= 3)                            r.Flags.Add("WriteoffConcentration");
                if (r.TotalArrears > r.TotalDisbursed * 0.5m)       r.Flags.Add("HighArrearsRatio");
                if (r.DefaultedLoans > r.TotalLoans * 0.7)          r.Flags.Add("MajorityDefaulted");

                r.RiskLevel = r.DefaultRate > 50 || r.WriteoffCount >= 3 ? "High"
                            : r.DefaultRate > 25 || r.WriteoffCount >= 1 ? "Medium"
                            : "Low";
                return r;
            }).ToList();
        }

        // ─── Shop / Merchant Fraud ───────────────────────────────────────────────
        // Groups borrowers by their collection agent (shop operator).
        // A shop where the majority of onboarded borrowers are defaulting suggests the
        // agent is enrolling fake or colluding borrowers at that location.

        public async Task<List<ShopFraudSignal>> GetShopFraudSignalsAsync(string entityId)
        {
            if (!TryParseOrgId(entityId, out int orgId)) return new();

            const string sql = """
                WITH BorrowerStats AS (
                    SELECT l.collectionAgentID,
                           l.BorrowerId,
                           SUM(ISNULL(l.AmountToDisburse, 0)) AS TotalDisbursed,
                           SUM(CASE WHEN ls.ExpectedDueDate < CAST(GETDATE() AS DATE)
                                         AND ls.amounttopay > 0
                                    THEN ls.amounttopay ELSE 0 END) AS Arrears,
                           MAX(CASE WHEN ls.ExpectedDueDate < CAST(GETDATE() AS DATE)
                                         AND ls.amounttopay > 0
                                    THEN 1 ELSE 0 END)              AS IsDefaulted
                    FROM Loans l
                    LEFT JOIN loanSchedule ls ON ls.Loanid = l.id
                    WHERE l.EntityId = @EntityId AND l.LoanBalance > 0
                    GROUP BY l.collectionAgentID, l.BorrowerId
                )
                SELECT CAST(u.ID AS NVARCHAR)                                          AS AgentId,
                       ISNULL(u.FirstName + ' ' + ISNULL(u.OtherName,''), 'Unknown')  AS AgentName,
                       COUNT(DISTINCT bs.BorrowerId)                                   AS UniqueBorrowers,
                       SUM(bs.IsDefaulted)                                             AS DefaultedBorrowers,
                       CASE WHEN COUNT(bs.BorrowerId) > 0
                            THEN CAST(SUM(bs.IsDefaulted) * 100.0 / COUNT(bs.BorrowerId) AS DECIMAL(5,2))
                            ELSE 0 END                                                 AS BorrowerDefaultRate,
                       SUM(bs.TotalDisbursed)                                          AS TotalDisbursed,
                       SUM(bs.Arrears)                                                 AS TotalArrears,
                       CASE WHEN SUM(bs.TotalDisbursed) > 0
                            THEN CAST(SUM(bs.Arrears) * 100.0 / SUM(bs.TotalDisbursed) AS DECIMAL(5,2))
                            ELSE 0 END                                                 AS PortfolioAtRisk
                FROM BorrowerStats bs
                INNER JOIN UserMaster u ON u.ID = bs.collectionAgentID
                GROUP BY u.ID, u.FirstName, u.OtherName
                HAVING COUNT(DISTINCT bs.BorrowerId) >= 3
                ORDER BY BorrowerDefaultRate DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            var rows = await conn.QueryAsync<ShopFraudSignal>(sql, new { EntityId = orgId });

            return rows.Select(r =>
            {
                r.RiskLevel = r.BorrowerDefaultRate > 60 ? "High"
                            : r.BorrowerDefaultRate > 35 ? "Medium"
                            : "Low";
                return r;
            }).ToList();
        }

        // ─── Duplicate National IDs ──────────────────────────────────────────────
        // Same NationalID registered under multiple BorrowerIds = synthetic identity fraud.

        public async Task<List<DuplicateIdentitySignal>> GetDuplicateIdentitiesAsync(string entityId)
        {
            if (!TryParseOrgId(entityId, out int orgId)) return new();

            const string sql = """
                SELECT b.NationalID                                                             AS NationalId,
                       COUNT(DISTINCT b.ID)                                                     AS BorrowerCount,
                       STRING_AGG(CAST(b.ID AS NVARCHAR), ', ') WITHIN GROUP (ORDER BY b.ID)   AS BorrowerIds,
                       STRING_AGG(ISNULL(b.firstName,'') + ' ' + ISNULL(b.otherName,''), ' | ')
                           WITHIN GROUP (ORDER BY b.ID)                                         AS Names,
                       STRING_AGG(ISNULL(b.PhoneNumber,''), ', ') WITHIN GROUP (ORDER BY b.ID) AS PhoneNumbers,
                       ISNULL(SUM(l.LoanBalance), 0)                                            AS TotalActiveBalance
                FROM Borrowers b
                LEFT JOIN Loans l ON l.BorrowerId = b.ID
                                 AND l.LoanBalance > 0
                                 AND l.EntityId = @EntityId
                WHERE b.EntityId = @EntityId
                  AND ISNULL(b.NationalID, '') <> ''
                GROUP BY b.NationalID
                HAVING COUNT(DISTINCT b.ID) > 1
                ORDER BY COUNT(DISTINCT b.ID) DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            return (await conn.QueryAsync<DuplicateIdentitySignal>(sql, new { EntityId = orgId })).AsList();
        }

        // ─── Loan Stacking ───────────────────────────────────────────────────────
        // One borrower holds more than one active loan simultaneously.

        public async Task<List<LoanStackingSignal>> GetLoanStackingAsync(string entityId)
        {
            if (!TryParseOrgId(entityId, out int orgId)) return new();

            const string sql = """
                SELECT CAST(l.BorrowerId AS NVARCHAR)                              AS BorrowerId,
                       ISNULL(b.firstName + ' ' + ISNULL(b.otherName,''), '')      AS BorrowerName,
                       ISNULL(b.PhoneNumber,'')                                    AS PhoneNumber,
                       ISNULL(b.NationalID,'')                                     AS NationalId,
                       ISNULL(u.FirstName + ' ' + ISNULL(u.OtherName,''), '')      AS AgentName,
                       COUNT(l.id)                                                 AS ActiveLoanCount,
                       SUM(l.LoanBalance)                                          AS TotalBalance,
                       SUM(ISNULL(l.AmountToDisburse, 0))                          AS TotalDisbursed,
                       MIN(l.LoanDisbursmentDate)                                  AS FirstLoanDate,
                       MAX(l.LoanDisbursmentDate)                                  AS LatestLoanDate
                FROM Loans l
                INNER JOIN Borrowers b ON b.ID = l.BorrowerId
                LEFT  JOIN UserMaster u ON u.ID = l.collectionAgentID
                WHERE l.EntityId = @EntityId AND l.LoanBalance > 0
                GROUP BY l.BorrowerId, b.firstName, b.otherName, b.PhoneNumber,
                         b.NationalID, u.FirstName, u.OtherName
                HAVING COUNT(l.id) > 1
                ORDER BY COUNT(l.id) DESC, SUM(l.LoanBalance) DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            return (await conn.QueryAsync<LoanStackingSignal>(sql, new { EntityId = orgId })).AsList();
        }

        // ─── Suspicious Loans ────────────────────────────────────────────────────
        // Loans overdue by minDaysInArrears or more. Sorted by payment count ASC so
        // zero-payment loans (borrow-and-disappear fraud) surface first.

        public async Task<List<SuspiciousLoanSignal>> GetSuspiciousLoansAsync(string entityId, int minDaysInArrears = 30)
        {
            if (!TryParseOrgId(entityId, out int orgId)) return new();

            const string sql = """
                WITH LoanArrears AS (
                    SELECT ls.Loanid,
                           SUM(CASE WHEN ls.ExpectedDueDate < CAST(GETDATE() AS DATE)
                                         AND ls.amounttopay > 0
                                    THEN ls.amounttopay ELSE 0 END)         AS OverdueAmount,
                           DATEDIFF(DAY, MIN(ls.ExpectedDueDate), GETDATE()) AS DaysInArrears
                    FROM loanSchedule ls
                    GROUP BY ls.Loanid
                ),
                LoanPayments AS (
                    SELECT p.LoanId,
                           COUNT(*)         AS PaymentCount,
                           SUM(TransAmount) AS TotalPaid
                    FROM Transactions.dbo.Payments p
                    WHERE p.EntityId = @EntityId AND p.isPosted = 1
                    GROUP BY p.LoanId
                )
                SELECT CAST(l.id AS NVARCHAR)                                      AS LoanId,
                       CAST(l.BorrowerId AS NVARCHAR)                              AS BorrowerId,
                       ISNULL(b.firstName + ' ' + ISNULL(b.otherName,''), '')      AS BorrowerName,
                       ISNULL(b.PhoneNumber,'')                                    AS PhoneNumber,
                       ISNULL(b.NationalID,'')                                     AS NationalId,
                       CAST(ISNULL(u.ID, 0) AS NVARCHAR)                           AS AgentId,
                       ISNULL(u.FirstName + ' ' + ISNULL(u.OtherName,''), '')      AS AgentName,
                       ISNULL(l.AmountToDisburse, 0)                               AS AmountDisbursed,
                       l.LoanDisbursmentDate                                       AS DisbursementDate,
                       ISNULL(la.OverdueAmount, 0)                                 AS TotalArrears,
                       ISNULL(la.DaysInArrears, 0)                                 AS DaysInArrears,
                       l.LoanBalance,
                       ISNULL(lp.PaymentCount, 0)                                  AS PaymentCount,
                       ISNULL(lp.TotalPaid, 0)                                     AS TotalPaid
                FROM Loans l
                INNER JOIN Borrowers b ON b.ID = l.BorrowerId
                LEFT  JOIN UserMaster u ON u.ID = l.collectionAgentID
                INNER JOIN LoanArrears la ON la.Loanid = l.id
                LEFT  JOIN LoanPayments lp ON lp.LoanId = CAST(l.id AS NVARCHAR)
                WHERE l.EntityId = @EntityId
                  AND l.LoanBalance > 0
                  AND la.OverdueAmount > 0
                  AND la.DaysInArrears >= @MinDays
                ORDER BY ISNULL(lp.PaymentCount, 0) ASC, la.DaysInArrears DESC
                """;

            await using var conn = new SqlConnection(ConnectionString);
            return (await conn.QueryAsync<SuspiciousLoanSignal>(sql,
                new { EntityId = orgId, MinDays = minDaysInArrears })).AsList();
        }

        // ─── Unposted Payment Fraud ──────────────────────────────────────────────
        // Payments received but sitting unposted for >3 days = possible staff diversion.

        public async Task<PaymentFraudSignal> GetPaymentFraudAsync(string entityId)
        {
            if (!TryParseOrgId(entityId, out int orgId)) return new();

            const string sql = """
                SELECT COUNT(*)                    AS UnpostedCount,
                       ISNULL(SUM(TransAmount), 0) AS UnpostedAmount,
                       MIN(DateDone)               AS OldestUnposted,
                       MAX(DateDone)               AS NewestUnposted
                FROM Transactions.dbo.Payments
                WHERE EntityId = @EntityId
                  AND isPosted  = 0
                  AND DateDone < DATEADD(DAY, -3, GETDATE())
                """;

            await using var conn = new SqlConnection(ConnectionString);
            return await conn.QueryFirstOrDefaultAsync<PaymentFraudSignal>(sql, new { EntityId = orgId })
                   ?? new PaymentFraudSignal();
        }
    }
}
