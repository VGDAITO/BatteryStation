
using System;
using System.Threading;

namespace BatteryStation
{
    internal class CellProcessor : IDisposable
    {
        private readonly SemaphoreSlim _charger;
        private readonly Mutex _calibrationMutex = new Mutex();

        private static readonly object _consoleLock = new object();

        public CellProcessor(int chargerSlots)
        {
            _charger = new SemaphoreSlim(chargerSlots);
        }

        private void Log(Cell cell, string message)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss.fff}] " +
                    $"Поток {Thread.CurrentThread.ManagedThreadId} | " +
                    $"Ячейка {cell.Id} | {message} | Статус: {cell.Status}"
                );
            }
        }

        public CellResult ProcessCell(Cell cell, CancellationToken token)
        {
            DateTime start = DateTime.Now;
            string? error = null;

            bool isChargerAcquired = false;
            bool isMutexAcquired = false;

            try
            {
                token.ThrowIfCancellationRequested();

                // 1. Диагностика

                cell.Status = Cell.CellStatus.Diagnostics;
                Log(cell, "Начата диагностика");

                double diagnosticValue = 0;

                for (int i = 0; i < cell.DiagnosticIterations; i++)
                {
                    if (i % 1000 == 0)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    diagnosticValue +=
                        Math.Sqrt(i + 1) * Math.Sin(i);
                }

                token.ThrowIfCancellationRequested();

                Log(cell, "Диагностика завершена");

                // 2. Зарядка

                cell.Status = Cell.CellStatus.WaitingForCharger;
                Log(cell, "Ожидание зарядного места");

                try
                {
                    _charger.Wait(token);
                    isChargerAcquired = true;

                    cell.Status = Cell.CellStatus.Charging;
                    Log(cell, "Зарядка началась");

                    bool canceled = token.WaitHandle.WaitOne(
                        cell.ChargeDurationMs
                    );

                    if (canceled)
                    {
                        throw new OperationCanceledException();
                    }

                    cell.Status = Cell.CellStatus.WaitingForCalibration;
                    Log(cell, "Зарядка завершена");
                }
                finally
                {
                    if (isChargerAcquired)
                    {
                        _charger.Release();
                        isChargerAcquired = false;

                        Log(cell, "Зарядное место освобождено");
                    }
                }

                // 3. Калибровка

                Log(cell, "Ожидание калибровочного стенда");

                int waitResult = WaitHandle.WaitAny(
                    new WaitHandle[]
                    {
                        _calibrationMutex,
                        token.WaitHandle
                    }
                );

                if (waitResult == 1)
                {
                    throw new OperationCanceledException();
                }

                isMutexAcquired = true;

                token.ThrowIfCancellationRequested();

                cell.Status = Cell.CellStatus.Calibration;
                Log(cell, "Калибровка началась");

                bool calibrationCanceled = token.WaitHandle.WaitOne(
                    cell.CalibrationDurationMs
                );

                if (calibrationCanceled)
                {
                    throw new OperationCanceledException();
                }

                cell.Status = Cell.CellStatus.Completed;
                Log(cell, "Калибровка завершена");
            }
            catch (OperationCanceledException)
            {
                cell.Status = Cell.CellStatus.Canceled;
                Log(cell, "Обработка отменена");
            }
            catch (Exception ex)
            {
                cell.Status = Cell.CellStatus.Faulted;
                error = ex.Message;

                Log(cell, $"Ошибка: {ex.Message}");
            }
            finally
            {
                if (isChargerAcquired)
                {
                    _charger.Release();
                    isChargerAcquired = false;
                }

                if (isMutexAcquired)
                {
                    _calibrationMutex.ReleaseMutex();
                    isMutexAcquired = false;

                    Log(cell, "Калибровочный стенд освобождён");
                }

                Log(cell, "Обработка ячейки завершена");
            }

            return new CellResult(
                cell.Id,
                cell.Status,
                start,
                DateTime.Now,
                error
            );
        }

        public void Dispose()
        {
            _charger.Dispose();
            _calibrationMutex.Dispose();
        }
    }
}
