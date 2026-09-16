using System;

namespace S1947.Models
{
    public class Shuttlemastervalidationcs
    {
        public int MTransNo { get; set; }
        public Nullable<int> ShuttleNo { get; set; }
        public String LiftNo { get; set; }
        public Nullable<int> RunMode { get; set; }
        public string LockStatus { get; set; }
        public Nullable<short> CreatedBy { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<short> ModifiedBy { get; set; }
        public Nullable<System.DateTime> ModifiedOn { get; set; }
        public string DeleteStatus { get; set; }
    }
}