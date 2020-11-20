using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace AyudanteNewen.Droid
{
	[Activity(Label = "Ayudante Newen", Icon = "@drawable/icon", Theme = "@style/MainTheme", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
	public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
	{
		protected override void OnCreate(Bundle bundle)
		{
			TabLayoutResource = Resource.Layout.Tabbar;
			ToolbarResource = Resource.Layout.Toolbar;

			base.OnCreate(bundle);

			ZXing.Net.Mobile.Forms.Android.Platform.Init();
			Xamarin.Forms.Forms.Init(this, bundle);
			OxyPlot.Xamarin.Forms.Platform.Android.PlotViewRenderer.Init();

			App.AlmacenarAnchoPantalla(Resources.DisplayMetrics.Density, Resources.DisplayMetrics.WidthPixels, Resources.DisplayMetrics.HeightPixels);

			LoadApplication(new App());
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			ZXing.Net.Mobile.Forms.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}

	}

}