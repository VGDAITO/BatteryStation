using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryStation
{
    internal class Cell
    {
        public int Id { get; set; }
        public int DiagnosticIterations { get; set; }

        public int ChargeDurationMs { get; set; }

        public int CalibrationDurationMs { get; set; }


        public enum CellStatus
        {
            Pending,
            Diagnostics,
            WaitingForCharger,
            Charging,
            WaitingForCalibration,
            Calibration,
            Completed,
            Canceled,
            Faulted

        }

        public CellStatus Status { get; set; }

        public Cell(int id, int diagnosticIterations, int chargeDurationMs, int calibrationDurationMs)
        {
            Id = id;
            DiagnosticIterations = diagnosticIterations;
            ChargeDurationMs = chargeDurationMs;
            CalibrationDurationMs = calibrationDurationMs;
            Status = CellStatus.Pending;
        }
    }
}
