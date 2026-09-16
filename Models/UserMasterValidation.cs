using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace S1947.Models
{
    public class UserMasterValidation
    {
        public int MTransNo { get; set; }

        public string UserName { get; set; }

        [Remote("isExists", "User", AdditionalFields = "MTransNo", ErrorMessage = "User ID is already exists.")]
        public string UserId { get; set; }

        public string PW { get; set; }

        public string UserType { get; set; }

        [RegularExpression(@"^[1-9]\d{9}$", ErrorMessage = "Enter a valid 10-digit contact number")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Contact number must be exactly 10 digits")]
        public string ContactNo { get; set; }
        public string LineNos { get; set; }
        public string EmailId { get; set; }
        public string FloorNo { get; set; }
        public string BioId1 { get; set; }
        public string BioId2 { get; set; }
        public string BioData1 { get; set; }
        public string BioData2 { get; set; }
        public string ImagePath { get; set; }
        [Required(ErrorMessage = "Selece the User Status")]
        public string LockStatus { get; set; }
        public Nullable<short> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<short> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public string DeleteStatus { get; set; }
        //public Nullable<short> SubscID { get; set; }
        public Nullable<System.DateTime> LastLogin { get; set; }
        public Nullable<int> flag { get; set; }
    }
}