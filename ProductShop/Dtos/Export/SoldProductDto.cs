
namespace ProductShop.Dtos.Export
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [XmlType("SoldProducts")]
    public class SoldProductDto
    {
        [XmlElement("count")] 
        public int Count { get; set; }

        [XmlArray("products")]
        public List<ProductExportDto> Products { get; set; }
    }
}
