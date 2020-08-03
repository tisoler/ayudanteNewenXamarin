
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
            Control.Settings.UserAgentString = Control.Settings.UserAgentString.Replace("; wv","");
        }
    }
}