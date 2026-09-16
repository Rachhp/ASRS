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
                        Model = model.Model,
                        FGCode = model.FGCode,
                        PalletNumber = model.PalletNumber,
                        ReceivedWeight = model.ReceivedWeight,
                        ReceivedQuantity = model.ReceivedQuantity,
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

            public PokaYokaViewModel FetchWeight(
                PokaYokaViewModel model)
            {
                if (model == null)
                    throw new Exception("Invalid request.");

                if (!model.QRScanned)
                    throw new Exception(
                        "Please scan QR code first.");

                var result = _weighingMachine.GetWeight();

                if (!result.Success)
                    throw new Exception(
                        result.ErrorMessage);

                model.ActualWeight = result.Weight;

                // Temporary.
                // Actual calculation will be implemented
                // after we know your quantity rule.
                model.ActualQuantity =
                    CalculateQuantity(model);

                model.WeightDifference =
                    Math.Abs(
                        model.ReceivedWeight -
                        model.ActualWeight);

                model.QuantityDifference =
                    Math.Abs(
                        model.ReceivedQuantity -
                        model.ActualQuantity);

                model.QCStatus =
                    CalculateQCStatus(model);

                model.WeightFetched = true;

                model.MachineStatus = "CONNECTED";

                // Update the scan record with weight results
                using (var db = new S1947Entities())
                {
                    var transaction = db.PokaYokaQCTransactions
                        .FirstOrDefault(t => t.Id == model.TransactionId);

                    if (transaction != null)
                    {
                        transaction.ActualWeight = model.ActualWeight;
                        transaction.ActualQuantity = model.ActualQuantity;
                        transaction.WeightDifference = model.WeightDifference;
                        transaction.QuantityDifference = model.QuantityDifference;
                        transaction.QCStatus = model.QCStatus;
                        transaction.MachineResponse = result.RawResponse;
                        transaction.WeighedAt = DateTime.Now;
                        transaction.CompletedAt = DateTime.Now;

                        db.SaveChanges();
                    }
                }

                return model;
            }

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
