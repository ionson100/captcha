using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Services;

namespace CaptchaIon
{
    public class CaptchaController : Controller
    {
        [WebMethod]
        public object Rfimage(int? id)
        {
            var guid = Guid.NewGuid().ToString().GetHashCode();
            var url = string.Format("/cp/captcha/image/{0}?ass={1}", guid, id);
            return new JavaScriptSerializer().Serialize( new {guid,url});
        }


        public FileStreamResult Image(string id)
        {
            int idcore;
            if (string.IsNullOrEmpty(id) || !int.TryParse(id, out idcore))
                return null;
            var w = int.Parse(ConfigurationManager.AppSettings["CaptchaWidth"]);
            var h = int.Parse(ConfigurationManager.AppSettings["CaptchaHeight"]);
            var length = int.Parse(ConfigurationManager.AppSettings["CaptchaTextLength"]);
            var ass = Request.QueryString["ass"];
            TypeCaptcha typeCaptcha;
            if (!Enum.TryParse(ass, out typeCaptcha))
            {
                typeCaptcha = TypeCaptcha.AllAss;
            }
            var f = new CaptchaImage6 { TextLength = length };
            var text = f.Text;
            var result = new byte[] { };
            switch (typeCaptcha)
            {
                case TypeCaptcha.LuckydAss:
                    var d1 = new CaptchaImage1(text, Color.Bisque, w, h);
                    result = ((IBytesImage)d1).GetBytesImage();
                    break;
                case TypeCaptcha.SimpleAss:
                    var d2 = new CaptchaImage2(text, w, h);
                    result = ((IBytesImage)d2).GetBytesImage();
                    break;

                case TypeCaptcha.MooAss:
                    using (var d3 = new CaptchaImage3(text, w, h))
                    {
                        result = ((IBytesImage)d3).GetBytesImage();
                    }
                    break;
                case TypeCaptcha.CalmAss:
                    var e1 = new CaptchaCharCollection();
                    e1.Add(new CaptchaChar("TimesNewRoman", 45, text));
                    using (var d4 = new CaptchaImage4(w, h, e1)
                                 {
                                     FontBackColor = Color.AntiqueWhite,
                                     BgForeColor = Color.Blue
                                 })
                    {
                        d4.BgForeColor = Color.LightSalmon;
                        result = ((IBytesImage)d4).GetBytesImage();
                    }
                    break;

                case TypeCaptcha.PutinAss:
                    var d5 = new CaptchaImage5 { Height = h, Width = w, Text = text };
                    result = ((IBytesImage)d5).GetBytesImage();
                    break;
                case TypeCaptcha.AllAss:
                    var d6 = new CaptchaImage6
                           {
                               Width = w,
                               Height = h,
                               LineNoise = CaptchaImage6.LineNoiseLevel.Medium,
                               FontWarp = CaptchaImage6.FontWarpFactor.Low,
                               TextLength = length
                           };
                    text = d6.Text;
                    result = ((IBytesImage)d6).GetBytesImage();
                    break;

            }
            if (Session["captcha"] != null)
            {
                ((Dictionary<int, string>)Session["captcha"]).Add(idcore, text);
            }
            else
            {
                var dic = new Dictionary<int, string> { { idcore, text } };
                Session["captcha"] = dic;
            }
            return new FileStreamResult(new MemoryStream(result), "image/png");


        }

        static readonly Lazy<Byte[]> ByteImage = new Lazy<byte[]>(() => UtilsCaptcha.ImageToByte(Properties.Resources.rf), LazyThreadSafetyMode.ExecutionAndPublication); 
     
        public FileStreamResult RefImage()
        {
            return new FileStreamResult(new MemoryStream(ByteImage.Value), "image/png");
        }

        
     
    }
}
