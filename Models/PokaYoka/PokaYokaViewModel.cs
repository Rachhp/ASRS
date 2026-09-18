using System;

namespace S1947.Models.PokaYoka
{
    public class PokaYokaViewModel
    {
        // =========================================================
        // DB TRANSACTION
        // =========================================================
        public long TransactionId { get; set; }


        // =========================================================
        // QR CODE DETAILS
        // =========================================================
        public string QRCode { get; set; }

        public string Model { get; set; }

        public string FGCode { get; set; }

        public string UniqueId { get; set; }

        public string PalletNumber { get; set; }


        // =========================================================
        // QR RECEIVED VALUES
        // =========================================================
        public decimal ReceivedWeight { get; set; }

        public int ReceivedQuantity { get; set; }


        // =========================================================
        // PALLET / EXPECTED WEIGHT
        // =========================================================

        // Empty pallet weight from Othermasters
        public decimal EmptyPalletWeight { get; set; }

        // Method 1:
        // ExpectedProductWeight = QR ReceivedWeight
        //
        // Method 2:
        // ExpectedProductWeight = ReceivedQuantity * SinglePartWeight
        public decimal ExpectedProductWeight { get; set; }

        // ExpectedProductWeight + EmptyPalletWeight
        public decimal ExpectedGrossWeight { get; set; }


        // =========================================================
        // PART MASTER VALUES
        // Used mainly for Method 2
        // =========================================================

        // Weight of one part
        public decimal SinglePartWeight { get; set; }

        // Maximum quantity allowed in one pallet
        public int MaxQtyPerPallet { get; set; }


        // =========================================================
        // MACHINE VALUES
        // =========================================================

        public decimal ActualWeight { get; set; }

        public int ActualQuantity { get; set; }

        public string MachineStatus { get; set; }


        // =========================================================
        // WEIGHT COMPARISON
        // =========================================================

        public decimal WeightDifference { get; set; }

        public decimal WeightTolerance { get; set; }

        public bool WeightPass { get; set; }


        // =========================================================
        // QUANTITY COMPARISON
        // =========================================================

        public int QuantityDifference { get; set; }

        public int QuantityTolerance { get; set; }

        public bool QuantityPass { get; set; }


        // =========================================================
        // INDIVIDUAL VALIDATION CHECKS
        // =========================================================

        // Method 2 - Quantity should not exceed MaxQtyPerPallet
        public bool Check1_QtyPass { get; set; }

        // QR weight / received weight check
        public bool Check2_QrWeightPass { get; set; }

        // Expected gross weight vs actual machine weight
        public bool Check3_ExpectedWeightPass { get; set; }


        // =========================================================
        // QC RESULT
        // =========================================================

        // IN_PROGRESS / ACCEPTED / REJECTED
        public string QCStatus { get; set; }

        public string Message { get; set; }


        // =========================================================
        // SCREEN / FLOW STATUS
        // =========================================================

        public bool QRScanned { get; set; }

        public bool WeightFetched { get; set; }
    }
}