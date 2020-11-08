
using System;
using AyudanteNewen.Clases;
using Xamarin.Forms;
using AyudanteNewen.Vistas;

namespace AyudanteNewen
{
	public partial class App
	{
		public static double AnchoRetratoDePantalla;
		public static double AltoRetratoDePantalla;
		public static double AnchoApaisadoDePantalla;
		public static double AltoApaisadoDePantalla;
		public static bool EstaApaisado;
		public const string RutaImagenSombraEncabezado = "AyudanteNewen.Imagenes.sombraEncabezado.png";
		public static App Instancia;

		public App()
		{
			InitializeComponent();
			Instancia = this;

			var columnasParaVer = CuentaUsuario.ObtenerColumnasParaVer();
			var columnasInventario = CuentaUsuario.ObtenerColumnasInventario();

			switch (CuentaUsuario.ObtenerAccesoDatos())
			{
				case "G":
					AccesoHojaDeCalculoGoogle(columnasParaVer, columnasInventario);
					break;
				case "B":
					AccesoBaseDeDatos(columnasParaVer, columnasInventario);
					break;
				default:
					MainPage = new NavigationPage(new AccesoDatos());
					break;
			}
		}

		private void AccesoHojaDeCalculoGoogle(string columnasParaVer, string columnasInventario)
		{
			var linkHojaConsulta = CuentaUsuario.ObtenerLinkHojaConsulta();

			if (string.IsNullOrEmpty(linkHojaConsulta) || string.IsNullOrEmpty(columnasParaVer) ||
					string.IsNullOrEmpty(columnasInventario) ||
					!CuentaUsuario.ValidarTokenDeGoogle())
				MainPage = new NavigationPage(new PaginaAuntenticacion(!string.IsNullOrEmpty(linkHojaConsulta) &&
																															 !string.IsNullOrEmpty(columnasParaVer) &&
																															 !string.IsNullOrEmpty(columnasInventario)));
			else
				MainPage = new NavigationPage(new PaginaGrilla(linkHojaConsulta, null));
		}

		private void AccesoBaseDeDatos(string columnasParaVer, string columnasInventario)
		{
			if (string.IsNullOrEmpty(columnasParaVer) || string.IsNullOrEmpty(columnasInventario))
				MainPage = new NavigationPage(new PaginaConexionBaseDeDatos());
			else
				MainPage = new NavigationPage(new PaginaGrilla());
		}

		[Android.Runtime.Preserve]
		public static void AlmacenarAnchoPantalla(double densidad, int anchoEnPixel, int altoEnPixel)
		{
			EstaApaisado = anchoEnPixel > altoEnPixel;
			AnchoRetratoDePantalla = EstaApaisado ? altoEnPixel / densidad : anchoEnPixel / densidad;
			AnchoApaisadoDePantalla = EstaApaisado ? anchoEnPixel / densidad : altoEnPixel / densidad;
		}

		public Image ObtenerImagen(TipoImagen tipoImagen)
		{
			LayoutOptions alineacionHorizontal;
			ImageSource fuenteArchivo;
			var altoImagen = AnchoRetratoDePantalla * .20555; //Por defecto, el valor de los botones

			switch (tipoImagen)
			{
				case TipoImagen.BotonAccesoDatos:
					alineacionHorizontal = LayoutOptions.EndAndExpand;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.accesoDatos.png");
					break;
				case TipoImagen.BotonRefrescarDatos:
					alineacionHorizontal = LayoutOptions.Center;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.refrescarDatos.png");
					break;
				case TipoImagen.BotonEscanearCodigo:
					alineacionHorizontal = LayoutOptions.StartAndExpand;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.escanearCodigo.png");
					break;
				case TipoImagen.BotonVolver:
					alineacionHorizontal = LayoutOptions.EndAndExpand;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.volver.png");
					break;
				case TipoImagen.BotonMovimientos:
					alineacionHorizontal = LayoutOptions.Center;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.movimientos.png");
					break;
				case TipoImagen.BotonGuardarCambios:
					alineacionHorizontal = LayoutOptions.StartAndExpand;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.guardarCambios.png");
					break;
				case TipoImagen.BotonListo:
					alineacionHorizontal = LayoutOptions.Center;
					fuenteArchivo = ImageSource.FromResource("AyudanteNewen.Imagenes.listo.png");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(tipoImagen), tipoImagen, null);
			}

			return new Image
			{
				HorizontalOptions = alineacionHorizontal,
				Source = fuenteArchivo,
				HeightRequest = altoImagen
			};
		}

		public void LimpiarNavegadorLuegoIrPagina(ContentPage pagina)
		{
			MainPage = new NavigationPage(pagina);
		}

	}

	public enum TipoImagen
	{
		BotonAccesoDatos,
		BotonRefrescarDatos,
		BotonEscanearCodigo,
		BotonVolver,
		BotonMovimientos,
		BotonGuardarCambios,
		BotonListo,
		SombraEncabezado
	}
}
