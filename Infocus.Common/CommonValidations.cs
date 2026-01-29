using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Infocus.Common
{
    public static class CommonValidations
    {
        private static readonly string RegexEmailValidation = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";

        public static Boolean IsValidEmail(String inputEmailAddresses, Boolean allowMultiple)
        {
            if(!allowMultiple)
            {
                return IsValidEmail(inputEmailAddresses);
            }

            String[] str = inputEmailAddresses.Split(',', ';');
            return IsValidEmail(str);
        }

        public static Boolean IsValidEmail(String inputEmail)
        {
            Regex re = new Regex(RegexEmailValidation);
            if(re.IsMatch(inputEmail))
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        public static Boolean IsValidEmail(String[] inputEmails)
        {
            foreach(String str in inputEmails)
            {
                if(!IsValidEmail(str))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
