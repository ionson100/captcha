using System.Collections.Generic;
using System.Web;

namespace CaptchaIon
{
  public   class Captcha
    {
      /// <summary>
      /// Символы с клиента
      /// </summary>
        public string Captchavalue  {  get; set; }
      /// <summary>
      /// Ключь  для строки на сервере
      /// </summary>
        public string Captchakey { get; set; }
      /// <summary>
      /// Валидность ввода с клиента
      /// </summary>
      public bool IsValid
      {
          get {
              if (HttpContext.Current.Session["captcha"] == null) return false;
              if (!((Dictionary<int, string>) HttpContext.Current.Session["captcha"]).ContainsKey(int.Parse(Captchakey)))
                  return false;
              return ((Dictionary<int, string>) HttpContext.Current.Session["captcha"])[int.Parse(Captchakey)] ==
                     Captchavalue;
          }
      }
    }
}
