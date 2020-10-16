using System;

namespace GstoreServer
{
    class Program
    {
        static void Main(string[] args)
        {
            GstoreRepository repo = new GstoreRepository();
            repo.Write("1", "1", "teste");
            Console.WriteLine(repo.Read("1", "1"));
            repo.Write("1", "1", "teste2");
            Console.WriteLine(repo.Read("1", "1"));
        }
    }
}
