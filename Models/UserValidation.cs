using System;

namespace S1947.Models
{
    public class UserValidation
    {
        public int MTransNo { get; set; }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public string PW { get; set; }
        public string UserType { get; set; }
        public string ContactNo { get; set; }
        public string EmailId { get; set; }
        public Nullable<short> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<short> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public string Status { get; set; }
        public string DeleteStatus { get; set; }


        //Finger Print
        public int FPId { get; set; }
        public byte[] Finger1 { get; set; }
        public byte[] Finger2 { get; set; }
    }
}