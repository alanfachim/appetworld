using System;

namespace Carimbador
{
    class Program
    {
        static void Main(string[] args)
        {
            Negocio n = new Negocio();
            n.Processar();
            Console.WriteLine("Hello World!");
            var db = new DAO();
            var a = db.getGrupParameters("0001", null).Result;
            var b = db.buscaRegras("51;1=1;2=1;", null).Result;
            var c = db.buscaPontoCurva("1111", "20210505", "210507", null).Result;

            
        }
    }
}
