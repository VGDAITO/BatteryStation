using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BatteryStation
{
    internal class CellProcessor
    {
        private readonly SemaphoreSlim _charger = new SemaphoreSlim(4);
        private readonly Mutex _calibrationMutex = new Mutex();

        public CellResult ProcessCell(Cell cell, CancellationToken token)
        {
            DateTime start = DateTime.Now;

            string? error = null;

            bool isChargerAcquired = false;
            bool isMutexAcquired = false;






            try
            {
                cell.Status = Cell.CellStatus.Diagnostics;

                token.ThrowIfCancellationRequested();

                double result = 0;

                for (int i = 0; i < cell.DiagnosticIterations; i++)
                {
                    if (i % 1000 == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }
                    result += Math.Sqrt(i + 1) * Math.Sin(i);

                }
                cell.Status = Cell.CellStatus.WaitingForCharger;
                _charger.Wait(token);
                isChargerAcquired = true;
                cell.Status = Cell.CellStatus.Charging;


                token.WaitHandle.WaitOne(cell.ChargeDurationMs);
                token.ThrowIfCancellationRequested();

                if (isChargerAcquired == true)
                {
                    _charger.Release();
                    isChargerAcquired = false;
                }




                cell.Status = Cell.CellStatus.WaitingForCalibration;
                
                int waitResult = WaitHandle.WaitAny(new WaitHandle[]
                                {
                    _calibrationMutex,
                    token.WaitHandle
                                });

                
                if(waitResult == 0)
                {
                    isMutexAcquired = true;
                    token.ThrowIfCancellationRequested();

                    cell.Status = Cell.CellStatus.Calibration;

                    token.WaitHandle.WaitOne(cell.CalibrationDurationMs);
                    token.ThrowIfCancellationRequested();

                    cell.Status = Cell.CellStatus.Completed;


                }


                else if (waitResult == 1)
                {
                    throw new OperationCanceledException();
                }
                




            }
            catch (OperationCanceledException)
            {
                cell.Status = Cell.CellStatus.Canceled;

            }

            catch (Exception ex)
            {
                cell.Status = Cell.CellStatus.Faulted;
                error = ex.Message;
            }
            finally
            {
                if (isChargerAcquired == true)
                {
                    _charger.Release();
                }
                if (isMutexAcquired == true)
                {
                    _calibrationMutex.ReleaseMutex();
                }
                

                Console.WriteLine($"Обработка ячейки {cell.Id} завершена");
            }
            return new CellResult(cell.Id, cell.Status, start, DateTime.Now, error);

        }
    }
}
