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
            var ret = parser.ParseAddressFromString("Değirmiçem Mh. Muammer Aksoy iş hanı No:63/A Şehitkamil GAZİANTEP 27090");
            Console.Read();
        }
    }
}
