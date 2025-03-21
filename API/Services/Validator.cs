using System.Text.RegularExpressions;

namespace API.Services
{
    public class Validator
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PhoneRegex = new Regex(@"^(\+?\d{1,3})?\(?\d{2}\)?\d{8,9}$", RegexOptions.Compiled);

        public static bool IsValidEmail(string email)
        {
            return EmailRegex.IsMatch(email);
        }

        public static bool IsValidCpf(string cpf)
        {
            cpf = cpf.Replace(".", "").Replace("-", "");

            if (cpf.Length != 11 || !long.TryParse(cpf, out _))
                return false;

            int[] cpfArray = Array.ConvertAll(cpf.ToCharArray(), c => (int)char.GetNumericValue(c));

            int[] weights1 = { 10, 9, 8, 7, 6, 5, 4, 3, 2 };
            int[] weights2 = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2 };

            int sum1 = 0;
            int sum2 = 0;

            for (int i = 0; i < 9; i++)
            {
                sum1 += cpfArray[i] * weights1[i];
            }

            int remainder1 = (sum1 % 11);
            int digit1 = (remainder1 < 2) ? 0 : 11 - remainder1;

            if (digit1 != cpfArray[9])
                return false;

            for (int i = 0; i < 10; i++)
            {
                sum2 += cpfArray[i] * weights2[i];
            }

            int remainder2 = (sum2 % 11);
            int digit2 = (remainder2 < 2) ? 0 : 11 - remainder2;

            return digit2 == cpfArray[10];
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            return PhoneRegex.IsMatch(phoneNumber);
        }

        public static bool ContainsLetter(string input)
        {
            foreach (char c in input)
            {
                if (char.IsLetter(c))
                    return true;
            }
            return false;
        }

        public string RemoveNonNumeric(string phoneNumber)
        {
            return new string(phoneNumber.Where(char.IsDigit).ToArray());
        }

        public static bool IsValidPassword(string password)
        {
            return password.Length >= 6 && password.Length <= 12 && password.Any(char.IsDigit);
        }
    }
}
