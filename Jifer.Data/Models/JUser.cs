using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Jifer.Data.Models
{
    public class JUser:IdentityUser
    {
        public JUser()
        {
            this.IsActive = true;
        }
        public string FirstName {  get; set; }

        public string MiddleName { get; set; }

        public string LastName { get; set; }

        public int MyProperty { get; set; }

        public bool IsActive { get; set; }

        public string Gender { get; set; }

        public DateTime DateOfBirth { get; set; }

        private enum ValidGenders{
            Male,
            Female,
            Other
        }

        private enum ValidAccessibility
        {
            FriendsOnly,
            FriendsToFriendsOnly,
            Public
        }

    }
}
