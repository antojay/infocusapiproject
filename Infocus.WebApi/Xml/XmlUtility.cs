using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml;

namespace Infocus.Common.Xml
{
    public abstract class XmlUtility
    {
        public static T ParseXmlObject<T>(String str)
        {
            T t = default(T);

            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using(StringReader reader = new StringReader(str))
            {
                t = (T)serializer.Deserialize(System.Xml.XmlReader.Create(reader));
            }

            return t;
        }

        public static String Serialize(Object obj)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            using(StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.ToString();
            }
        }
    }
}
