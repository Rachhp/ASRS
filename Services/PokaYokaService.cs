using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using Org.BouncyCastle.Bcpg;
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


        public PokaYokaService(IQrCodeParser qrCodeParser, IWeighingMachineService weighingMachine)
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
                var vmodel = new PokaYokaViewModel();
                // Fetch the scanned record from DB
                var transaction = db.PokaYokaQCTransactions.FirstOrDefault(t => t.Id == transactionId);

                if (transaction == null)
                    throw new Exception("Transaction not found.");

                // Call weighing machine
                var result = _weighingMachine.GetWeight();

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);
                decimal actualWeight = result.Weight;

                // Step 3 - Get empty pallet weight
                decimal emptyPalletWeight = 0m;
                var otherMaster = db.Othermasters.FirstOrDefault();
                if (otherMaster != null &&
                    otherMaster.EmptyPalletwight.HasValue)
                {
                    emptyPalletWeight = otherMaster.EmptyPalletwight.Value;
                }

                // Step 4 - Get PartMaster information
                // TODO:
                // Replace these hardcoded values with PartMaster values
                // once the columns are available.
                //
                // Example:
                //
                // var partMaster = db.PartMasters
                //     .FirstOrDefault(x => x.FGCode == transaction.FGCode);
                //
                // if (partMaster == null)
                //     throw new Exception("Part Master not found.");
                //
                // decimal singlePartWeight = partMaster.SinglePartWeight;
                // int maxQtyPerPallet = partMaster.MaxQtyPerPallet;

                decimal EachPartWeight = 200m; // TODO: Fetch from PartMaster
                int maxQtyPerPallet = 150;       // TODO: Fetch from PartMaster
                decimal weightTolerance = 0.5m;


                // Build model from DB record
                var model = new PokaYokaViewModel
                {
                    TransactionId = transaction.Id,
                    QRCode = transaction.QRCode,
                    Model = transaction.FGModel,
                    FGCode = transaction.FGCode,
                    UniqueId = transaction.UniqueId,
                    PalletNumber = transaction.PalletNumber,
                    ReceivedWeight = transaction.ReceivedWeight ?? 0,//from scanner received weight
                    ReceivedQuantity = (int)(transaction.ReceivedQuantity ?? 0),
                    QRScanned = true,
                    ActualWeight = result.Weight,
                    WeightFetched = true,
                    MachineStatus = "CONNECTED"
                };

                //Step 6 - Calculate expected gross weight

                //also check receivedweight with actual weight 
                //model.Check1_QtyPass = transaction.ReceivedWeight == actualWeight;


                //Expected Gross Weight = Quantity * Weight of One Part + Empty Pallet Weight
                decimal expectedGrossWeight = (model.ReceivedQuantity * EachPartWeight) + emptyPalletWeight;

                // Step 7 - Calculate Weight differences
                decimal expectedWeightDifference = expectedGrossWeight - model.ActualWeight;
                model.WeightDifference = Math.Abs(expectedWeightDifference);//set to model

                // step 8 Quantity need to compare with the master maxperpallet qty & received qty
                model.QuantityDifference = maxQtyPerPallet - model.ReceivedQuantity;
                // Quantity difference (for display)
                model.ActualQuantity = model.ReceivedQuantity;

                //model.Check3_ExpectedWeightPass =Math.Abs(expectedWeightDifference) <= WeightTolerance;

                // Quantity should not exceed maximum quantity
                // model.Check1_QtyPass =model.ReceivedQuantity <= maxQtyPerPallet;

                // ================================================
                // Step 5 - Final Decision
                // ================================================
                if (expectedGrossWeight == model.ActualWeight)
                {
                    model.QCStatus = "OK";
                    model.Message = "QC PASSED";
                }
                else
                {
                    model.QCStatus = "NOT OK";

                    var failures = "Expected weight mismatch";
                    model.Message = "QC FAILED: " + string.Join(", ", failures);
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

        }
    }
}
