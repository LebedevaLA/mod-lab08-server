using System.Threading;
using System;
using System.IO;
namespace TPProj
{
    class Program
    {
        static int Factorial(int n)
        {
            if (n == 0 || n == 1) return 1;
            return n * Factorial(n - 1);
        }
        static void Main()
        {
            // p = lambda/mu;
            //Интенсивность поступления требований  lambda = 1/единица времени поступления;
            //Интенсивность обслуживания mu = 1/единица времени обработки;
            //P простоя P0 =   сумма i от 0 до n (p в степени i/i!) в -1;
            //P отказа Pn = (p в степени n/ n!)*P0;
            //относительная пропускная способнсть Q = 1 - Pn;
            //абсолютная пропускная способность A = lambda*Q;
            //среднее число занятых каналов k = A/mu; 
            int n = 5;
            for (int requestDelay = 10; requestDelay <= 300; requestDelay += 10) { 
                for (int serviceTime = 100; serviceTime <= 800; serviceTime += 100)
                {
                    double lambda = 1.0/ (double)requestDelay;
                    double mu = 1.0 / (double)serviceTime;
                    double p = lambda / mu;
                    double P0 = 0;
                    for (int i =0; i < n; i++)
                    {
                        P0 += 1/(Math.Pow(p, i)/Factorial(i));
                    }
                    double Pn = (Math.Pow(p, n) / Factorial(n)) * P0;
                    double Q = 1 - Pn;
                    double A = lambda * Q;
                    double k = A / mu;
                }
            }
            for (int requestDelay = 10; requestDelay <= 300; requestDelay += 10)
            {
                for (int serviceTime = 100; serviceTime <= 800; serviceTime += 100)
                {
                    Server server = new Server(serviceTime, n);
                    Client client = new Client(server, requestDelay);
                    for (int id = 1; id <= 100; id++)
                    {
                        client.send(id);
                    }
                    Console.WriteLine("Всего заявок: {0}", server.requestCount);
                    Console.WriteLine("Обработано заявок: {0}", server.processedCount);
                    Console.WriteLine("Отклонено заявок: {0}", server.rejectedCount);
                    double lambda = 1.0 / (double)requestDelay;
                    double mu = 1.0 / (double)serviceTime;
                    double P0 = 1 - ((double)server.processedCount / server.requestCount);
                    double Pn = (double)server.rejectedCount / server.requestCount;
                    double Q = (double)server.processedCount / server.requestCount;
                    double A = lambda * Q;
                    double k = A / mu;
                }
            }
        }
        struct PoolRecord
        {
            public Thread thread;
            public bool in_use;
        }
        class Server
        {
            private int serviceTime;
            private int n;
            private PoolRecord[] pool;
            private object threadLock = new object();
            public int requestCount = 0;
            public int processedCount = 0;
            public int rejectedCount = 0;
            public Server(int serviceTime, int n)
            {
                this.serviceTime = serviceTime;
                this.n = n;
                pool = new PoolRecord[n];
            }
            public void proc(object sender, procEventArgs e)
            {
                lock (threadLock)
                {
                    Console.WriteLine("Заявка с номером: {0}", e.id);
                    requestCount++;
                    for (int i = 0; i < n; i++)
                    {
                        if (!pool[i].in_use)
                        {
                            pool[i].in_use = true;
                            pool[i].thread = new Thread(new ParameterizedThreadStart(Answer));
                            pool[i].thread.Start(e.id);
                            processedCount++;
                            return;
                        }
                    }
                    rejectedCount++;
                }
            }
            public void Answer(object arg)
            {
                int id = (int)arg;

                Console.WriteLine("Обработка заявки: {0}", id);

                Thread.Sleep(this.serviceTime);

                for (int i = 0; i < n; i++)
                {
                    if (pool[i].thread == Thread.CurrentThread)
                        pool[i].in_use = false;
                }
            }
            public void PrintStatistic()
            {

            }
        }
        class Client
        {
            private Server server;
            private int requestDelay;
            public Client(Server server, int requestDelay)
            {
                this.requestDelay = requestDelay;
                this.server = server;
                this.request += server.proc;

            }
            public void send(int id)
            {
                Thread.Sleep(requestDelay);
                procEventArgs args = new procEventArgs();
                args.id = id;
                OnProc(args);
            }
            protected virtual void OnProc(procEventArgs e)
            {
                EventHandler<procEventArgs> handler = request;
                if (handler != null)
                {
                    handler(this, e);
                }
            }
            public event EventHandler<procEventArgs> request;
        }

        public class procEventArgs : EventArgs
        {
            public int id { get; set; }
        }
    };

};