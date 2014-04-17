using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace CaptchaIon
{
   static class UtilsCaptcha
    {
       public static byte[] ImageToByte(Image img)
       {
           if (img == null) return null;
           var converter = new ImageConverter();
           return (byte[])converter.ConvertTo(img, typeof(byte[]));
       }
    }
}
