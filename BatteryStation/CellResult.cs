using System;
using static BatteryStation.Cell;

namespace BatteryStation
{
    internal class CellResult
    {
        public int CellIdentifier { get; set; }

        public Cell.CellStatus Status { get; set; }

        public DateTime Start { get; set; }

        public DateTime? End { get; set; }

        public string? Error { get; set; }

        public CellResult(int cellIdentifier, Cell.CellStatus status, DateTime start, DateTime? end, string? error)
        {
            CellIdentifier = cellIdentifier;
            Status = status;
            Start = start;
            End = end;
            Error = error;
        }
    }
}
