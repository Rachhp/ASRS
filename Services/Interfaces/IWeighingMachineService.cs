using S1947.Models.PokaYoka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Services.Interfaces
{
    public interface IWeighingMachineService
    {
        WeightResultModel GetWeight();
    }
}