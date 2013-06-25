using System.Runtime.Serialization;

namespace AddParserSrv.Classes
{
   [DataContract]
   public class AddressDT
   {
       [DataMember]
       public string Mahalle { get; set; }
       [DataMember]
       public string Sokak { get; set; }
       [DataMember]
       public string Cadde { get; set; }
       [DataMember]
       public string Site { get; set; }
       [DataMember]
       public string Blok { get; set; }
       [DataMember]
       public string Bina { get; set; }
       [DataMember]
       public string Bulv { get; set; }
       [DataMember]
       public string PostaKodu { get; set; }
       [DataMember]
       public string Il { get; set; }
       [DataMember]
       public string Ilce { get; set; }
       [DataMember]
       public string No { get; set; }
       [DataMember]
       public string Kat { get; set; }
       [DataMember]
       public string Daire { get; set; }
       [DataMember]
       public string Bolge { get; set; }
   }

}