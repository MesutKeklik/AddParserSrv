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
            var ret = parser.ParseAddressFromString(
                "Yıldız mah. Asariye cad. Sinanpasa mescidi sok. Yıldız apt., no:1 d:4 Cıragan/Besiktas Beşiktaş (Yıldız),İSTANBUL ".ToLower());
            Console.Read();
        }
    }
}
