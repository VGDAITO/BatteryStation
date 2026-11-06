
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace BatteryStation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Главный поток: {Thread.CurrentThread.ManagedThreadId}");

            const int cellCount = 20;
            const int chargerSlots = 4;
            const int diagnosticIterations = 100000;
            const int autoCancelMs = 0;

            using CancellationTokenSource cts = new CancellationTokenSource();

            CellProcessor processor = new CellProcessor(chargerSlots);

            List<Cell> cells = new List<Cell>();

            Random random = new Random();

            for (int i = 1; i <= cellCount; i++)
            {
                cells.Add(new Cell(
                    i,
                    diagnosticIterations,
                    random.Next(300, 901),
                    random.Next(100, 301)
                ));
            }

            using CountdownEvent countDownEvent = new CountdownEvent(cells.Count);

            ConcurrentBag<CellResult> results = new ConcurrentBag<CellResult>();

            Stopwatch stopwatch = new Stopwatch();

            int workerThreads;
            int completionPortThreads;

            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

            Console.WriteLine($"Доступно рабочих потоков: {workerThreads}");
            Console.WriteLine($"Доступно потоков ввода-вывода: {completionPortThreads}");

            Console.WriteLine("\nЯчейки подготовлены. Начинаем обработку.\n");

            ConsoleCancelEventHandler cancelHandler = (sender, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("\nПолучена команда отмены!");
            };

            Console.CancelKeyPress += cancelHandler;

            if (autoCancelMs > 0)
            {
                cts.CancelAfter(autoCancelMs);
            }

            stopwatch.Start();

            foreach (var cell in cells)
            {
                bool queued = ThreadPool.QueueUserWorkItem(state =>
                {
                    Cell currentCell = (Cell)state!;

                    try
                    {
                        Console.WriteLine(
                            $"Поток {Thread.CurrentThread.ManagedThreadId} обрабатывает ячейку {currentCell.Id}"
                        );

                        CellResult cellResult = processor.ProcessCell(currentCell, cts.Token);

                        results.Add(cellResult);
                    }
                    catch (Exception ex)
                    {
                        currentCell.Status = Cell.CellStatus.Faulted;

                        results.Add(new CellResult(
                            currentCell.Id,
                            currentCell.Status,
                            DateTime.Now,
                            DateTime.Now,
                            ex.Message
                        ));
                    }
                    finally
                    {
                        countDownEvent.Signal();
                    }
                }, cell);

                if (!queued)
                {
                    cell.Status = Cell.CellStatus.Faulted;

                    results.Add(new CellResult(
                        cell.Id,
                        cell.Status,
                        DateTime.Now,
                        DateTime.Now,
                        "Не удалось добавить ячейку в ThreadPool"
                    ));

                    countDownEvent.Signal();
                }
            }

            countDownEvent.Wait();

            stopwatch.Stop();

            Console.CancelKeyPress -= cancelHandler;

            Console.WriteLine("\nВсе ячейки обработаны.");

            int completedCount = 0;
            int canceledCount = 0;
            int faultedCount = 0;

            foreach (var result in results)
            {
                Console.WriteLine(
                    $"Ячейка {result.CellIdentifier}: {result.Status}"
                );

                if (result.Status == Cell.CellStatus.Completed)
                {
                    completedCount++;
                }
                else if (result.Status == Cell.CellStatus.Canceled)
                {
                    canceledCount++;
                }
                else if (result.Status == Cell.CellStatus.Faulted)
                {
                    faultedCount++;
                }
            }

            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);

            Console.WriteLine("\nИтоговая статистика:");
            Console.WriteLine($"Успешно: {completedCount}");
            Console.WriteLine($"Отменено: {canceledCount}");
            Console.WriteLine($"Ошибки: {faultedCount}");
            Console.WriteLine($"Всего: {results.Count}");
            Console.WriteLine($"Время выполнения: {stopwatch.ElapsedMilliseconds} мс");

            Console.WriteLine($"\nДоступно рабочих потоков: {workerThreads}");
            Console.WriteLine($"Доступно потоков ввода-вывода: {completionPortThreads}");

            processor.Dispose();
        }
    }
}
