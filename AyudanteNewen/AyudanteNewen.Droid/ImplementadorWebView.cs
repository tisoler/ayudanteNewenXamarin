
//BORRAR
using AyudanteNewen.Droid;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(WebView), typeof(ImplementadorWebView))]

namespace AyudanteNewen.Droid
{
    public class ImplementadorWebView : WebViewRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<WebView> e)
        {
            base.OnElementChanged(e);

            Control.Settings.UserAgentString = "Mozilla/5.0 (X11; U; Linux i686; en-US; rv:1.9.0.4) Gecko/20100101 Firefox/4.0";
        }
    }
}