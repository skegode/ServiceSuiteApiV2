using System.Data;
using System.Data.SqlClient;

namespace ServiceSuiteUssdApi.Models
{
    public class LoanApplication
    {
        public int insertloan(string phone, decimal amount, int productid, int companyid, string constr)
        {
            phone = phone.Length > 12 ? phone.Substring(1, 12) : phone;
            int result = 0;

            using (var con = new SqlConnection(constr))
            using (var cmd = new SqlCommand("sp_UssdInsertLoan", con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.Parameters.AddWithValue("@Principal", amount);
                cmd.Parameters.AddWithValue("@ProductId", productid);
                cmd.Parameters.AddWithValue("@EntityId", companyid);
                con.Open();
                result = Convert.ToInt32(cmd.ExecuteScalar());
            }

            return result;
        }

        public decimal loanbalanceNloanlimt(string phone, int flag, int entityId, string constr)
        {
            decimal result = 0;

            using (SqlConnection con = new SqlConnection(constr))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("sp_GetolbOrLoanLimit", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@phone", phone);
                        cmd.Parameters.AddWithValue("@flag", flag);
                        cmd.Parameters.AddWithValue("@EntityId", entityId);
                        con.Open();
                        result = Convert.ToDecimal(cmd.ExecuteScalar());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during loan balance/limit retrieval: {ex.Message}");
                }
            }

            return result;
        }

        public string LoanBalance(string phoneNumber, int entityId, string constr)
        {
            decimal loanBalance = loanbalanceNloanlimt(phoneNumber, 2, entityId, constr);
            return $"END Your Loan Balance is {loanBalance}\n";
        }

        public string GetEntityProducts(int entityId, string constr)
        {
            string response = "CON Select Product\n";

            using (SqlConnection con = new SqlConnection(constr))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT ID AS ProductId, ProductName FROM Products WHERE EntityId = @EntityId", con))
                    {
                        cmd.Parameters.AddWithValue("@EntityId", entityId);
                        con.Open();
                        using (SqlDataReader read = cmd.ExecuteReader())
                        {
                            while (read.Read())
                                response += $"{read["ProductId"]}. {read["ProductName"]}\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching products: {ex.Message}");
                    response = "END Error occurred while fetching products. Please try again later.\n";
                }
            }

            return response;
        }

        public string GetCompanyNamesList(string phone, string constr)
        {
            string names = "";

            using (SqlConnection con = new SqlConnection(constr))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("EXEC GetCustomerCompanies @phone", con))
                    {
                        cmd.Parameters.AddWithValue("@phone", phone);
                        con.Open();
                        using (SqlDataReader read = cmd.ExecuteReader())
                        {
                            while (read.Read())
                                names += $"{read["CompanyName"]}\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching company names: {ex.Message}");
                }
            }

            return names;
        }

        public string GetCustomerCompanies(string phone, string constr)
        {
            string response = "CON Select Lender\n";

            using (SqlConnection con = new SqlConnection(constr))
            {
                try
                {
                    using (SqlCommand cmd = new SqlCommand("EXEC GetCustomerCompanies @phone", con))
                    {
                        cmd.Parameters.AddWithValue("@phone", phone);
                        con.Open();
                        using (SqlDataReader read = cmd.ExecuteReader())
                        {
                            while (read.Read())
                                response += $"{read["id"]}. {read["CompanyName"]}\n";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching customer companies: {ex.Message}");
                    response = "END Error occurred while fetching customer companies. Please try again later.\n";
                }
            }

            return response;
        }
    }
}
