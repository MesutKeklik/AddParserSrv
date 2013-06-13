using System.ServiceModel;
using System.ServiceModel.Web;
using AddParserSrv.Classes;

namespace AddParserSrv
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface IAddressParserSRV
    {
        [WebGet(UriTemplate = "/parseAddress?adr={irregularAddress}")]
        [OperationContract]
        AddressDT ParseAddressFromString(string irregularAddress);

    }
}
