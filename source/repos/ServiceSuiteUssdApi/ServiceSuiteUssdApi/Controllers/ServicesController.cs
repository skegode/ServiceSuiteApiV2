using System.Text;
using Microsoft.AspNetCore.Mvc;
using ServiceSuiteUssdApi.Models;

namespace ServiceSuiteUssdApi.Controllers
{
    [Route("ussd")]
    [ApiController]
    public class ServicesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ServicesController> _logger;

        public ServicesController(IConfiguration configuration, ILogger<ServicesController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("service")]
        public async Task<IActionResult> HttpResponseMessage([FromBody] UssdRequest ussdRequest)
        {
            string connectionString = _configuration.GetConnectionString("dbConnectionString");
            string response = string.Empty;
            Login login = new Login();
            LoanApplication loanApplication = new LoanApplication();
            Screens screens = new Screens();
            BcryptHasher b = new BcryptHasher();
            Payments payments = new Payments();

            ussdRequest.USSD_STRING ??= "";

            // Split raw USSD input into parts
            string[] rawParts = string.IsNullOrEmpty(ussdRequest.USSD_STRING)
                ? Array.Empty<string>()
                : ussdRequest.USSD_STRING.Replace("*", ",").Split(',');

            // "00" anywhere in the input = exit immediately
            if (rawParts.Any(p => p == "00"))
                return Content("END Goodbye.", "text/plain", Encoding.UTF8);

            // Build effective navigation path: "0" pops the previous entry (back)
            string[] parts = BuildEffectiveParts(rawParts);
            bool isGoingBack = rawParts.Length > 0 && rawParts[^1] == "0";
            int level = parts.Length; // 0=initial, 1=PIN entered, 2=first option, etc.

            if (!string.IsNullOrEmpty(ussdRequest.USSD_STRING))
            {
                _logger.LogInformation("Phone: {Phone}, Text: {Text}, SessionID: {SessionId}, ServiceCode: {ServiceCode}",
                    ussdRequest.MSISDN, ussdRequest.USSD_STRING, ussdRequest.SESSION_ID, ussdRequest.SERVICE_CODE);
            }

            int ussdpinStatus = login.ussdPinStatus(ussdRequest.MSISDN, 0, connectionString);

            if (ussdpinStatus == 2)
            {
                // PIN setup flow
                if (level == 0)
                {
                    int customerStatus = login.checkcustomerExists(ussdRequest.MSISDN, connectionString);
                    if (customerStatus == -2)
                    {
                        string companyName = login.GetCompanyName(ussdRequest.MSISDN, connectionString);
                        response = $"CON Welcome to {companyName}\nEnter new PIN to Proceed\n00. Exit";
                    }
                    else if (customerStatus == -3)
                    {
                        string names = loanApplication.GetCompanyNamesList(ussdRequest.MSISDN, connectionString);
                        response = $"CON Welcome!\nYou are registered with:\n{names}Enter new PIN to Proceed\n00. Exit";
                    }
                    else
                    {
                        response = "CON Enter new PIN to Proceed\n00. Exit";
                    }
                    if (!isGoingBack)
                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                }
                else if (level == 1)
                {
                    response = "CON Repeat new PIN to Proceed\n0. Back\n00. Exit";
                    if (!isGoingBack)
                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                }
                else if (level == 2)
                {
                    try
                    {
                        if (int.TryParse(parts[0], out int newPin) && int.TryParse(parts[1], out int repeatPin))
                        {
                            if (newPin == repeatPin)
                            {
                                string encryptedPin = b.HashPassword(newPin.ToString());
                                string phone = StripPhone(ussdRequest.MSISDN);

                                using (var connection = new System.Data.SqlClient.SqlConnection(connectionString))
                                {
                                    connection.Open();
                                    using (var command = new System.Data.SqlClient.SqlCommand(
                                        "UPDATE Borrowers SET USSDPIN = @newPin WHERE PhoneNumber = @phoneNumber", connection))
                                    {
                                        command.Parameters.AddWithValue("@newPin", encryptedPin);
                                        command.Parameters.AddWithValue("@phoneNumber", phone);
                                        command.ExecuteNonQuery();
                                    }
                                }
                                response = "END Your PIN has been updated successfully. Dial the code again to log in.";
                            }
                            else
                            {
                                response = "END New PIN and Repeat PIN should be the same.";
                            }
                        }
                        else
                        {
                            response = "END Invalid input. Please enter a numeric PIN.";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during PIN setup for {Phone}", ussdRequest.MSISDN);
                        response = "END An error occurred. Please try again.";
                    }
                }
            }
            else
            {
                if (level == 0)
                {
                    // Initial request or user backed all the way out — show welcome/PIN prompt
                    response = screens.FirstScreen(ussdRequest, login, connectionString);
                    login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                }
                else
                {
                    // parts[0] is always the PIN
                    if (!TryGetInt(parts, 0, out int pin))
                        return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                    if (!login.ValidatePin(ussdRequest.MSISDN, 0, pin, connectionString))
                        return Content("END Invalid Pin\n", "text/plain", Encoding.UTF8);

                    int customerStatus = login.checkcustomerExists(ussdRequest.MSISDN, connectionString);
                    int entityId = 0;
                    int selectedEntityId = 0;

                    if (level == 1)
                    {
                        // PIN validated — show main menu
                        if (customerStatus == -2)
                        {
                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, 0, connectionString);
                            if (entityId != 8)
                            {
                                response  = "CON 1. Loan Application\n";
                                response += "2. Check Balance\n";
                                response += "3. Pay\n";
                                response += "0. Back\n";
                                response += "00. Exit";
                            }
                            else
                            {
                                response  = "CON 1. Check Balance\n";
                                response += "2. Pay\n";
                                response += "0. Back\n";
                                response += "00. Exit";
                            }
                        }
                        else if (customerStatus == -3)
                        {
                            response = loanApplication.GetCustomerCompanies(ussdRequest.MSISDN, connectionString);
                            response += "0. Back\n00. Exit";
                        }

                        if (!isGoingBack)
                            login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                    }
                    else if (level == 2)
                    {
                        if (customerStatus == -2)
                        {
                            if (!TryGetInt(parts, 1, out int category))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, 0, connectionString);

                            if (entityId != 8)
                            {
                                if (category == 1)
                                {
                                    response  = loanApplication.GetEntityProducts(entityId, connectionString);
                                    response += "0. Back\n00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                                else if (category == 2)
                                {
                                    response = loanApplication.LoanBalance(ussdRequest.MSISDN, entityId, connectionString);
                                }
                                else if (category == 3)
                                {
                                    response  = "CON Enter Amount to pay\n";
                                    response += "0. Back\n";
                                    response += "00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                            }
                            else
                            {
                                if (category == 1)
                                {
                                    response = loanApplication.LoanBalance(ussdRequest.MSISDN, entityId, connectionString);
                                }
                                else if (category == 2)
                                {
                                    response  = "CON Enter Amount to pay\n";
                                    response += "0. Back\n";
                                    response += "00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                            }
                        }
                        else if (customerStatus == -3)
                        {
                            if (!TryGetInt(parts, 1, out selectedEntityId))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, selectedEntityId, connectionString);

                            if (selectedEntityId != 8)
                            {
                                response  = "CON 1. Loan Application\n";
                                response += "2. Check Balance\n";
                                response += "3. Pay Loan\n";
                                response += "0. Back\n";
                                response += "00. Exit";
                            }
                            else
                            {
                                response  = "CON 1. Check Balance\n";
                                response += "2. Pay Loan\n";
                                response += "0. Back\n";
                                response += "00. Exit";
                            }
                            if (!isGoingBack)
                                login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                        }
                    }
                    else if (level == 3)
                    {
                        if (customerStatus == -2)
                        {
                            if (!TryGetInt(parts, 1, out int category))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, 0, connectionString);

                            if (entityId != 8)
                            {
                                if (category == 1)
                                {
                                    // product selected — ask for loan amount
                                    response  = "CON Enter Loan Amount\n";
                                    response += "0. Back\n";
                                    response += "00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                                else if (category == 3)
                                {
                                    if (!TryGetDecimal(parts, 2, out decimal amount))
                                        return Content("END Invalid amount.", "text/plain", Encoding.UTF8);

                                    response = await ExecuteStkPush(payments, amount, ussdRequest.MSISDN, entityId, connectionString);
                                }
                            }
                            else
                            {
                                if (category == 2)
                                {
                                    if (!TryGetDecimal(parts, 2, out decimal amount))
                                        return Content("END Invalid amount.", "text/plain", Encoding.UTF8);

                                    response = await ExecuteStkPush(payments, amount, ussdRequest.MSISDN, entityId, connectionString);
                                }
                            }
                        }
                        else if (customerStatus == -3)
                        {
                            if (!TryGetInt(parts, 1, out selectedEntityId) || !TryGetInt(parts, 2, out int category))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, selectedEntityId, connectionString);

                            if (selectedEntityId != 8)
                            {
                                if (category == 1)
                                {
                                    response  = loanApplication.GetEntityProducts(entityId, connectionString);
                                    response += "0. Back\n00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                                else if (category == 2)
                                {
                                    response = loanApplication.LoanBalance(ussdRequest.MSISDN, entityId, connectionString);
                                }
                                else if (category == 3)
                                {
                                    response  = "CON Enter Amount to pay\n";
                                    response += "0. Back\n";
                                    response += "00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                            }
                            else
                            {
                                if (category == 1)
                                {
                                    response = loanApplication.LoanBalance(ussdRequest.MSISDN, entityId, connectionString);
                                }
                                else if (category == 2)
                                {
                                    response  = "CON Enter Amount to pay\n";
                                    response += "0. Back\n";
                                    response += "00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                            }
                        }
                    }
                    else if (level == 4)
                    {
                        if (customerStatus == -2)
                        {
                            if (!TryGetInt(parts, 1, out int category))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, 0, connectionString);

                            if (entityId != 8 && category == 1)
                            {
                                if (!TryGetInt(parts, 2, out int productId) || !TryGetDecimal(parts, 3, out decimal loanAmount))
                                    return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                                int result = loanApplication.insertloan(ussdRequest.MSISDN, loanAmount, productId, entityId, connectionString);
                                response = result > 0
                                    ? "END Your loan application has been submitted successfully."
                                    : "END Failed to submit loan application. Please try again.";
                            }
                        }
                        else if (customerStatus == -3)
                        {
                            if (!TryGetInt(parts, 1, out selectedEntityId) || !TryGetInt(parts, 2, out int category))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, selectedEntityId, connectionString);

                            if (selectedEntityId != 8)
                            {
                                if (category == 1)
                                {
                                    // product selected — ask for loan amount
                                    response  = "CON Enter Loan Amount\n";
                                    response += "0. Back\n";
                                    response += "00. Exit";
                                    if (!isGoingBack)
                                        login.sessionlevel(ussdRequest.SESSION_ID, ussdRequest.MSISDN, connectionString);
                                }
                                else if (category == 3)
                                {
                                    if (!TryGetDecimal(parts, 3, out decimal amount))
                                        return Content("END Invalid amount.", "text/plain", Encoding.UTF8);

                                    response = await ExecuteStkPush(payments, amount, ussdRequest.MSISDN, entityId, connectionString);
                                }
                            }
                            else
                            {
                                if (category == 2)
                                {
                                    if (!TryGetDecimal(parts, 3, out decimal amount))
                                        return Content("END Invalid amount.", "text/plain", Encoding.UTF8);

                                    response = await ExecuteStkPush(payments, amount, ussdRequest.MSISDN, entityId, connectionString);
                                }
                            }
                        }
                    }
                    else if (level == 5)
                    {
                        if (customerStatus == -3)
                        {
                            if (!TryGetInt(parts, 1, out selectedEntityId) || !TryGetInt(parts, 2, out int category))
                                return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                            entityId = new GetEntityId().EntityId(ussdRequest.MSISDN, customerStatus, selectedEntityId, connectionString);

                            if (selectedEntityId != 8 && category == 1)
                            {
                                if (!TryGetInt(parts, 3, out int productId) || !TryGetDecimal(parts, 4, out decimal loanAmount))
                                    return Content("END Invalid input.", "text/plain", Encoding.UTF8);

                                int result = loanApplication.insertloan(ussdRequest.MSISDN, loanAmount, productId, entityId, connectionString);
                                response = result > 0
                                    ? "END Your loan application has been submitted successfully."
                                    : "END Failed to submit loan application. Please try again.";
                            }
                        }
                    }
                }
            }

            return Content(response, "text/plain", Encoding.UTF8);
        }

        // "0" pops the previous entry from the navigation stack.
        // e.g. ["1234","2","0","1"] → ["1234","1"] (user went back then chose 1)
        private static string[] BuildEffectiveParts(string[] rawParts)
        {
            var stack = new Stack<string>();
            foreach (string p in rawParts)
            {
                if (p == "0")
                {
                    if (stack.Count > 0) stack.Pop();
                }
                else
                {
                    stack.Push(p);
                }
            }
            return stack.Reverse().ToArray();
        }

        private async Task<string> ExecuteStkPush(Payments payments, decimal amount,
            string msisdn, int entityId, string connectionString)
        {
            try
            {
                string stkResult = await payments.InitiateStkPush(amount, msisdn, entityId, connectionString);
                _logger.LogInformation("STK response for {Phone} EntityId={EntityId}: {Result}", msisdn, entityId, stkResult);

                if (string.IsNullOrEmpty(stkResult))
                    return "END Payment request failed. Please try again later.";

                // Safaricom error responses contain "errorCode"
                if (stkResult.Contains("errorCode"))
                {
                    _logger.LogWarning("STK push rejected for {Phone} EntityId={EntityId}: {Result}", msisdn, entityId, stkResult);
                    return "END Payment request failed. Please try again later.";
                }

                return "END Your request has been received and is being processed. Wait for M-Pesa prompt.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "STK push exception for {Phone} EntityId={EntityId}", msisdn, entityId);
                return "END There was a problem processing your request, try again later.";
            }
        }

        private static string StripPhone(string phone) =>
            phone.Length > 12 ? phone.Substring(1, 12) : phone;

        private static bool TryGetInt(string[] parts, int index, out int value)
        {
            value = 0;
            return index < parts.Length && int.TryParse(parts[index], out value);
        }

        private static bool TryGetDecimal(string[] parts, int index, out decimal value)
        {
            value = 0;
            return index < parts.Length && decimal.TryParse(parts[index], out value);
        }
    }
}
