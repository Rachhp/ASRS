using S1947.Models;
using S1947.Models.PokaYoka;
using S1947.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Services
{
        public class PokaYokaService : IPokaYokaService
        {
            private readonly IQrCodeParser _qrCodeParser;
            private readonly IWeighingMachineService _weighingMachine;


        // ============================================================
        // HARDCODED - Replace with DB fetch from PartMaster
        // when MaxQtyPerPallet and EachPartWeight columns are added.
        //
        // TODO: Fetch from PartMaster:
        //   var part = db.PartMasters
        //       .FirstOrDefault(p => p.FGCode == model.FGCode);
        //   maxQtyPerPallet = part.MaxQtyPerPallet;
        //   eachPartWeight  = part.EachPartWeight;
        // ============================================================
        private const int MaxQtyPerPallet = 100;         // hardcoded
        private const decimal EachPartWeight = 1.25m;    // hardcoded (kg per part)
        private const decimal WeightTolerance = 0.5m;    // kg tolerance



        public PokaYokaService(
                IQrCodeParser qrCodeParser,
                IWeighingMachineService weighingMachine)
            {
                _qrCodeParser = qrCodeParser;
                _weighingMachine = weighingMachine;
            }

            public PokaYokaViewModel ScanQr(string qrCode)
            {
                var model = _qrCodeParser.Parse(qrCode);

                // Insert scan record
                using (var db = new S1947Entities())
                {
                    var transaction = new PokaYokaQCTransaction
                    {
                        QRCode = model.QRCode,
                        FGModel = model.Model,
                        FGCode = model.FGCode,
                        PalletNumber = model.PalletNumber,
                        ReceivedWeight = model.ReceivedWeight,
                        ReceivedQuantity = model.ReceivedQuantity,
                        UniqueId = model.UniqueId,
                        ScannedAt = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        CreatedBy = System.Web.HttpContext.Current != null &&
                                    System.Web.HttpContext.Current.User != null &&
                                    System.Web.HttpContext.Current.User.Identity != null
                                        ? System.Web.HttpContext.Current.User.Identity.Name
                                        : "SYSTEM"
                    };

                    db.PokaYokaQCTransactions.Add(transaction);
                    db.SaveChanges();

                    model.TransactionId = transaction.Id;
                }

                return model;
            }


        public PokaYokaViewModel FetchWeight(long transactionId)
        {
            using (var db = new S1947Entities())
            {
                // Fetch the scanned record from DB
                var transaction = db.PokaYokaQCTransactions
                    .FirstOrDefault(t => t.Id == transactionId);

                if (transaction == null)
                    throw new Exception("Transaction not found.");

                // Call weighing machine
                var result = _weighingMachine.GetWeight();

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                // Get empty pallet weight from Othermaster
                decimal emptyPalletWeight = 0;
                var otherMaster = db.Othermasters.FirstOrDefault();
                if (otherMaster != null && otherMaster.EmptyPalletwight.HasValue)
                    emptyPalletWeight = otherMaster.EmptyPalletwight.Value;



                // Build model from DB record
                // Build model from DB record
                var model = new PokaYokaViewModel
                {
                    TransactionId = transaction.Id,
                    QRCode = transaction.QRCode,
                    Model = transaction.FGModel,
                    FGCode = transaction.FGCode,
                    UniqueId = transaction.UniqueId,
                    PalletNumber = transaction.PalletNumber,
                    ReceivedWeight = transaction.ReceivedWeight ?? 0,
                    ReceivedQuantity = (int)(transaction.ReceivedQuantity ?? 0),
                    QRScanned = true,
                    ActualWeight = result.Weight,
                    WeightFetched = true,
                    MachineStatus = "CONNECTED"
                };
                // ================================================
                // Step 4 - Validation
                // ================================================

                // Check 1 - Quantity
                // Received Quantity <= Max Qty per Pallet
                model.Check1_QtyPass =
                    model.ReceivedQuantity <= MaxQtyPerPallet;

                // Check 2 - QR Weight vs. Scale Weight
                // Weight Difference = QR Weight - Actual Scale Weight
                model.WeightDifference =
                    model.ReceivedWeight - model.ActualWeight;

                model.Check2_QrWeightPass =
                    Math.Abs(model.WeightDifference) <= WeightTolerance;

                // Check 3 - Expected Weight vs. Actual Weight
                // Expected Gross Weight =
                //   (Quantity x Each Part Weight) + Empty Pallet Weight
                decimal expectedGrossWeight =
                    (model.ReceivedQuantity * EachPartWeight)
                    + emptyPalletWeight;

                decimal expectedWeightDifference =
                    expectedGrossWeight - model.ActualWeight;

                model.Check3_ExpectedWeightPass =
                    Math.Abs(expectedWeightDifference) <= WeightTolerance;

                // Quantity difference (for display)
                model.ActualQuantity = model.ReceivedQuantity;
                model.QuantityDifference = 0;

                // ================================================
                // Step 5 - Final Decision
                // ================================================
                if (model.Check1_QtyPass &&
                    model.Check2_QrWeightPass &&
                    model.Check3_ExpectedWeightPass)
                {
                    model.QCStatus = "OK";
                    model.Message = "QC PASSED";
                }
                else
                {
                    model.QCStatus = "NOT OK";

                    var failures = new List<string>();
                    if (!model.Check1_QtyPass)
                        failures.Add("Qty exceeds max per pallet");
                    if (!model.Check2_QrWeightPass)
                        failures.Add("QR weight mismatch");
                    if (!model.Check3_ExpectedWeightPass)
                        failures.Add("Expected weight mismatch");

                    model.Message = "QC FAILED: "
                        + string.Join(", ", failures);
                }

                // Update the DB record
                transaction.ActualWeight = model.ActualWeight;
                transaction.ActualQuantity = model.ActualQuantity;
                transaction.WeightDifference = model.WeightDifference;
                transaction.QuantityDifference = model.QuantityDifference;
                transaction.QCStatus = model.QCStatus;
                transaction.MachineResponse = result.RawResponse;
                transaction.WeighedAt = DateTime.Now;
                transaction.CompletedAt = DateTime.Now;

                db.SaveChanges();

                return model;
            }

            // Check 1 - Quantity
            // Received Quantity <= Max Qty per Pallet
            //    model.Check1_QtyPass =
            //      model.ReceivedQuantity <= MaxQtyPerPallet;

            //  //  model.ActualQuantity = CalculateQuantity(model);

            //    model.WeightDifference = Math.Abs(
            //        model.ReceivedWeight - model.ActualWeight);

            //    model.QuantityDifference = Math.Abs(
            //        model.ReceivedQuantity - model.ActualQuantity);

            //    model.QCStatus = CalculateQCStatus(model);

            //    // Update the DB record
            //    transaction.ActualWeight = model.ActualWeight;
            //    transaction.ActualQuantity = model.ActualQuantity;
            //    transaction.WeightDifference = model.WeightDifference;
            //    transaction.QuantityDifference = model.QuantityDifference;
            //    transaction.QCStatus = model.QCStatus;
            //    transaction.MachineResponse = result.RawResponse;
            //    transaction.WeighedAt = DateTime.Now;
            //    transaction.CompletedAt = DateTime.Now;

            //    db.SaveChanges();

            //    return model;
            //}
        }


        //public PokaYokaViewModel FetchWeight(
        //    PokaYokaViewModel model)
        //{
        //    if (model == null)
        //        throw new Exception("Invalid request.");

        //    if (!model.QRScanned)
        //        throw new Exception(
        //            "Please scan QR code first.");

        //    var result = _weighingMachine.GetWeight();

        //    if (!result.Success)
        //        throw new Exception(
        //            result.ErrorMessage);

        //    model.ActualWeight = result.Weight;

        //    // Temporary.
        //    // Actual calculation will be implemented
        //    // after we know your quantity rule.
        //    model.ActualQuantity =
        //        CalculateQuantity(model);

        //    model.WeightDifference =
        //        Math.Abs(
        //            model.ReceivedWeight -
        //            model.ActualWeight);

        //    model.QuantityDifference =
        //        Math.Abs(
        //            model.ReceivedQuantity -
        //            model.ActualQuantity);

        //    model.QCStatus =
        //        CalculateQCStatus(model);

        //    model.WeightFetched = true;

        //    model.MachineStatus = "CONNECTED";

        //    // Update the scan record with weight results
        //    using (var db = new S1947Entities())
        //    {
        //        var transaction = db.PokaYokaQCTransactions
        //            .FirstOrDefault(t => t.Id == model.TransactionId);

        //        if (transaction != null)
        //        {
        //            transaction.ActualWeight = model.ActualWeight;
        //            transaction.ActualQuantity = model.ActualQuantity;
        //            transaction.WeightDifference = model.WeightDifference;
        //            transaction.QuantityDifference = model.QuantityDifference;
        //            transaction.QCStatus = model.QCStatus;
        //            transaction.MachineResponse = result.RawResponse;
        //            transaction.WeighedAt = DateTime.Now;
        //            transaction.CompletedAt = DateTime.Now;

        //            db.SaveChanges();
        //        }
        //    }
        //    return model;
        //}

        private decimal CalculateQuantity(
                PokaYokaViewModel model)
            {
                // TODO:
                // Implement your actual quantity formula.

                return model.ReceivedQuantity;
            }

            private string CalculateQCStatus(
                PokaYokaViewModel model)
            {
                const decimal WeightTolerance = 0.01m; // example tolerance
                if (model.WeightDifference <= WeightTolerance && model.QuantityDifference == 0)
                    return "OK";
                return "NOT OK";
            }
        }
    }
