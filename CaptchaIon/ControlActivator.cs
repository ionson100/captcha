using System;
using System.Configuration;
using System.Globalization;
using System.Web.Mvc;

namespace CaptchaIon
{
    public static class ControlActivator
    {
        private static bool _activateCaptchaRouter;
        /// <summary>
        /// Полезная нагрузка, вызов капчи
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="ass"> тип генераци картинки</param>
        /// <param name="textButton">то что мы видим на кнопке</param>
        /// <returns></returns>
        public static MvcHtmlString Captchaion(this HtmlHelper htmlHelper, TypeCaptcha ass, string textButton)
        {
            if (_activateCaptchaRouter == false)
            {


                htmlHelper.RouteCollection.MapRoute("captcha", "cp/{controller}/{action}/{id}",
                                               namespaces: new[] { typeof(CaptchaController).Namespace },
                                               defaults: new { controller = " Captcha", action = "Image", id = UrlParameter.Optional });
                _activateCaptchaRouter = true;
            }
            return MvcHtmlString.Create(GreateCaptcha(htmlHelper, ass, textButton));
        }

        internal static string GreateCaptcha(HtmlHelper htmlHelper, TypeCaptcha ass, string textButton)
        {
            //<add key="CaptchaWidth" value="200" />
            //<add key="CaptchaHeight" value="50" />
            //<add key="CaptchaTextLength" value="5" />
            //<add key="ErrorText" value="Количество символов должно быть -5" />
            //<add key="AltUrlRefImage" value=""


            var guid = Guid.NewGuid().ToString().GetHashCode();
            var url = string.Format("/cp/captcha/image/{0}?ass={1}", guid, (int)ass);

            return Properties.Resources.CaptchaionHtml.
                Replace("#altimg#", ConfigurationManager.AppSettings["AltUrlRefImage"] ?? "/cp/captcha/RefImage/3").
                Replace("#errortext#", ConfigurationManager.AppSettings["ErrorText"]).
            Replace("#url#", url.ToString(CultureInfo.InvariantCulture)).
            Replace("#key#", guid.ToString(CultureInfo.InvariantCulture)).
            Replace("#text#", textButton).
            Replace("#ass#", ((int)ass).ToString(CultureInfo.InvariantCulture)).
            Replace("#length#", ConfigurationManager.AppSettings["CaptchaTextLength"]).
            Replace("#error#", string.Empty);
        }

    }
}
