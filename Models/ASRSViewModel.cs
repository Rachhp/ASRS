using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace S1947.Models
{
    public class ASRSViewModel
    {

        public List<int> Levels { get; set; }
        public List<int> Columns { get; set; }

        public List<ASRSLevelColumn> LevelColumns { get; set; }

        public string SelectedLocation { get; set; }

        public List<ASRSPosition> Positions { get; set; }
    }


        public class ASRSLevelColumn
        {
            public int Level { get; set; }

            public int Column { get; set; }

            public string LocationName
            {
                get
                {
                    return $"L{Level}C{Column}";
                }
            }
        }



        public class ASRSPosition
        {
            public string LocationNo { get; set; }

            public int Level { get; set; }

            public int Column { get; set; }

            public int Position { get; set; }


            public string Status { get; set; }

            public string PalletNo { get; set; }

            public decimal Qty { get; set; }
        }
    public class ASRSLocation
    {
        public int Level { get; set; }
        public int Column { get; set; }
        public int Position { get; set; }

        public string Barcode { get; set; }
        public int Qty { get; set; }
    }

}