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
using System.Web.Services.Description;

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
            string userName =
           System.Web.HttpContext.Current != null &&
           System.Web.HttpContext.Current.User != null &&
           System.Web.HttpContext.Current.User.Identity != null
               ? System.Web.HttpContext.Current.User.Identity.Name
               : "SYSTEM";


            // Insert scan record
            using (var db = new S1947Entities())
            {
                // Find existing pallet using UniqueId
                var pallet = db.PokaYokaPallets.FirstOrDefault(x => x.UniqueId == model.UniqueId);
                int attemptNo;

                if (pallet != null && pallet.AttemptNo >= 1 && pallet.QCStatus == "ACCEPTED")
                    return new PokaYokaViewModel
                    {
                        Message = "Already scanned and accepted."
                    };
  
                //2. explit the qr
                var data = qrCode.Split(';');
                if (data.Length < 11)
                {
                    return new PokaYokaViewModel { Message = "Invalid QR format" };
                }


                if (pallet == null)
                {
                    attemptNo = 1;
                    // FIRST ATTEMPT
                    pallet = new PokaYokaPallet
                    {
                        QRCode = model.QRCode,
                        FGModel = model.Model,
                        FGCode = model.FGCode,
                        PalletNumber = model.PalletNumber,
                        ReceivedWeight = model.ReceivedWeight,
                        ReceivedQuantity = model.ReceivedQuantity,
                        UniqueId = model.UniqueId,
                        AttemptNo = attemptNo,
                        QCStatus = "SCANNED",
                        CreatedBy = userName,
                        CreatedAt = DateTime.Now
                    };
                    db.PokaYokaPallets.Add(pallet);
                    db.SaveChanges();
                }
                else
                {
                    // RE-ATTEMPT
                    // Previous attempt + 1
                    attemptNo = (pallet.AttemptNo ?? 0) + 1;
                    pallet.AttemptNo = attemptNo;
                    // Update latest scanned information
                    pallet.QRCode = model.QRCode;
                    pallet.FGModel = model.Model;
                    pallet.FGCode = model.FGCode;
                    pallet.PalletNumber = model.PalletNumber;
                    pallet.ReceivedWeight = model.ReceivedWeight;
                    pallet.ReceivedQuantity = model.ReceivedQuantity;
                    pallet.QCStatus = "SCANNED";
                    pallet.ReworkStatus = "IN_PROGRESS";
                    pallet.ReworkCompletedAt = DateTime.Now;
                    //pallet.UpdatedBy = userName;
                    pallet.RejectReason = userName;

                    db.SaveChanges();
                }

                // CREATE NEW QC HISTORY FOR THIS ATTEMPT
                var history = new PokaYokaQCHistory
                {
                    TransactionId = checked((int)pallet.Id),
                    ReceivedWeight = model.ReceivedWeight,
                    ReceivedQuantity = model.ReceivedQuantity,
                    ScannedAt = DateTime.Now,
                    AttemptNo = attemptNo,
                    QCStatus = "IN_PROGRESS",
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName
                };

                db.PokaYokaQCHistories.Add(history);
                db.SaveChanges();
                // Return IDs to UI
                model.TransactionId = history.AttemptId;
                return model;
            }
        }



        public PokaYokaViewModel FetchWeightMeth1(long transactionId)
        {
            using (var db = new S1947Entities())
            {
                // 1. GET QC HISTORY
                var history = db.PokaYokaQCHistories.FirstOrDefault(x => x.AttemptId == transactionId);

                if (history == null)
                    throw new Exception("QC history record not found.");

                // 2. GET PALLET
                var pallet = db.PokaYokaPallets.FirstOrDefault(x => x.Id == history.TransactionId);

                if (pallet == null)
                    throw new Exception("Pallet record not found.");

                // 3. GET WEIGHT FROM WEIGHING MACHINE
                var result = _weighingMachine.GetWeight();
                if (!result.Success)
                    throw new Exception(result.ErrorMessage);
                decimal actualWeight = result.Weight;

                // 4. GET EMPTY PALLET WEIGHT
                decimal emptyPalletWeight = 0m;
                var otherMaster = db.Othermasters.FirstOrDefault();
                if (otherMaster != null &&
                    otherMaster.EmptyPalletwight.HasValue)
                {
                    emptyPalletWeight =
                        otherMaster.EmptyPalletwight.Value;
                }

                // 5. METHOD 1
                //
                // QR Received Weight does NOT include
                // pallet weight.
                //
                // Expected Gross Weight =
                // QR Received Weight + Empty Pallet Weight
                decimal receivedWeight =history.ReceivedWeight ?? 0m;
                decimal expectedProductWeight =receivedWeight;//same bcz QR weight is only product weight
                decimal expectedGrossWeight = expectedProductWeight + emptyPalletWeight;

                // 6. WEIGHT TOLERANCE
                decimal weightTolerance = 0.5m;

                // 7. CALCULATE WEIGHT DIFFERENCE

                decimal weightDifference =Math.Abs(expectedGrossWeight - actualWeight);

                // 8. UPDATE HISTORY
                history.ReceivedWeight =receivedWeight;
                history.EmptyPalletWeight = emptyPalletWeight;
                history.ExpectedProductWeight =expectedProductWeight;
                history.ExpectedGrossWeight = expectedGrossWeight;
                history.ActualWeight = actualWeight;
                history.WeightDifference = weightDifference;
                history.WeightTolerance = weightTolerance;

                // 9. METHOD 1 HAS NO:
                // SinglePartWeight
                // MaxQtyPerPallet
                // QuantityDifference
                // QuantityTolerance
                // Therefore these remain NULL.
                history.SinglePartWeight = null;
                history.MaxQtyPerPallet = null;
                history.QuantityDifference = null;
                history.QuantityTolerance = null;


                // 10. FINAL QC DECISION
                // ONLY WEIGHT CHECK
                if (weightDifference <= weightTolerance)
                {
                    history.QCStatus = "ACCEPTED";
                    history.QCReason = "Weight matched within tolerance.";
                }
                else
                {
                    history.QCStatus = "REJECTED";
                    history.QCReason = "Expected weight mismatch.";
                }

                // 11. MACHINE RESPONSE
                history.MachineResponse =result.RawResponse;
                history.WeighedAt =DateTime.Now;
                history.CompletedAt = DateTime.Now;

                // 12. UPDATE CURRENT PALLET STATUS
                pallet.QCStatus =history.QCStatus;
                if (history.QCStatus == "REJECTED")
                {
                    pallet.RejectReason =history.QCReason;
                    pallet.ReworkStatus ="PENDING";
                    pallet.ReworkCompletedAt = null;
                }
                else
                {
                    pallet.RejectReason = null;
                    pallet.ReworkStatus ="COMPLETED";
                    pallet.ReworkCompletedAt = DateTime.Now;
                }
                db.SaveChanges();

                // 14. BUILD VIEW MODEL
                var model = new PokaYokaViewModel
                {
                    TransactionId = history.AttemptId,
                    QRCode =pallet.QRCode,
                    Model = pallet.FGModel,
                    FGCode =pallet.FGCode,
                    UniqueId = pallet.UniqueId,
                    PalletNumber =pallet.PalletNumber,
                    ReceivedWeight =receivedWeight,
                    ReceivedQuantity =history.ReceivedQuantity ?? 0,
                    QRScanned =true,
                    ActualWeight =actualWeight,
                    WeightFetched =true,
                    MachineStatus ="CONNECTED",
                    ExpectedGrossWeight = expectedGrossWeight,
                    WeightDifference = weightDifference,
                    QCStatus = history.QCStatus,
                    Message =history.QCReason
                };

                return model;
            }
        }


        public PokaYokaViewModel FetchWeightMeth2(long transactionId)
        {
            using (var db = new S1947Entities())
            {
                // 1. GET QC HISTORY
                var history = db.PokaYokaQCHistories
                    .FirstOrDefault(x => x.AttemptId == transactionId);

                if (history == null)
                    throw new Exception("QC history record not found.");

                // 2. GET PALLET
                var pallet = db.PokaYokaPallets
                    .FirstOrDefault(x => x.Id == history.TransactionId);

                if (pallet == null)
                    throw new Exception("Pallet record not found.");

                // 3. GET ACTUAL WEIGHT FROM WEIGHING MACHINE
                var result = _weighingMachine.GetWeight();

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                decimal actualWeight = result.Weight;

                // 4. GET EMPTY PALLET WEIGHT AND PALLET TOLERANCE
                var otherMaster = db.Othermasters.FirstOrDefault();

                if (otherMaster == null)
                    throw new Exception("Pallet master configuration not found.");

                decimal emptyPalletWeight =
                    otherMaster.EmptyPalletwight ?? 0m;

                decimal palletWeightTolerance =
                    Convert.ToDecimal(otherMaster.palletWeightTolerance);

                if (emptyPalletWeight < 0)
                    throw new Exception("Empty pallet weight cannot be negative.");

                if (palletWeightTolerance < 0)
                    throw new Exception("Pallet weight tolerance cannot be negative.");

                // 5. GET PART MASTER
                var partMaster = db.PartMasters
                    .FirstOrDefault(x => x.FGCode == pallet.FGCode);

                if (partMaster == null)
                {
                    throw new Exception(
                        "Part Master not found for FG Code: " + pallet.FGCode);
                }

                // 6. GET MASTER UNIT PART WEIGHT
                decimal singlePartWeight =
                    Convert.ToDecimal(partMaster.FGWeight);

                if (singlePartWeight <= 0)
                    throw new Exception("Master unit part weight must be greater than zero.");

                // 7. GET MAXIMUM QUANTITY PER PALLET
                int maxQtyPerPallet;

                if (partMaster.Segment == "MC")
                {
                    maxQtyPerPallet = 38;
                }
                else if (partMaster.Segment == "SC")
                {
                    maxQtyPerPallet = 87;
                }
                else
                {
                    throw new Exception(
                        "Maximum pallet quantity is not configured for segment: "
                        + partMaster.Segment);
                }

                // 8. GET RECEIVED QUANTITY
                int receivedQuantity = history.ReceivedQuantity ?? 0;

                if (receivedQuantity <= 0)
                    throw new Exception("Received quantity must be greater than zero.");

                // 9. UNIT PART-WEIGHT VARIATION
                // EXAMPLE VALUE: Replace with your actual master-data field
                // when its column name is available.
                decimal unitPartWeightVariation = 0.005m;

                if (unitPartWeightVariation < 0)
                {
                    throw new Exception(
                        "Unit part-weight variation cannot be negative.");
                }

                // 10. CALCULATE EXPECTED PRODUCT WEIGHT
                // Master unit part weight x received quantity
                decimal expectedProductWeight =
                    singlePartWeight * receivedQuantity;

                // 11. CALCULATE EXPECTED GROSS WEIGHT
                // Expected product weight + empty pallet weight
                decimal expectedGrossWeight =
                    expectedProductWeight + emptyPalletWeight;

                // 12. CALCULATE TOTAL TOLERANCE
                // Pallet tolerance + (unit part-weight variation x quantity)
                decimal totalTolerance =
                    palletWeightTolerance +
                    (unitPartWeightVariation * receivedQuantity);

                // 13. CALCULATE WEIGHT DIFFERENCE
                decimal weightDifference =
                    Math.Abs(actualWeight - expectedGrossWeight);

                // 14. WEIGHT AND QUANTITY VALIDATION
                bool weightPassed =
                    weightDifference <= totalTolerance;

                bool quantityPassed =
                    receivedQuantity <= maxQtyPerPallet;

                // Remaining capacity, NOT the number of physically missing parts.
                int quantityDifference =
                    maxQtyPerPallet - receivedQuantity;
                // 8. CALCULATE MISSING QUANTITY
                // Assumption: every pallet should contain its maximum capacity.
                int expectedQuantity = maxQtyPerPallet;

                int missingQuantity = Math.Max(0,expectedQuantity - receivedQuantity );

                // 15. UPDATE QC HISTORY
                history.SinglePartWeight = singlePartWeight;
                history.EmptyPalletWeight = emptyPalletWeight;
                history.MaxQtyPerPallet = maxQtyPerPallet;

                history.ExpectedProductWeight = expectedProductWeight;
                history.ExpectedGrossWeight = expectedGrossWeight;
                history.ActualWeight = actualWeight;

                history.ReceivedQuantity = receivedQuantity;
                history.ActualQuantity = receivedQuantity;
                history.WeightDifference = weightDifference;
                history.QuantityDifference = quantityDifference;

                // Save the total tolerance actually used for the decision.
                history.WeightTolerance = totalTolerance;

                // 16. FINAL QC DECISION
                var failures = new List<string>();

                if (!weightPassed)
                {
                    failures.Add("Expected weight mismatch");
                }

                if (!quantityPassed)
                {
                    failures.Add("Quantity exceeds maximum quantity");
                }

                if (failures.Count == 0)
                {
                    history.QCStatus = "ACCEPTED";
                    history.QCReason = "QC PASSED";

                    pallet.LoadingStatus = "ALLOWED";
                    pallet.RejectReason = null;
                    pallet.ReworkStatus = "COMPLETED";
                    pallet.ReworkCompletedAt = DateTime.Now;
                }
                else
                {
                    history.QCStatus = "REJECTED";
                    history.QCReason = string.Join(", ", failures);

                    pallet.LoadingStatus = "NOTALLOWED";
                    pallet.RejectReason = history.QCReason;
                    pallet.ReworkStatus = "PENDING";
                    pallet.ReworkCompletedAt = null;
                }

                // 17. MACHINE INFORMATION
                history.MachineResponse = result.RawResponse;
                history.WeighedAt = DateTime.Now;
                history.CompletedAt = DateTime.Now;

                // 18. UPDATE PALLET QC STATUS
                pallet.QCStatus = history.QCStatus;

                // 19. SAVE CHANGES
                db.SaveChanges();

                // 20. BUILD VIEW MODEL
                var model = new PokaYokaViewModel
                {
                    TransactionId = history.AttemptId,
                    QRCode = pallet.QRCode,
                    Model = pallet.FGModel,
                    FGCode = pallet.FGCode,
                    UniqueId = pallet.UniqueId,
                    PalletNumber = pallet.PalletNumber,

                    ReceivedWeight = history.ReceivedWeight ?? 0m,
                    ReceivedQuantity = receivedQuantity,

                    QRScanned = true,
                    ActualWeight = actualWeight,
                    ActualQuantity = receivedQuantity,
                    WeightFetched = true,
                    MachineStatus = "CONNECTED",

                    ExpectedGrossWeight = expectedGrossWeight,
                    WeightDifference = weightDifference,
                    QuantityDifference = quantityDifference,

                    QCStatus = history.QCStatus,
                    MissingQuantity = missingQuantity, 
                    Message = history.QCReason
                };

                return model;
            }
        }


        //public PokaYokaViewModel FetchWeightMeth2(long transactionId)
        //{
        //    using (var db = new S1947Entities())
        //    {
        //        // 1. GET QC HISTORY
        //        var history = db.PokaYokaQCHistories.FirstOrDefault(x => x.AttemptId == transactionId);
        //        if (history == null)
        //            throw new Exception("QC history record not found.");

        //        // 2. GET PALLET
        //        var pallet = db.PokaYokaPallets.FirstOrDefault(x => x.Id == history.TransactionId);
        //        if (pallet == null)
        //            throw new Exception("Pallet record not found.");

        //        // 3. GET WEIGHT FROM WEIGHING MACHINE
        //        var result = _weighingMachine.GetWeight();//call weighing machine service to get weight

        //        if (!result.Success)
        //            throw new Exception(result.ErrorMessage);

        //        decimal actualWeight =result.Weight;

        //        // 4. GET EMPTY PALLET WEIGHT 
        //        decimal emptyPalletWeight = 0m;
        //        var otherMaster =db.Othermasters.FirstOrDefault();
        //        if (otherMaster != null && otherMaster.EmptyPalletwight.HasValue)
        //        {
        //            emptyPalletWeight = otherMaster.EmptyPalletwight.Value;
        //        }

        //        // 5. GET PART MASTER
        //        // IMPORTANT:
        //        // Replace the below query/column names with
        //        // your actual PartMaster entity/columns.

        //        var partMaster = db.PartMasters.FirstOrDefault(x =>x.FGCode == pallet.FGCode);
        //        if (partMaster == null)
        //            throw new Exception(
        //                "Part Master not found for FG Code: "+ pallet.FGCode);

        //       // 6.GET MASTER VALUES
        //        decimal singlePartWeight = (decimal)partMaster.FGWeight;
        //        int maxQtyPerPallet = 0;
        //        //check if part is mc then  38 and if sc then 87 max             
        //        //new chnages after discussion with team october 8
        //        if (partMaster.Segment == "MC")
        //        {
        //             maxQtyPerPallet = 38;
        //        }
        //        else if (partMaster.Segment == "SC")
        //        {
        //             maxQtyPerPallet = 87;
        //        }
        //        // 7. GET RECEIVED QUANTITY
        //        int receivedQuantity = history.ReceivedQuantity ?? 0;

        //        // 8. METHOD 2
        //       //  Product Weight = Quantity × Single Part Weight              
        //        decimal expectedProductWeight =receivedQuantity *singlePartWeight;

        //        // 9. EXPECTED GROSS WEIGHT
        //        // Product Weight + Empty Pallet Weight
        //        decimal expectedGrossWeight =expectedProductWeight + emptyPalletWeight;


        //        // Fetch these two values from your master data.             
        //        decimal palletWeightTolerance = (decimal)otherMaster.palletWeightTolerance;
        //        decimal unitPartWeightVariation = 0.005m;

        //        // Calculate total tolerance
        //        decimal totalTolerance =palletWeightTolerance +(unitPartWeightVariation * receivedQuantity);

        //        // Calculate weight difference
        //        decimal weightDifference =Math.Abs(expectedGrossWeight - actualWeight);

        //        // Validate weight
        //        bool weightPassed =weightDifference <= totalTolerance;

        //        // Validate quantity
        //        bool quantityPassed =receivedQuantity <= maxQtyPerPallet;


        //        // 10. WEIGHT TOLERANCE 
        //        ////now for tolreance
        //        //decimal weightTolerance = 0.5m;

        //        //// 11. QUANTITY TOLERANCE
        //        //int quantityTolerance = 0;

        //        //// 12. WEIGHT DIFFERENCE
        //        //decimal weightDifference =Math.Abs(expectedGrossWeight -actualWeight);

        //        //// 13. QUANTITY DIFFERENCE
        //        int quantityDifference = maxQtyPerPallet - receivedQuantity;

        //        //// 14. VALIDATIONS
        //        //bool weightPassed =weightDifference <=weightTolerance;
        //        //bool quantityPassed =receivedQuantity <=maxQtyPerPallet;

        //        // 15. UPDATE HISTORY

        //        history.ReceivedWeight =history.ReceivedWeight;
        //        history.ReceivedQuantity =receivedQuantity;
        //        history.SinglePartWeight =singlePartWeight;
        //        history.EmptyPalletWeight =emptyPalletWeight;
        //        history.MaxQtyPerPallet =maxQtyPerPallet;
        //        history.ExpectedProductWeight =expectedProductWeight;
        //        history.ExpectedGrossWeight =expectedGrossWeight;
        //        history.ActualWeight =actualWeight;
        //        history.ActualQuantity = receivedQuantity;
        //        history.WeightDifference = weightDifference;
        //        history.QuantityDifference =quantityDifference;
        //        history.WeightTolerance =totalTolerance;
        //       // history.QuantityTolerance =quantityTolerance;

        //        // 16. FINAL QC DECISION
        //        var failures =new List<string>();
        //        if (!weightPassed)
        //        {
        //            failures.Add(
        //                "Expected weight mismatch");
        //        }

        //        if (!quantityPassed)
        //        {
        //            failures.Add(
        //                "Quantity exceeds maximum quantity");
        //        }

        //        if (failures.Count == 0)
        //        {
        //            history.QCStatus ="ACCEPTED";
        //            history.QCReason ="QC PASSED";
        //            pallet.LoadingStatus = "ALLOWED";

        //        }
        //        else
        //        {
        //            history.QCStatus ="REJECTED";
        //            history.QCReason = string.Join(", ", failures);
        //            pallet.LoadingStatus = "NOTALLOWED";
        //            history.QCReason =string.Join(", ", failures);
        //        }

        //        // 17. MACHINE INFORMATION
        //        history.MachineResponse =result.RawResponse;
        //        history.WeighedAt = DateTime.Now;
        //        history.CompletedAt =DateTime.Now;

        //        // ==========================================
        //        // 18. UPDATE PALLET
        //        // ==========================================

        //        pallet.QCStatus = history.QCStatus;
        //        if (history.QCStatus == "REJECTED")
        //        {
        //            pallet.RejectReason =history.QCReason;
        //            pallet.ReworkStatus = "PENDING";
        //            pallet.LoadingStatus = "NOTALLOWED";
        //            pallet.ReworkCompletedAt =null;
        //        }
        //        else
        //        {
        //            pallet.RejectReason =null;
        //            pallet.ReworkStatus = "COMPLETED";
        //            pallet.ReworkCompletedAt = DateTime.Now;
        //        }
        //        db.SaveChanges();

        //        // 20. BUILD VIEW MODEL
        //        var model = new PokaYokaViewModel
        //        {
        //            TransactionId =history.AttemptId,
        //            QRCode =pallet.QRCode,
        //            Model =pallet.FGModel,
        //            FGCode =pallet.FGCode,
        //            UniqueId = pallet.UniqueId,
        //            PalletNumber = pallet.PalletNumber,
        //            ReceivedWeight =history.ReceivedWeight ?? 0,
        //            ReceivedQuantity =receivedQuantity,
        //            QRScanned =true,
        //            ActualWeight = actualWeight,
        //            ActualQuantity = receivedQuantity,
        //            WeightFetched =true,
        //            MachineStatus = "CONNECTED",
        //            ExpectedGrossWeight = expectedGrossWeight,
        //            WeightDifference =weightDifference,
        //            QuantityDifference =quantityDifference,
        //            QCStatus =history.QCStatus,
        //            Message =  history.QCReason
        //        };

        //        return model;
        //    }
        //}

    }
}
