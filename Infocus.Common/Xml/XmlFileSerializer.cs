using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Infocus.Common.Cryptography;
namespace Infocus.Common.Xml
{
    public static class XmlFileSerializer
    {
        public static void Serialize(Object obj, String filePath)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            using (TextWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, obj);
                writer.Close();
            }
            
        }

        public static T Deserialize<T>(String filePath)
        {
            Object obj;
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using(TextReader reader = new StreamReader(filePath))
            {
                obj = serializer.Deserialize(reader);
            }
            if(obj == null)
            {
                return default(T);
            }
            return (T)obj;
        }

        public static void SerializeEncryptedAes(Object obj, String filePath, String passPhrase, String saltValue)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            String str = SerializeEncryptedAesToString(obj, passPhrase, saltValue);

            using(TextWriter textWriter = new StreamWriter(filePath))
            {
                textWriter.Write(str);
                textWriter.Close();
            }
        }

        public static String SerializeEncryptedAesToString(Object obj, String passPhrase, String saltValue)
        {
            XmlSerializer serializer = new XmlSerializer(obj.GetType());
            String str = null;
            using(StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                str = AesCryptographer.Encrypt(writer.ToString(), passPhrase, saltValue);
            }

            return str;
        }

        public static T DeserializeEncryptedAes<T>(String filePath, String passPhrase, String saltValue)
        {
            using(TextReader reader = new StreamReader(filePath))
            {
                return DeserializeEncryptedAes<T>(reader, passPhrase, saltValue);
            }
        }

        public static T DeserializeEncryptedAes<T>(TextReader reader, String passPhrase, String saltValue)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            String str = reader.ReadToEnd();
            Object obj = null;
            str = AesCryptographer.Decrypt(str, passPhrase, saltValue);
            using(StringReader strReader = new StringReader(str))
            {
                obj = serializer.Deserialize(strReader);
            }
            if(obj == null)
            {
                return default(T);
            }
            return (T)obj;
        }
    }
}
