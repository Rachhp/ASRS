using S1947.Models;
using S1947.Models.PokaYoka;
using S1947.Services.Interfaces;
using System;

// TODO: Uncomment when TCP machine is connected
// using System.IO;
// using System.Net.Sockets;
// using System.Text;

namespace S1947.Services
{
    public class TcpWeighingMachineService : IWeighingMachineService
    {
        private readonly string _ipAddress;
        private readonly int _port;

        public TcpWeighingMachineService(
            string ipAddress,
            int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public WeightResultModel GetWeight()
        {
            try
            {
                // ============================================================
                // HARDCODED - for testing without machine.
                // Remove this block and uncomment the TCP code below
                // when the weighing machine is connected.
                // ============================================================
                return new WeightResultModel
                {
                    Success = true,
                    Weight = 125.400m,
                    RawResponse = "125.400",
                    ReadTime = DateTime.Now
                };

                // ============================================================
                // TCP IMPLEMENTATION - uncomment when machine is connected.
                //
                // Adjust the following as per your machine protocol:
                //   1. CommandToSend  - poll command the machine expects
                //                       (some machines push weight
                //                       continuously and no command is
                //                       needed - just read the stream).
                //   2. Terminator     - response terminator (CR/LF etc.).
                //   3. ParseWeight()  - parsing logic per the machine's
                //                       data frame format.
                // ============================================================
                //const string CommandToSend = "P\r\n"; // e.g. "P" = poll weight
                //const int ReceiveTimeoutMs = 3000;
                //const int ConnectTimeoutMs = 3000;
                //
                //string rawResponse;
                //
                //using (var client = new TcpClient())
                //{
                //    // Connect with timeout
                //    var connectResult = client.BeginConnect(
                //        _ipAddress, _port, null, null);
                //
                //    bool connected = connectResult.AsyncWaitHandle
                //        .WaitOne(ConnectTimeoutMs);
                //
                //    if (!connected)
                //        throw new Exception(
                //            "Weighing machine connection timeout.");
                //
                //    client.EndConnect(connectResult);
                //    client.ReceiveTimeout = ReceiveTimeoutMs;
                //    client.SendTimeout = ReceiveTimeoutMs;
                //
                //    using (var stream = client.GetStream())
                //    {
                //        // 1. Send poll command (skip if machine pushes data)
                //        byte[] commandBytes =
                //            Encoding.ASCII.GetBytes(CommandToSend);
                //        stream.Write(commandBytes, 0, commandBytes.Length);
                //
                //        // 2. Read response until terminator
                //        var buffer = new byte[256];
                //        var response = new StringBuilder();
                //
                //        int bytesRead;
                //        do
                //        {
                //            bytesRead = stream.Read(
                //                buffer, 0, buffer.Length);
                //
                //            response.Append(
                //                Encoding.ASCII.GetString(
                //                    buffer, 0, bytesRead));
                //        }
                //        while (bytesRead > 0 &&
                //               !response.ToString().Contains("\n"));
                //
                //        rawResponse = response.ToString().Trim();
                //    }
                //}
                //
                //// 3. Parse the raw response
                //decimal weight = ParseWeight(rawResponse);
                //
                //return new WeightResultModel
                //{
                //    Success = true,
                //    Weight = weight,
                //    RawResponse = rawResponse,
                //    ReadTime = DateTime.Now
                //};
            }
            catch (Exception ex)
            {
                return new WeightResultModel
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ReadTime = DateTime.Now
                };
            }
        }

        // ============================================================
        // TCP IMPLEMENTATION - uncomment when machine is connected.
        //
        // Parses the machine's raw response and returns the weight.
        // Adjust per your machine's frame format.
        //
        // Common formats:
        //   "ST,GS,+  125.400 kg"  -> take numeric part
        //   "125.400"              -> direct parse
        // ============================================================
        //private decimal ParseWeight(string rawResponse)
        //{
        //    if (string.IsNullOrWhiteSpace(rawResponse))
        //        throw new Exception("Empty response from weighing machine.");
        //
        //    // Extract numeric characters, sign and decimal point
        //    var numericPart = new string(
        //        rawResponse
        //            .Where(c => char.IsDigit(c) || c == '.' || c == '-')
        //            .ToArray());
        //
        //    decimal weight;
        //    if (!decimal.TryParse(
        //            numericPart,
        //            System.Globalization.NumberStyles.Any,
        //            System.Globalization.CultureInfo.InvariantCulture,
        //            out weight))
        //    {
        //        throw new Exception(
        //            "Invalid weight response: " + rawResponse);
        //    }
        //
        //    return weight;
        //}
    }
}
