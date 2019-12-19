using System;
using System.Collections.Generic;
using System.Text;

namespace ASPE.DQM.Identity
{
    public class Claims
    {
        public const string SystemAdministrator_Key = "SYSTEM.ADMINISTRATOR";
        public const string AuthorMetric_Key = "AUTHOR.METRIC";
        public const string SubmitMeasure_Key = "SUBMIT.MEASURE";
        public const string FirstName_Key = System.Security.Claims.ClaimTypes.GivenName;
        public const string LastName_Key = System.Security.Claims.ClaimTypes.Surname;
        public const string Phone_Key = System.Security.Claims.ClaimTypes.OtherPhone;
        public const string Organization_Key = "USER.ORGANIZATION";
        //do not include NameIdentifier in the key's collection so that it does not affect the sync process
        public const string UserID_Key = System.Security.Claims.ClaimTypes.NameIdentifier;

        public static readonly string[] Keys = new[] {
            SystemAdministrator_Key,
            AuthorMetric_Key,
            SubmitMeasure_Key,
            FirstName_Key,
            LastName_Key,
            Phone_Key,
            Organization_Key
        };
    }
}
