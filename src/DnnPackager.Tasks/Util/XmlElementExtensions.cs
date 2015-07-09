using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DnnPackager.Tasks.Util
{
    public static class XmlElementExtensions
    {
        public static XElement ElementAnyNamespace(this XContainer root, string localName)
        {
            return root.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
        }
    }
}
