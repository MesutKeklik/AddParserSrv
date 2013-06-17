using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AddParserSrv;

namespace ServiceTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new AddressParserSRV();
            var ret = parser.ParseAddressFromString("Büyükdere Caddesi No 245 USO Center Plaza Maslak, İstanbul");
            Console.Read();
        }
    }
}
