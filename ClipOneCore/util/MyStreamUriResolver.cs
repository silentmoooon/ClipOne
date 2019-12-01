using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipOne.util
{
    public sealed class MyStreamUriResolver : IUriToStreamResolver
    {
     
        public Stream UriToStream(Uri uri)
        {
             
            return GetContent(uri);
        }

       

        private Stream GetContent(Uri uri)
        {
           
            string path = uri.AbsolutePath;

            Console.WriteLine(path);
            string str = File.ReadAllText(path.ReplaceFirst("/",""));                  
            byte[] array = Encoding.ASCII.GetBytes(str);
            MemoryStream stream = new MemoryStream(array);
            return stream;
        }
    }
}
