
using AyudanteNewen.Clases;
using AyudanteNewen.Vistas;
using System;
using System.Net.Http;
using Xamarin.Forms;

namespace AyudanteNewen
{
	public partial class PaginaAuntenticacion
	{
		private readonly string _clientId = "898150154619-oijkkc2eqo5lun5qbr2nvv7vo58gn4rs.apps.googleusercontent.com";
		private readonly bool _conexionExistente;

		public PaginaAuntenticacion(bool conexionExistente = false)
		{
			InitializeComponent();
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			CuentaUsuario.AlmacenarAccesoDatos("G");
			// Si es verdadero debe llevarnos a la Grilla en lugar de avanzar hacia la página de selección de libros
			_conexionExistente = conexionExistente;

			var webView = new WebView
			{
				VerticalOptions = LayoutOptions.FillAndExpand
			};
			webView.Navigated += CuandoNavegaWebView;

			if (!CuentaUsuario.ValidarTokenDeGoogle())
			{
				var solicitud =
					"https://accounts.google.com/o/oauth2/auth?client_id=" + _clientId
					+ "&scope=https://www.googleapis.com/auth/drive https://spreadsheets.google.com/feeds https://www.googleapis.com/auth/plus.login"
					+ "&token_uri=https://accounts.google.com/o/oauth2/token"
					+ "&response_type=token&redirect_uri=http://localhost";

				webView.Source = solicitud;
			}
			else
			{
				webView.Source = "http://localhost/#access_token=" + CuentaUsuario.ObtenerTokenActualDeGoogle() +
				                 "&token_type=&expires_in=&noActualizarFecha";
			}

			Contenedor.Children.Add(webView);
		}

		private void CuandoNavegaWebView(object sender, WebNavigatedEventArgs e)
		{
			ExtraerTokenAccesoDesdeUrl(e.Url);
		}

		private async void DeterminarProcesoParaCargaDatos(string tokenDeAcceso)
		{
			ContentPage pagina;

			if (_conexionExistente) // Si es verdadero debe llevarnos a la Grilla en lugar de avanzar hacia la página de selección de libros
			{
				var linkHojaConsulta = CuentaUsuario.ObtenerLinkHojaConsulta();
				App.Instancia.LimpiarNavegadorLuegoIrPagina(new PaginaGrilla(linkHojaConsulta, null));
			}
			else
			{
				pagina = new ListaLibrosGoogle(tokenDeAcceso);
				Navigation.InsertPageBefore(pagina, this);
				await Navigation.PopAsync();
			}
		}

		private void ExtraerTokenAccesoDesdeUrl(string url)
		{
			if (url.Contains("access_token") && url.Contains("&expires_in="))
			{
				Content = null;

				var at = url.Replace("http://localhost/#access_token=", "");

				if (Device.OS == TargetPlatform.WinPhone || Device.OS == TargetPlatform.Windows) //VER
				{
					at = url.Replace("http://localhost/#access_token=", "");
				}

				if (!url.Contains("&noActualizarFecha"))
				{
					//Expira en 1 hora, por las dudas, lo actualizamos a los 55 minutos para evitar potencial desfasaje en el horario del servidor.
					var fechaExpiracion = DateTime.Now.AddMinutes(55);
					CuentaUsuario.AlmacenarFechaExpiracionToken(fechaExpiracion);
				}
				var tokenDeAcceso = at.Remove(at.IndexOf("&token_type="));

				CuentaUsuario.AlmacenarTokenDeGoogle(tokenDeAcceso);

				//Recuperar el nombre de usuario para el historial de movimientos
				if(string.IsNullOrEmpty(CuentaUsuario.ObtenerNombreUsuarioGoogle()))
					RecuperarNombreUsuarioGoogle(tokenDeAcceso);
				//A partir de la procedencia determinar si irá hacia la página de grilla o hacia la de libros
				DeterminarProcesoParaCargaDatos(tokenDeAcceso);
			}
		}

		private static async void RecuperarNombreUsuarioGoogle(string tokenDeAcceso)
		{
			var url = @"https://www.googleapis.com/oauth2/v1/userinfo?access_token=" + tokenDeAcceso;

			using (var cliente = new HttpClient())
			{
				var usuario = await cliente.GetStringAsync(url);
				usuario = usuario.Substring(usuario.IndexOf("\"name\": \"") + 9);
				usuario = usuario.Remove(usuario.IndexOf("\",\n"));
				CuentaUsuario.AlmacenarNombreUsuarioGoogle(usuario);
			}
		}
	}
}
