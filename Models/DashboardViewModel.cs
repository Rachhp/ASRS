using System;
using System.Collections.Generic;

namespace S1947.Models
{
    public class DashboardViewModel
    {
        public int LiveStock { get; set; }
        public int ALocation { get; set; }
        public int TotalUser { get; set; }

        public int MTransNo { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Shift { get; set; }
        public int PackingLineNo { get; set; }

        public string UniqueId { get; set; }
        public int PalletSrNo { get; set; }
        public int Quantity { get; set; }
        public string Model { get; set; }
        public string ModelDesc { get; set; }
        public int FGcode { get; set; }
        public decimal WeightTheo { get; set; }
        public string OperatorName { get; set; }
        public string ShiftSupervisor { get; set; }
        public int TotalIssue { get; set; }
        public int TotalReturn { get; set; }
        public int TotalQty { get; set; }
        public int PartNumbers { get; set; }
        public int TotalCells { get; set; }
        public int OccupiedCells { get; set; }
        public int AvailableCells { get; set; }
        public decimal Utilization { get; set; }

        public List<RecentTransactionVM> RecentTransactions { get; set; }
    }
    public class RecentTransactionVM
    {
        public string MaterialCode { get; set; }
        public string MaterialDescription { get; set; }
        public string MachineCode { get; set; }
        public string MachineDesc { get; set; }
        public string Operation { get; set; }
        public DateTime? Date { get; set; }
    }
}