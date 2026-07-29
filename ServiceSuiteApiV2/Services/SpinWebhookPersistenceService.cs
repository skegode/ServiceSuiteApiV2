using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using ServiceSuiteApiV2.Models;

namespace ServiceSuiteApiV2.Services
{
    public class SpinWebhookPersistenceService : ISpinWebhookPersistenceService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SpinWebhookPersistenceService> _logger;

        private string ConnectionString => _config.GetConnectionString("CallBoxConnection")!;

        public SpinWebhookPersistenceService(IConfiguration config, ILogger<SpinWebhookPersistenceService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task<int> SavePayloadAsync(SpinWebhookPayload payload, string rawBody, DateTime receivedAt)
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var payloadId = await conn.QuerySingleAsync<int>("sp_InsertSpinWebhookPayload", new
                {
                    payload.RemoteIdentifier,
                    payload.FileUniqueId,
                    payload.FileType,
                    payload.FileName,
                    payload.Password,
                    payload.Email,
                    payload.Phone,
                    payload.IdNumber,
                    payload.BankName,
                    payload.AccountNumber,
                    payload.Duration,
                    SourceTimestamp = payload.Timestamp,
                    payload.StateName,
                    JsonData = payload.JsonData?.GetRawText(),
                    RawBody = rawBody,
                    ReceivedAt = receivedAt
                }, transaction: tx, commandType: CommandType.StoredProcedure);

                if (payload.JsonData is JsonElement root && root.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        await SaveJsonDataAsync(conn, tx, payloadId, root);
                    }
                    catch (Exception ex)
                    {
                        // Statements vary in shape and completeness; the flat row + raw JSON above is already
                        // safely committed, so a failure normalizing a sub-section must not lose the webhook.
                        _logger.LogWarning(ex, "[SpinWebhook] Partial failure normalizing json_data for PayloadId={PayloadId}", payloadId);
                    }
                }

                await tx.CommitAsync();
                return payloadId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task NormalizeExistingPayloadAsync(int payloadId, JsonElement jsonData)
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                await SaveJsonDataAsync(conn, tx, payloadId, jsonData);
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        private static async Task SaveJsonDataAsync(SqlConnection conn, SqlTransaction tx, int payloadId, JsonElement root)
        {
            var document = GetObj(root, "document");
            if (document is JsonElement doc)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO dbo.SpinWebhookDocument (PayloadId, Name, Status, DocDate) VALUES (@PayloadId, @Name, @Status, @DocDate)",
                    new { PayloadId = payloadId, Name = GetStr(doc, "name"), Status = GetStr(doc, "status"), DocDate = GetStr(doc, "date") },
                    tx);
            }

            var lastData = GetObj(root, "last_data");
            var header = lastData is JsonElement ld ? GetObj(ld, "header") : null;
            if (header is JsonElement h)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookHeader
                        (PayloadId, TotalSendAmt, TotalReceivedAmt, TotalAgentDeposit, TotalAgentWithdrawal, TotalLipaNaMpesaPaybill, TotalLipaNaMpesaBuygoods,
                         TotalPaidIn, TotalPaidOut, TotalPaidInAverage, TotalPaidOutAverage, TotalOthers, MpesaBalance, OpeningBalance, GeneratedDate)
                    VALUES
                        (@PayloadId, @TotalSendAmt, @TotalReceivedAmt, @TotalAgentDeposit, @TotalAgentWithdrawal, @TotalLipaNaMpesaPaybill, @TotalLipaNaMpesaBuygoods,
                         @TotalPaidIn, @TotalPaidOut, @TotalPaidInAverage, @TotalPaidOutAverage, @TotalOthers, @MpesaBalance, @OpeningBalance, @GeneratedDate)
                    """, new
                {
                    PayloadId = payloadId,
                    TotalSendAmt = GetDecimal(h, "total_send_amt"),
                    TotalReceivedAmt = GetDecimal(h, "total_received_amt"),
                    TotalAgentDeposit = GetDecimal(h, "total_agent_deposit"),
                    TotalAgentWithdrawal = GetDecimal(h, "total_agent_withdrawal"),
                    TotalLipaNaMpesaPaybill = GetDecimal(h, "total_lipa_na_mpesa_paybill"),
                    TotalLipaNaMpesaBuygoods = GetDecimal(h, "total_lipa_na_mpesa_buygoods"),
                    TotalPaidIn = GetDecimal(h, "total_paid_in"),
                    TotalPaidOut = GetDecimal(h, "total_paid_out"),
                    TotalPaidInAverage = GetDecimal(h, "total_paid_in_average"),
                    TotalPaidOutAverage = GetDecimal(h, "total_paid_out_average"),
                    TotalOthers = GetDecimal(h, "total_others"),
                    MpesaBalance = GetDecimal(h, "mpesa_balance"),
                    OpeningBalance = GetDecimal(h, "opening_balance"),
                    GeneratedDate = GetStr(h, "generated_date")
                }, tx);
            }

            var body = lastData is JsonElement ld2 ? GetObj(ld2, "body") : null;
            if (body is not JsonElement b) return;

            var kplc = GetObj(b, "kplc_account");
            if (kplc is JsonElement k)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO dbo.SpinWebhookKplcAccount (PayloadId, Name, PhoneNumber, AccountNumber) VALUES (@PayloadId, @Name, @PhoneNumber, @AccountNumber)",
                    new { PayloadId = payloadId, Name = GetStr(k, "name"), PhoneNumber = GetStr(k, "phone_number"), AccountNumber = GetStr(k, "account_number") },
                    tx);
            }

            // information / boundaries / scoring_payload / peak_inflow_dates are siblings of "body" under
            // "last_data" in the vendor payload, not children of "body" itself.
            var information = GetObj(lastData, "information");
            if (information is JsonElement info)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookInformation
                        (PayloadId, CustomerNames, IdentityNumber, Email, PhoneNumber, DateOfStatement, StatementPeriod, VehicleRegNo, HighestLocation, CustomerBankAccount)
                    VALUES
                        (@PayloadId, @CustomerNames, @IdentityNumber, @Email, @PhoneNumber, @DateOfStatement, @StatementPeriod, @VehicleRegNo, @HighestLocation, @CustomerBankAccount)
                    """, new
                {
                    PayloadId = payloadId,
                    CustomerNames = GetStr(info, "customer_names"),
                    IdentityNumber = GetStr(info, "identity_number"),
                    Email = GetStr(info, "email"),
                    PhoneNumber = GetStr(info, "phone_number"),
                    DateOfStatement = GetStr(info, "date_of_statement"),
                    StatementPeriod = GetStr(info, "statement_period"),
                    VehicleRegNo = GetStr(b, "vehicle_reg_no"),
                    HighestLocation = GetStr(b, "highest_location"),
                    CustomerBankAccount = GetStr(b, "customer_bank_account")
                }, tx);
            }

            var boundaries = GetObj(lastData, "boundaries");
            if (boundaries is JsonElement bd)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookBoundaries (PayloadId, SubmissionAgeInDays, ProcessedOnAgeInDays, ReceivedOn, DurationInMonths)
                    VALUES (@PayloadId, @SubmissionAgeInDays, @ProcessedOnAgeInDays, @ReceivedOn, @DurationInMonths)
                    """, new
                {
                    PayloadId = payloadId,
                    SubmissionAgeInDays = GetInt(bd, "submission_age_in_days"),
                    ProcessedOnAgeInDays = GetInt(bd, "processed_on_age_in_days"),
                    ReceivedOn = GetStr(bd, "received_on"),
                    DurationInMonths = GetInt(bd, "duration_in_months")
                }, tx);
            }

            var loansData = GetObj(b, "loans_data");
            if (loansData is JsonElement ln)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookLoansData
                        (PayloadId, TotalLoanDisbursements, TotalLoanRepayments, HighestSafLoan, HighestSafLoanDesc, HighestOtherLoan, HighestOtherLoanDesc)
                    VALUES
                        (@PayloadId, @TotalLoanDisbursements, @TotalLoanRepayments, @HighestSafLoan, @HighestSafLoanDesc, @HighestOtherLoan, @HighestOtherLoanDesc)
                    """, new
                {
                    PayloadId = payloadId,
                    TotalLoanDisbursements = GetDecimal(ln, "total_loan_disbursements"),
                    TotalLoanRepayments = GetDecimal(ln, "total_loan_repayments"),
                    HighestSafLoan = GetDecimal(ln, "highest_saf_loan"),
                    HighestSafLoanDesc = GetStr(ln, "highest_saf_loan_desc"),
                    HighestOtherLoan = GetDecimal(ln, "highest_other_loan"),
                    HighestOtherLoanDesc = GetStr(ln, "highest_other_loan_desc")
                }, tx);
            }

            await SaveLocationsAsync(conn, tx, payloadId, GetArr(b, "locations"));
            await SaveCustomerDirectionAsync(conn, tx, payloadId, "Sent", GetObj(GetObj(b, "customers"), "sent"));
            await SaveCustomerDirectionAsync(conn, tx, payloadId, "Received", GetObj(GetObj(b, "customers"), "received"));
            await SaveProductStatsAsync(conn, tx, payloadId, "Airtime", GetObj(b, "airtime"));
            await SaveProductStatsAsync(conn, tx, payloadId, "InternetBundles", GetObj(b, "internet_bundles"));

            var fuliza = GetObj(b, "fuliza");
            if (fuliza is JsonElement fz)
            {
                var received = GetObj(fz, "received");
                var paid = GetObj(fz, "paid");
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookFuliza
                        (PayloadId, ReceivedCount, ReceivedHighest, ReceivedLowest, ReceivedTotal, ReceivedLast, PaidCount, PaidHighest, PaidLowest, PaidTotal, PaidLast)
                    VALUES
                        (@PayloadId, @ReceivedCount, @ReceivedHighest, @ReceivedLowest, @ReceivedTotal, @ReceivedLast, @PaidCount, @PaidHighest, @PaidLowest, @PaidTotal, @PaidLast)
                    """, new
                {
                    PayloadId = payloadId,
                    ReceivedCount = received is JsonElement r ? GetInt(r, "count") : null,
                    ReceivedHighest = received is JsonElement r1 ? GetDecimal(r1, "highest") : null,
                    ReceivedLowest = received is JsonElement r2 ? GetDecimal(r2, "lowest") : null,
                    ReceivedTotal = received is JsonElement r3 ? GetDecimal(r3, "total") : null,
                    ReceivedLast = received is JsonElement r4 ? GetDecimal(r4, "last") : null,
                    PaidCount = paid is JsonElement p ? GetInt(p, "count") : null,
                    PaidHighest = paid is JsonElement p1 ? GetDecimal(p1, "highest") : null,
                    PaidLowest = paid is JsonElement p2 ? GetDecimal(p2, "lowest") : null,
                    PaidTotal = paid is JsonElement p3 ? GetDecimal(p3, "total") : null,
                    PaidLast = paid is JsonElement p4 ? GetDecimal(p4, "last") : null
                }, tx);
            }

            var smallBiz = GetObj(b, "small_business");
            if (smallBiz is JsonElement sb)
            {
                var received = GetObj(sb, "received");
                var paid = GetObj(sb, "paid");
                var transfer = GetObj(sb, "transfer");
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookSmallBusiness
                        (PayloadId, ReceivedCount, ReceivedHighest, ReceivedLowest, ReceivedTotal, ReceivedLast,
                         PaidCount, PaidHighest, PaidLowest, PaidTotal, PaidLast,
                         TransferCount, TransferHighest, TransferLowest, TransferTotal, TransferLast)
                    VALUES
                        (@PayloadId, @ReceivedCount, @ReceivedHighest, @ReceivedLowest, @ReceivedTotal, @ReceivedLast,
                         @PaidCount, @PaidHighest, @PaidLowest, @PaidTotal, @PaidLast,
                         @TransferCount, @TransferHighest, @TransferLowest, @TransferTotal, @TransferLast)
                    """, new
                {
                    PayloadId = payloadId,
                    ReceivedCount = received is JsonElement r ? GetInt(r, "count") : null,
                    ReceivedHighest = received is JsonElement r1 ? GetDecimal(r1, "highest") : null,
                    ReceivedLowest = received is JsonElement r2 ? GetDecimal(r2, "lowest") : null,
                    ReceivedTotal = received is JsonElement r3 ? GetDecimal(r3, "total") : null,
                    ReceivedLast = received is JsonElement r4 ? GetDecimal(r4, "last") : null,
                    PaidCount = paid is JsonElement p ? GetInt(p, "count") : null,
                    PaidHighest = paid is JsonElement p1 ? GetDecimal(p1, "highest") : null,
                    PaidLowest = paid is JsonElement p2 ? GetDecimal(p2, "lowest") : null,
                    PaidTotal = paid is JsonElement p3 ? GetDecimal(p3, "total") : null,
                    PaidLast = paid is JsonElement p4 ? GetDecimal(p4, "last") : null,
                    TransferCount = transfer is JsonElement t ? GetInt(t, "count") : null,
                    TransferHighest = transfer is JsonElement t1 ? GetDecimal(t1, "highest") : null,
                    TransferLowest = transfer is JsonElement t2 ? GetDecimal(t2, "lowest") : null,
                    TransferTotal = transfer is JsonElement t3 ? GetDecimal(t3, "total") : null,
                    TransferLast = transfer is JsonElement t4 ? GetDecimal(t4, "last") : null
                }, tx);
            }

            var otherProducts = GetObj(b, "other_products");
            if (otherProducts is JsonElement op)
            {
                var micro = GetObj(GetObj(op, "micro_sme_business"), "paid");
                var loanSoko = GetObj(GetObj(op, "loan_soko"), "repayment");
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookOtherProducts
                        (PayloadId, MicroSmeBusinessPaidCount, MicroSmeBusinessPaidHighest, MicroSmeBusinessPaidLowest, MicroSmeBusinessPaidTotal, MicroSmeBusinessPaidLast,
                         LoanSokoRepaymentCount, LoanSokoRepaymentHighest, LoanSokoRepaymentLowest, LoanSokoRepaymentTotal, LoanSokoRepaymentLast)
                    VALUES
                        (@PayloadId, @MicroSmeBusinessPaidCount, @MicroSmeBusinessPaidHighest, @MicroSmeBusinessPaidLowest, @MicroSmeBusinessPaidTotal, @MicroSmeBusinessPaidLast,
                         @LoanSokoRepaymentCount, @LoanSokoRepaymentHighest, @LoanSokoRepaymentLowest, @LoanSokoRepaymentTotal, @LoanSokoRepaymentLast)
                    """, new
                {
                    PayloadId = payloadId,
                    MicroSmeBusinessPaidCount = micro is JsonElement m ? GetInt(m, "count") : null,
                    MicroSmeBusinessPaidHighest = micro is JsonElement m1 ? GetDecimal(m1, "highest") : null,
                    MicroSmeBusinessPaidLowest = micro is JsonElement m2 ? GetDecimal(m2, "lowest") : null,
                    MicroSmeBusinessPaidTotal = micro is JsonElement m3 ? GetDecimal(m3, "total") : null,
                    MicroSmeBusinessPaidLast = micro is JsonElement m4 ? GetDecimal(m4, "last") : null,
                    LoanSokoRepaymentCount = loanSoko is JsonElement l ? GetInt(l, "count") : null,
                    LoanSokoRepaymentHighest = loanSoko is JsonElement l1 ? GetDecimal(l1, "highest") : null,
                    LoanSokoRepaymentLowest = loanSoko is JsonElement l2 ? GetDecimal(l2, "lowest") : null,
                    LoanSokoRepaymentTotal = loanSoko is JsonElement l3 ? GetDecimal(l3, "total") : null,
                    LoanSokoRepaymentLast = loanSoko is JsonElement l4 ? GetDecimal(l4, "last") : null
                }, tx);
            }

            await SaveAgentAsync(conn, tx, payloadId, "Withdraw", GetObj(GetObj(b, "agent"), "withdraw"));
            await SaveAgentAsync(conn, tx, payloadId, "Deposit", GetObj(GetObj(b, "agent"), "deposit"));

            var fees = GetObj(b, "fees");
            if (fees is JsonElement fe)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookFees (PayloadId, Withdraw, CustomerTransfer, Paybill, Merchant, UtilityReversal, UnregisteredUser)
                    VALUES (@PayloadId, @Withdraw, @CustomerTransfer, @Paybill, @Merchant, @UtilityReversal, @UnregisteredUser)
                    """, new
                {
                    PayloadId = payloadId,
                    Withdraw = GetDecimal(fe, "withdraw"),
                    CustomerTransfer = GetDecimal(fe, "customer_transfer"),
                    Paybill = GetDecimal(fe, "paybill"),
                    Merchant = GetDecimal(fe, "merchant"),
                    UtilityReversal = GetDecimal(fe, "utility_reversal"),
                    UnregisteredUser = GetDecimal(fe, "unregistered_user")
                }, tx);
            }

            await SavePaybillDirectionAsync(conn, tx, payloadId, "Sent", GetObj(GetObj(b, "paybill"), "sent"));
            await SavePaybillDirectionAsync(conn, tx, payloadId, "Received", GetObj(GetObj(b, "paybill"), "received"));

            var buyGoods = GetObj(b, "buy_goods");
            if (buyGoods is JsonElement bg)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO dbo.SpinWebhookBuyGoods (PayloadId, Count, Highest, Lowest, Total) VALUES (@PayloadId, @Count, @Highest, @Lowest, @Total)",
                    new { PayloadId = payloadId, Count = GetInt(bg, "count"), Highest = GetDecimal(bg, "highest"), Lowest = GetDecimal(bg, "lowest"), Total = GetDecimal(bg, "total") },
                    tx);

                if (GetArr(bg, "top") is JsonElement topArr)
                {
                    foreach (var item in topArr.EnumerateArray())
                    {
                        await conn.ExecuteAsync("""
                            INSERT INTO dbo.SpinWebhookBuyGoodsTop (PayloadId, TillNo, Name, Count, Total, Highest)
                            VALUES (@PayloadId, @TillNo, @Name, @Count, @Total, @Highest)
                            """, new
                        {
                            PayloadId = payloadId,
                            TillNo = GetStr(item, "till_no"),
                            Name = GetStr(item, "name"),
                            Count = GetInt(item, "count"),
                            Total = GetDecimal(item, "total"),
                            Highest = GetDecimal(item, "highest")
                        }, tx);
                    }
                }
            }

            await SaveLoanProductAsync(conn, tx, payloadId, "KcbMpesa", GetObj(b, "kcb_mpesa"));
            await SaveLoanProductAsync(conn, tx, payloadId, "Mshwari", GetObj(b, "mshwari"));
            await SaveLoanProductAsync(conn, tx, payloadId, "HustlerFund", GetObj(b, "hustler_fund"));

            var intlRemit = GetObj(b, "international_remmitance");
            if (intlRemit is JsonElement ir)
            {
                await conn.ExecuteAsync(
                    "INSERT INTO dbo.SpinWebhookInternationalRemittance (PayloadId, Count, Highest, Total, LastAmount) VALUES (@PayloadId, @Count, @Highest, @Total, @LastAmount)",
                    new { PayloadId = payloadId, Count = GetInt(ir, "count"), Highest = GetDecimal(ir, "highest"), Total = GetDecimal(ir, "total"), LastAmount = GetDecimal(ir, "last_amount") },
                    tx);
            }

            if (GetArr(b, "final_classifications") is JsonElement classArr)
            {
                foreach (var c in classArr.EnumerateArray())
                {
                    var sent = GetObj(c, "sent");
                    var recv = GetObj(c, "received");
                    await conn.ExecuteAsync("""
                        INSERT INTO dbo.SpinWebhookClassifications
                            (PayloadId, CategoryName, SentTotal, SentCount, SentHighest, SentLowest, SentHighestWho, SentLowestWho, SentLastDraw,
                             ReceivedTotal, ReceivedCount, ReceivedHighest, ReceivedLowest, ReceivedHighestWho, ReceivedLowestWho, ReceivedLastDraw)
                        VALUES
                            (@PayloadId, @CategoryName, @SentTotal, @SentCount, @SentHighest, @SentLowest, @SentHighestWho, @SentLowestWho, @SentLastDraw,
                             @ReceivedTotal, @ReceivedCount, @ReceivedHighest, @ReceivedLowest, @ReceivedHighestWho, @ReceivedLowestWho, @ReceivedLastDraw)
                        """, new
                    {
                        PayloadId = payloadId,
                        CategoryName = GetStr(c, "name") ?? "Unknown",
                        SentTotal = sent is JsonElement s ? GetDecimal(s, "total") : null,
                        SentCount = sent is JsonElement s1 ? GetInt(s1, "count") : null,
                        SentHighest = sent is JsonElement s2 ? GetDecimal(s2, "highest") : null,
                        SentLowest = sent is JsonElement s3 ? GetDecimal(s3, "lowest") : null,
                        SentHighestWho = sent is JsonElement s4 ? GetStr(s4, "highest_who") : null,
                        SentLowestWho = sent is JsonElement s5 ? GetStr(s5, "lowest_who") : null,
                        SentLastDraw = sent is JsonElement s6 ? GetDecimal(s6, "last_draw") : null,
                        ReceivedTotal = recv is JsonElement rv ? GetDecimal(rv, "total") : null,
                        ReceivedCount = recv is JsonElement rv1 ? GetInt(rv1, "count") : null,
                        ReceivedHighest = recv is JsonElement rv2 ? GetDecimal(rv2, "highest") : null,
                        ReceivedLowest = recv is JsonElement rv3 ? GetDecimal(rv3, "lowest") : null,
                        ReceivedHighestWho = recv is JsonElement rv4 ? GetStr(rv4, "highest_who") : null,
                        ReceivedLowestWho = recv is JsonElement rv5 ? GetStr(rv5, "lowest_who") : null,
                        ReceivedLastDraw = recv is JsonElement rv6 ? GetDecimal(rv6, "last_draw") : null
                    }, tx);
                }
            }

            if (GetArr(b, "monthly_tabulated") is JsonElement monthArr)
            {
                foreach (var m in monthArr.EnumerateArray())
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO dbo.SpinWebhookMonthlySummary
                            (PayloadId, MonthLabel, Debits, DebitsCount, LoanRepayments, NetDebits, Credits, CreditsCount, LoanDisbursements, NetCredits, Closing)
                        VALUES
                            (@PayloadId, @MonthLabel, @Debits, @DebitsCount, @LoanRepayments, @NetDebits, @Credits, @CreditsCount, @LoanDisbursements, @NetCredits, @Closing)
                        """, new
                    {
                        PayloadId = payloadId,
                        MonthLabel = GetStr(m, "Month") ?? "Unknown",
                        Debits = GetDecimal(m, "Debits"),
                        DebitsCount = GetDecimal(m, "Debits_count"),
                        LoanRepayments = GetDecimal(m, "Loan_repayments"),
                        NetDebits = GetDecimal(m, "Net_debits"),
                        Credits = GetDecimal(m, "Credits"),
                        CreditsCount = GetDecimal(m, "Credits_count"),
                        LoanDisbursements = GetDecimal(m, "Loan_disbursements"),
                        NetCredits = GetDecimal(m, "Net_credits"),
                        Closing = GetDecimal(m, "Closing")
                    }, tx);
                }
            }

            await SaveFlowAsync(conn, tx, payloadId, "Inflow", GetArr(b, "income_flow"));
            await SaveFlowAsync(conn, tx, payloadId, "Outflow", GetArr(b, "expense_flow"));
            await SaveBreakdownAsync(conn, tx, payloadId, "Income", GetObj(b, "income"));
            await SaveBreakdownAsync(conn, tx, payloadId, "Expenditure", GetObj(b, "expenditure"));

            if (GetArr(b, "end_of_day_balances") is JsonElement eodArr)
            {
                foreach (var monthEntry in eodArr.EnumerateArray())
                {
                    var monthLabel = GetStr(monthEntry, "month") ?? "Unknown";
                    if (GetArr(monthEntry, "data") is JsonElement dayArr)
                    {
                        foreach (var dayEntry in dayArr.EnumerateArray())
                        {
                            await conn.ExecuteAsync("""
                                INSERT INTO dbo.SpinWebhookEndOfDayBalance (PayloadId, MonthLabel, DayLabel, ClosingBalance)
                                VALUES (@PayloadId, @MonthLabel, @DayLabel, @ClosingBalance)
                                """, new
                            {
                                PayloadId = payloadId,
                                MonthLabel = monthLabel,
                                DayLabel = GetStr(dayEntry, "day") ?? "?",
                                ClosingBalance = GetDecimal(dayEntry, "closing_balance")
                            }, tx);
                        }
                    }
                }
            }

            if (lastData is JsonElement ldPeak && GetArr(ldPeak, "peak_inflow_dates") is JsonElement peakArr)
            {
                foreach (var d in peakArr.EnumerateArray())
                {
                    if (d.ValueKind == JsonValueKind.Number && d.TryGetInt32(out var day))
                    {
                        await conn.ExecuteAsync(
                            "INSERT INTO dbo.SpinWebhookPeakInflowDate (PayloadId, DayOfMonth) VALUES (@PayloadId, @DayOfMonth)",
                            new { PayloadId = payloadId, DayOfMonth = day }, tx);
                    }
                }
            }

            var scoringPayload = GetObj(lastData, "scoring_payload");
            if (scoringPayload is JsonElement sp)
            {
                var impact = GetObj(sp, "positive_impact_transactions");
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookScoringPayload
                        (PayloadId, DocType, LoansRepaid, LoansReceived, StatementLength, AverageIncome, VolatilityScore, BettingToIncomeRatio, MedicalToIncomeRatio,
                         DebtToIncomeRatio, ExpenseToIncomeRatio, SavingsToIncomeRatio, LoansCount, GrossIncome, NetIncomeIncome, CreditHistory, MpesaCategories,
                         HighestSafLoan, HighestOtherLoan, Affordability, TransactionFrequency, ImpactInsurance, ImpactSaccos, ImpactMmf, ImpactFixedDeposit, ImpactBondsStocks)
                    VALUES
                        (@PayloadId, @DocType, @LoansRepaid, @LoansReceived, @StatementLength, @AverageIncome, @VolatilityScore, @BettingToIncomeRatio, @MedicalToIncomeRatio,
                         @DebtToIncomeRatio, @ExpenseToIncomeRatio, @SavingsToIncomeRatio, @LoansCount, @GrossIncome, @NetIncomeIncome, @CreditHistory, @MpesaCategories,
                         @HighestSafLoan, @HighestOtherLoan, @Affordability, @TransactionFrequency, @ImpactInsurance, @ImpactSaccos, @ImpactMmf, @ImpactFixedDeposit, @ImpactBondsStocks)
                    """, new
                {
                    PayloadId = payloadId,
                    DocType = GetStr(sp, "doc_type"),
                    LoansRepaid = GetInt(sp, "loans_repaid"),
                    LoansReceived = GetInt(sp, "loans_received"),
                    StatementLength = GetInt(sp, "statement_length"),
                    AverageIncome = GetDecimal(sp, "average_income"),
                    VolatilityScore = GetDecimal(sp, "volatility_score"),
                    BettingToIncomeRatio = GetDecimal(sp, "betting_to_income_ratio"),
                    MedicalToIncomeRatio = GetDecimal(sp, "medical_to_income_ratio"),
                    DebtToIncomeRatio = GetDecimal(sp, "debt_to_income_ratio"),
                    ExpenseToIncomeRatio = GetDecimal(sp, "expense_to_income_ratio"),
                    SavingsToIncomeRatio = GetDecimal(sp, "savings_to_income_ratio"),
                    LoansCount = GetInt(sp, "loans_count"),
                    GrossIncome = GetDecimal(sp, "gross_income"),
                    NetIncomeIncome = GetDecimal(sp, "net_income_income"),
                    CreditHistory = GetInt(sp, "credit_history"),
                    MpesaCategories = GetInt(sp, "mpesa_categories"),
                    HighestSafLoan = GetDecimal(sp, "highest_saf_loan"),
                    HighestOtherLoan = GetDecimal(sp, "highest_other_loan"),
                    Affordability = GetDecimal(sp, "affordability"),
                    TransactionFrequency = GetDecimal(sp, "transaction_frequency"),
                    ImpactInsurance = impact is JsonElement i1 ? GetBool(i1, "insurance") : null,
                    ImpactSaccos = impact is JsonElement i2 ? GetBool(i2, "saccos") : null,
                    ImpactMmf = impact is JsonElement i3 ? GetBool(i3, "mmf") : null,
                    ImpactFixedDeposit = impact is JsonElement i4 ? GetBool(i4, "fixed_deposit") : null,
                    ImpactBondsStocks = impact is JsonElement i5 ? GetBool(i5, "bonds_stocks") : null
                }, tx);

                var credits = GetArr(sp, "monthly_credits");
                var balances = GetArr(sp, "monthly_balances");
                var creditsList = credits?.EnumerateArray().ToList();
                var balancesList = balances?.EnumerateArray().ToList();
                var maxLen = Math.Max(creditsList?.Count ?? 0, balancesList?.Count ?? 0);
                for (int i = 0; i < maxLen; i++)
                {
                    decimal? credit = creditsList != null && i < creditsList.Count ? ToDecimal(creditsList[i]) : null;
                    decimal? balance = balancesList != null && i < balancesList.Count ? ToDecimal(balancesList[i]) : null;
                    await conn.ExecuteAsync(
                        "INSERT INTO dbo.SpinWebhookScoringMonthly (PayloadId, MonthIndex, MonthlyCredit, MonthlyBalance) VALUES (@PayloadId, @MonthIndex, @MonthlyCredit, @MonthlyBalance)",
                        new { PayloadId = payloadId, MonthIndex = i, MonthlyCredit = credit, MonthlyBalance = balance }, tx);
                }
            }

            // Top-level sections (outside last_data.body)
            var oldScores = GetObj(root, "old_scores");
            if (oldScores is JsonElement os)
            {
                var mScore = GetObj(os, "m_score");
                var gScore = GetObj(os, "g_score");
                var dScore = GetObj(os, "d_score");
                var dMobile = dScore is JsonElement d1 ? GetObj(d1, "mobile") : null;
                var dLongTerm = dScore is JsonElement d2 ? GetObj(d2, "long_term") : null;
                var dData = dScore is JsonElement d3 ? GetObj(d3, "data") : null;

                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookOldScores
                        (PayloadId, MScoreScore, MScoreLoanable, MScoreHighest, MScoreRiskLevel,
                         GScoreHighest, GScoreLoanable, GScoreLoanableHighest, GScoreRisk, GScoreNetLoanableHighest,
                         DScoreMobileNetScore, DScoreMobileGrossScore, DScoreMobileRiskLevel, DScoreMobileLoanable,
                         DScoreLongTermNetScore, DScoreLongTermGrossScore, DScoreLongTermRiskLevel,
                         DScoreLongTermNetLoanableHighest, DScoreLongTermLoanableHighest, DScoreLongTermLoanable, DScoreLongTermHighest,
                         DScoreDataIncomeMatrix, DScoreDataActivityLevelMatrix, DScoreDataImpactMatrix,
                         DScoreDataIndebtednessMatrix, DScoreDataSafBasedLoansMatrix, DScoreDataOtherLoansMatrix, DScoreDataLongTermOtherLoansMatrix,
                         DScoreDataHighBetting, DScoreDataHealthcareRatioHigh, DScoreDataLowActivity,
                         DScoreDataHistoryOfRepayingOtherMobileLoans, DScoreDataDebtToIncomeRatioHigh)
                    VALUES
                        (@PayloadId, @MScoreScore, @MScoreLoanable, @MScoreHighest, @MScoreRiskLevel,
                         @GScoreHighest, @GScoreLoanable, @GScoreLoanableHighest, @GScoreRisk, @GScoreNetLoanableHighest,
                         @DScoreMobileNetScore, @DScoreMobileGrossScore, @DScoreMobileRiskLevel, @DScoreMobileLoanable,
                         @DScoreLongTermNetScore, @DScoreLongTermGrossScore, @DScoreLongTermRiskLevel,
                         @DScoreLongTermNetLoanableHighest, @DScoreLongTermLoanableHighest, @DScoreLongTermLoanable, @DScoreLongTermHighest,
                         @DScoreDataIncomeMatrix, @DScoreDataActivityLevelMatrix, @DScoreDataImpactMatrix,
                         @DScoreDataIndebtednessMatrix, @DScoreDataSafBasedLoansMatrix, @DScoreDataOtherLoansMatrix, @DScoreDataLongTermOtherLoansMatrix,
                         @DScoreDataHighBetting, @DScoreDataHealthcareRatioHigh, @DScoreDataLowActivity,
                         @DScoreDataHistoryOfRepayingOtherMobileLoans, @DScoreDataDebtToIncomeRatioHigh)
                    """, new
                {
                    PayloadId = payloadId,
                    MScoreScore = mScore is JsonElement ms ? GetDecimal(ms, "score") : null,
                    MScoreLoanable = mScore is JsonElement ms1 ? GetDecimal(ms1, "loanable") : null,
                    MScoreHighest = mScore is JsonElement ms2 ? GetDecimal(ms2, "highest") : null,
                    MScoreRiskLevel = mScore is JsonElement ms3 ? GetStr(ms3, "risk_level") : null,
                    GScoreHighest = gScore is JsonElement gs ? GetDecimal(gs, "highest") : null,
                    GScoreLoanable = gScore is JsonElement gs1 ? GetDecimal(gs1, "loanable") : null,
                    GScoreLoanableHighest = gScore is JsonElement gs2 ? GetDecimal(gs2, "loanable_highest") : null,
                    GScoreRisk = gScore is JsonElement gs3 ? GetStr(gs3, "risk") : null,
                    GScoreNetLoanableHighest = gScore is JsonElement gs4 ? GetDecimal(gs4, "net_loanable_highest") : null,
                    DScoreMobileNetScore = dMobile is JsonElement dm ? GetDecimal(dm, "net_score") : null,
                    DScoreMobileGrossScore = dMobile is JsonElement dm1 ? GetDecimal(dm1, "gross_score") : null,
                    DScoreMobileRiskLevel = dMobile is JsonElement dm2 ? GetStr(dm2, "risk_level") : null,
                    DScoreMobileLoanable = dMobile is JsonElement dm3 ? GetDecimal(dm3, "loanable") : null,
                    DScoreLongTermNetScore = dLongTerm is JsonElement dl ? GetDecimal(dl, "net_score") : null,
                    DScoreLongTermGrossScore = dLongTerm is JsonElement dl1 ? GetDecimal(dl1, "gross_score") : null,
                    DScoreLongTermRiskLevel = dLongTerm is JsonElement dl2 ? GetStr(dl2, "risk_level") : null,
                    DScoreLongTermNetLoanableHighest = dLongTerm is JsonElement dl3 ? GetDecimal(dl3, "net_loanable_highest") : null,
                    DScoreLongTermLoanableHighest = dLongTerm is JsonElement dl4 ? GetDecimal(dl4, "loanable_highest") : null,
                    DScoreLongTermLoanable = dLongTerm is JsonElement dl5 ? GetDecimal(dl5, "loanable") : null,
                    DScoreLongTermHighest = dLongTerm is JsonElement dl6 ? GetDecimal(dl6, "highest") : null,
                    DScoreDataIncomeMatrix = dData is JsonElement dd ? GetDecimal(dd, "income_matrix") : null,
                    DScoreDataActivityLevelMatrix = dData is JsonElement dd1 ? GetDecimal(dd1, "activity_level_matrix") : null,
                    DScoreDataImpactMatrix = dData is JsonElement dd2 ? GetDecimal(dd2, "impact_matrix") : null,
                    DScoreDataIndebtednessMatrix = dData is JsonElement dd3 ? GetDecimal(dd3, "indebtedness_matrix") : null,
                    DScoreDataSafBasedLoansMatrix = dData is JsonElement dd4 ? GetDecimal(dd4, "saf_based_loans_matrix") : null,
                    DScoreDataOtherLoansMatrix = dData is JsonElement dd5 ? GetDecimal(dd5, "other_loans_matrix") : null,
                    DScoreDataLongTermOtherLoansMatrix = dData is JsonElement dd6 ? GetDecimal(dd6, "long_term_other_loans_matrix") : null,
                    DScoreDataHighBetting = dData is JsonElement dd7 ? GetStr(dd7, "high_betting") : null,
                    DScoreDataHealthcareRatioHigh = dData is JsonElement dd8 ? GetStr(dd8, "healthcare_ratio_high") : null,
                    DScoreDataLowActivity = dData is JsonElement dd9 ? GetStr(dd9, "low_activity") : null,
                    DScoreDataHistoryOfRepayingOtherMobileLoans = dData is JsonElement dd10 ? GetStr(dd10, "history_of_repaying_other_mobile_loans") : null,
                    DScoreDataDebtToIncomeRatioHigh = dData is JsonElement dd11 ? GetStr(dd11, "debt_to_income_ratio_high") : null
                }, tx);
            }

            var scores = GetObj(root, "scores");
            if (scores is JsonElement sc)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookScores (PayloadId, SpinScore, RiskLevel, HighestSafLoan, HighestOtherLoan, Affordability, EliminativeFactor)
                    VALUES (@PayloadId, @SpinScore, @RiskLevel, @HighestSafLoan, @HighestOtherLoan, @Affordability, @EliminativeFactor)
                    """, new
                {
                    PayloadId = payloadId,
                    SpinScore = GetInt(sc, "spin_score"),
                    RiskLevel = GetStr(sc, "risk_level"),
                    HighestSafLoan = GetDecimal(sc, "highest_saf_loan"),
                    HighestOtherLoan = GetDecimal(sc, "highest_other_loan"),
                    Affordability = GetDecimal(sc, "affordability"),
                    EliminativeFactor = GetStr(sc, "eliminative_factor")
                }, tx);
            }
        }

        private static async Task SaveLocationsAsync(SqlConnection conn, SqlTransaction tx, int payloadId, JsonElement? locationsArr)
        {
            if (locationsArr is not JsonElement arr) return;

            foreach (var loc in arr.EnumerateArray())
            {
                var data = GetObj(loc, "data");
                var locationId = await conn.QuerySingleAsync<int>("""
                    INSERT INTO dbo.SpinWebhookLocations (PayloadId, Name, Count, Total, Highest, Lowest, LastAmount, LastOn)
                    OUTPUT INSERTED.Id
                    VALUES (@PayloadId, @Name, @Count, @Total, @Highest, @Lowest, @LastAmount, @LastOn)
                    """, new
                {
                    PayloadId = payloadId,
                    Name = GetStr(loc, "name"),
                    Count = data is JsonElement d ? GetInt(d, "count") : null,
                    Total = data is JsonElement d1 ? GetDecimal(d1, "total") : null,
                    Highest = data is JsonElement d2 ? GetDecimal(d2, "highest") : null,
                    Lowest = data is JsonElement d3 ? GetDecimal(d3, "lowest") : null,
                    LastAmount = data is JsonElement d4 ? GetDecimal(d4, "last") : null,
                    LastOn = data is JsonElement d5 ? GetStr(d5, "last_on") : null
                }, tx);

                if (data is JsonElement dObj && GetArr(dObj, "top") is JsonElement topArr)
                {
                    foreach (var t in topArr.EnumerateArray())
                    {
                        await conn.ExecuteAsync("""
                            INSERT INTO dbo.SpinWebhookLocationTransactions (LocationId, LastDraw, Description, LastAmount, Receipt, Total, Count)
                            VALUES (@LocationId, @LastDraw, @Description, @LastAmount, @Receipt, @Total, @Count)
                            """, new
                        {
                            LocationId = locationId,
                            LastDraw = GetStr(t, "last_draw"),
                            Description = GetStr(t, "desc"),
                            LastAmount = GetDecimal(t, "last_amount"),
                            Receipt = GetStr(t, "receipt"),
                            Total = GetDecimal(t, "total"),
                            Count = GetInt(t, "count")
                        }, tx);
                    }
                }
            }
        }

        private static async Task SaveCustomerDirectionAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string direction, JsonElement? section)
        {
            if (section is not JsonElement s) return;

            var summaryId = await conn.QuerySingleAsync<int>("""
                INSERT INTO dbo.SpinWebhookCustomerSummary (PayloadId, Direction, Count, Highest, Lowest, Total, UniqueCount)
                OUTPUT INSERTED.Id
                VALUES (@PayloadId, @Direction, @Count, @Highest, @Lowest, @Total, @UniqueCount)
                """, new
            {
                PayloadId = payloadId,
                Direction = direction,
                Count = GetInt(s, "count"),
                Highest = GetDecimal(s, "highest"),
                Lowest = GetDecimal(s, "lowest"),
                Total = GetDecimal(s, "total"),
                UniqueCount = GetInt(s, "unique")
            }, tx);

            if (GetArr(s, "top") is JsonElement topArr)
            {
                foreach (var t in topArr.EnumerateArray())
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO dbo.SpinWebhookCustomerTop (SummaryId, Phone, Name, Count, Total, Highest)
                        VALUES (@SummaryId, @Phone, @Name, @Count, @Total, @Highest)
                        """, new
                    {
                        SummaryId = summaryId,
                        Phone = GetStr(t, "phone"),
                        Name = GetStr(t, "name"),
                        Count = GetInt(t, "count"),
                        Total = GetDecimal(t, "total"),
                        Highest = GetDecimal(t, "highest")
                    }, tx);
                }
            }
        }

        private static async Task SaveProductStatsAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string productName, JsonElement? section)
        {
            if (section is not JsonElement s) return;

            await conn.ExecuteAsync("""
                INSERT INTO dbo.SpinWebhookProductStats (PayloadId, ProductName, Count, Highest, Lowest, Total, LastAmount)
                VALUES (@PayloadId, @ProductName, @Count, @Highest, @Lowest, @Total, @LastAmount)
                """, new
            {
                PayloadId = payloadId,
                ProductName = productName,
                Count = GetInt(s, "count"),
                Highest = GetDecimal(s, "highest"),
                Lowest = GetDecimal(s, "lowest"),
                Total = GetDecimal(s, "total"),
                LastAmount = GetDecimal(s, "last")
            }, tx);
        }

        private static async Task SaveAgentAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string agentType, JsonElement? section)
        {
            if (section is not JsonElement s) return;

            var summaryId = await conn.QuerySingleAsync<int>("""
                INSERT INTO dbo.SpinWebhookAgentSummary (PayloadId, AgentType, Count, Highest, Lowest, Total, LastDraw, UniqueCount)
                OUTPUT INSERTED.Id
                VALUES (@PayloadId, @AgentType, @Count, @Highest, @Lowest, @Total, @LastDraw, @UniqueCount)
                """, new
            {
                PayloadId = payloadId,
                AgentType = agentType,
                Count = GetInt(s, "count"),
                Highest = GetDecimal(s, "highest"),
                Lowest = GetDecimal(s, "lowest"),
                Total = GetDecimal(s, "total"),
                LastDraw = GetDecimal(s, "last_draw"),
                UniqueCount = GetInt(s, "unique")
            }, tx);

            if (GetArr(s, "top") is JsonElement topArr)
            {
                foreach (var t in topArr.EnumerateArray())
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO dbo.SpinWebhookAgentTop (SummaryId, AgentNo, Name, Count, Total, Highest)
                        VALUES (@SummaryId, @AgentNo, @Name, @Count, @Total, @Highest)
                        """, new
                    {
                        SummaryId = summaryId,
                        AgentNo = GetStr(t, "agent_no"),
                        Name = GetStr(t, "name"),
                        Count = GetInt(t, "count"),
                        Total = GetDecimal(t, "total"),
                        Highest = GetDecimal(t, "highest")
                    }, tx);
                }
            }
        }

        private static async Task SavePaybillDirectionAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string direction, JsonElement? section)
        {
            if (section is not JsonElement s) return;

            var summaryId = await conn.QuerySingleAsync<int>("""
                INSERT INTO dbo.SpinWebhookPaybillSummary (PayloadId, Direction, Count, Highest, Lowest, Total)
                OUTPUT INSERTED.Id
                VALUES (@PayloadId, @Direction, @Count, @Highest, @Lowest, @Total)
                """, new
            {
                PayloadId = payloadId,
                Direction = direction,
                Count = GetInt(s, "count"),
                Highest = GetDecimal(s, "highest"),
                Lowest = GetDecimal(s, "lowest"),
                Total = GetDecimal(s, "total")
            }, tx);

            if (GetArr(s, "top") is JsonElement topArr)
            {
                foreach (var t in topArr.EnumerateArray())
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO dbo.SpinWebhookPaybillTop (SummaryId, PaybillNo, Name, Count, Total, Highest)
                        VALUES (@SummaryId, @PaybillNo, @Name, @Count, @Total, @Highest)
                        """, new
                    {
                        SummaryId = summaryId,
                        PaybillNo = GetStr(t, "paybill_no"),
                        Name = GetStr(t, "name"),
                        Count = GetInt(t, "count"),
                        Total = GetDecimal(t, "total"),
                        Highest = GetDecimal(t, "highest")
                    }, tx);
                }
            }

            if (GetObj(s, "classifications") is JsonElement classifications)
            {
                foreach (var categoryProp in classifications.EnumerateObject())
                {
                    if (categoryProp.Value.ValueKind != JsonValueKind.Object) continue;
                    if (GetObj(categoryProp.Value, "breakdown") is not JsonElement breakdown) continue;

                    foreach (var billerProp in breakdown.EnumerateObject())
                    {
                        if (billerProp.Value.ValueKind != JsonValueKind.Object) continue;

                        foreach (var accountProp in billerProp.Value.EnumerateObject())
                        {
                            var acct = accountProp.Value;
                            await conn.ExecuteAsync("""
                                INSERT INTO dbo.SpinWebhookPaybillAccountBreakdown
                                    (PayloadId, Direction, Category, BillerName, AccountNumber, Count, Highest, LastDraw, LastAmount)
                                VALUES
                                    (@PayloadId, @Direction, @Category, @BillerName, @AccountNumber, @Count, @Highest, @LastDraw, @LastAmount)
                                """, new
                            {
                                PayloadId = payloadId,
                                Direction = direction,
                                Category = categoryProp.Name,
                                BillerName = billerProp.Name,
                                AccountNumber = accountProp.Name,
                                Count = GetInt(acct, "count"),
                                Highest = GetDecimal(acct, "highest"),
                                LastDraw = GetStr(acct, "last_draw"),
                                LastAmount = GetDecimal(acct, "last_amount")
                            }, tx);
                        }
                    }
                }
            }
        }

        private static async Task SaveLoanProductAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string productName, JsonElement? section)
        {
            if (section is not JsonElement s) return;

            var withdraw = GetObj(s, "withdraw");
            var deposit = GetObj(s, "deposit");
            var loan = GetObj(s, "loan");
            var disburse = loan is JsonElement l ? GetObj(l, "disburse") : null;
            var repayment = loan is JsonElement l2 ? GetObj(l2, "repayment") : null;

            await conn.ExecuteAsync("""
                INSERT INTO dbo.SpinWebhookLoanProduct
                    (PayloadId, ProductName, WithdrawCount, WithdrawHighest, WithdrawTotal, WithdrawLast,
                     DepositCount, DepositHighest, DepositTotal, DepositLast,
                     LoanDisburseCount, LoanDisburseHighest, LoanDisburseTotal, LoanDisburseLast,
                     LoanRepaymentCount, LoanRepaymentHighest, LoanRepaymentTotal, LoanRepaymentLast)
                VALUES
                    (@PayloadId, @ProductName, @WithdrawCount, @WithdrawHighest, @WithdrawTotal, @WithdrawLast,
                     @DepositCount, @DepositHighest, @DepositTotal, @DepositLast,
                     @LoanDisburseCount, @LoanDisburseHighest, @LoanDisburseTotal, @LoanDisburseLast,
                     @LoanRepaymentCount, @LoanRepaymentHighest, @LoanRepaymentTotal, @LoanRepaymentLast)
                """, new
            {
                PayloadId = payloadId,
                ProductName = productName,
                WithdrawCount = withdraw is JsonElement w ? GetInt(w, "count") : null,
                WithdrawHighest = withdraw is JsonElement w1 ? GetDecimal(w1, "highest") : null,
                WithdrawTotal = withdraw is JsonElement w2 ? GetDecimal(w2, "total") : null,
                WithdrawLast = withdraw is JsonElement w3 ? GetDecimal(w3, "last_amount") : null,
                DepositCount = deposit is JsonElement dp ? GetInt(dp, "count") : null,
                DepositHighest = deposit is JsonElement dp1 ? GetDecimal(dp1, "highest") : null,
                DepositTotal = deposit is JsonElement dp2 ? GetDecimal(dp2, "total") : null,
                DepositLast = deposit is JsonElement dp3 ? GetDecimal(dp3, "last_amount") : null,
                LoanDisburseCount = disburse is JsonElement ds ? GetInt(ds, "count") : null,
                LoanDisburseHighest = disburse is JsonElement ds1 ? GetDecimal(ds1, "highest") : null,
                LoanDisburseTotal = disburse is JsonElement ds2 ? GetDecimal(ds2, "total") : null,
                LoanDisburseLast = disburse is JsonElement ds3 ? GetDecimal(ds3, "last_amount") : null,
                LoanRepaymentCount = repayment is JsonElement rp ? GetInt(rp, "count") : null,
                LoanRepaymentHighest = repayment is JsonElement rp1 ? GetDecimal(rp1, "highest") : null,
                LoanRepaymentTotal = repayment is JsonElement rp2 ? GetDecimal(rp2, "total") : null,
                LoanRepaymentLast = repayment is JsonElement rp3 ? GetDecimal(rp3, "last_amount") : null
            }, tx);
        }

        private static async Task SaveFlowAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string flowType, JsonElement? flowArr)
        {
            if (flowArr is not JsonElement arr) return;

            foreach (var entry in arr.EnumerateArray())
            {
                if (GetArr(entry, "series") is not JsonElement series) continue;

                foreach (var item in series.EnumerateArray())
                {
                    await conn.ExecuteAsync("""
                        INSERT INTO dbo.SpinWebhookIncomeExpenseFlow (PayloadId, FlowType, Period, Value)
                        VALUES (@PayloadId, @FlowType, @Period, @Value)
                        """, new
                    {
                        PayloadId = payloadId,
                        FlowType = flowType,
                        Period = GetStr(item, "name"),
                        Value = GetDecimal(item, "value")
                    }, tx);
                }
            }
        }

        private static async Task SaveBreakdownAsync(SqlConnection conn, SqlTransaction tx, int payloadId, string breakdownType, JsonElement? section)
        {
            if (section is not JsonElement s) return;
            if (GetArr(s, "data") is not JsonElement dataArr) return;
            if (GetArr(s, "labels") is not JsonElement labelsArr) return;

            var data = dataArr.EnumerateArray().ToList();
            var labels = labelsArr.EnumerateArray().ToList();
            var count = Math.Min(data.Count, labels.Count);

            for (int i = 0; i < count; i++)
            {
                await conn.ExecuteAsync("""
                    INSERT INTO dbo.SpinWebhookIncomeExpenditureBreakdown (PayloadId, BreakdownType, Label, Value)
                    VALUES (@PayloadId, @BreakdownType, @Label, @Value)
                    """, new
                {
                    PayloadId = payloadId,
                    BreakdownType = breakdownType,
                    Label = labels[i].ValueKind == JsonValueKind.String ? labels[i].GetString() : null,
                    Value = ToDecimal(data[i])
                }, tx);
            }
        }

        private static JsonElement? GetObj(JsonElement? parent, string name)
        {
            if (parent is not JsonElement p) return null;
            return p.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Object ? v : (JsonElement?)null;
        }

        private static JsonElement? GetArr(JsonElement parent, string name) =>
            parent.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Array ? v : (JsonElement?)null;

        private static string? GetStr(JsonElement obj, string name) =>
            obj.TryGetProperty(name, out var v) && v.ValueKind != JsonValueKind.Null ? v.ToString() : null;

        private static int? GetInt(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null) return null;
            if (v.ValueKind == JsonValueKind.Number) return v.TryGetInt32(out var i) ? i : (int?)Math.Round(v.GetDouble());
            if (v.ValueKind == JsonValueKind.String) return int.TryParse(v.GetString(), out var s) ? s : null;
            return null;
        }

        private static decimal? GetDecimal(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null) return null;
            return ToDecimal(v);
        }

        private static decimal? ToDecimal(JsonElement v)
        {
            if (v.ValueKind == JsonValueKind.Number) return v.GetDecimal();
            if (v.ValueKind == JsonValueKind.String) return decimal.TryParse(v.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;
            return null;
        }

        private static bool? GetBool(JsonElement obj, string name)
        {
            if (!obj.TryGetProperty(name, out var v)) return null;
            return v.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => null
            };
        }
    }
}
