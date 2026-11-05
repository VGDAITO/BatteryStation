namespace BatteryStation
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine(
    $"Главный поток: {Thread.CurrentThread.ManagedThreadId}"
);

            using CancellationTokenSource cts = new CancellationTokenSource();
            SemaphoreSlim semaphore = new SemaphoreSlim(2);



            CellProcessor processor = new CellProcessor();

            using CountdownEvent countDownEvent = new CountdownEvent(cells.Count);



            List<Cell> cells = new List<Cell>
            {
                new Cell(1, 5, 1000, 500),
                new Cell(2, 3, 1500, 700),
                new Cell(3, 4, 1200, 600)
            };

            foreach (var cell in cells)
            {
                Console.WriteLine($"Информация о батарейках ID {cell.Id}\nДиагностика: {cell.DiagnosticIterations}\nЗарядка: {cell.ChargeDurationMs}\nКалибровка: {cell.CalibrationDurationMs}\nСтатус: {cell.Status}\n-----------------\n\n\n");
            }

            cts.Cancel();



            foreach (var cell in cells)
            {
                ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        Console.WriteLine($"Рабочий поток: {Thread.CurrentThread.ManagedThreadId}");
                        CellResult result = processor.ProcessCell(cell, cts.Token);
                        Console.WriteLine($"Ячейка: {result.CellIdentifier}\n" +
                            $"Статус: {result.Status}\n" +
                            $"Начало: {result.Start}\n" +
                            $"Конец: {result.End}\n" +
                            $"Ошибка: {result.Error ?? "Нет"}\n\n\n");
                    }


                    finally
                    {
                        countDownEvent.Signal();
                    }
                });

            }


            countDownEvent.Wait();
            Console.WriteLine("Все ячейки обработаны.");
        }
    }
}
