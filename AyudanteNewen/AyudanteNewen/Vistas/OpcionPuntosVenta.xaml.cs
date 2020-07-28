using System;
using Google.GData.Client;
using Google.GData.Spreadsheets;
using AyudanteNewen.Clases;
using Xamarin.Forms;

namespace AyudanteNewen.Vistas
{
	public partial class OpcionPuntosVenta
	{
		private double _anchoActual;
		private readonly SpreadsheetsService _servicio;
		private readonly AtomEntryCollection _listaHojas;

		public OpcionPuntosVenta(SpreadsheetsService servicio, AtomEntryCollection listaHojas)
		{
			InitializeComponent();
			_servicio = servicio;
			_listaHojas = listaHojas;
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
		}

		[Android.Runtime.Preserve]
		private void SeleccionarHojaPtosVtas(object sender, EventArgs args)
		{
			var pagina = new ListaHojasPtosVtaGoogle(_servicio, _listaHojas);
			Navigation.PushAsync(pagina, true);
		}

		[Android.Runtime.Preserve]
		private void IrSeleccionColumnas(object sender, EventArgs args)
		{
			//Si no configuro Puntos de venta lo saco de la memoria para que no muestre el campo en Productos.
			CuentaUsuario.RemoverValorEnCuentaLocal("puntosVenta");

			ContentPage pagina = new SeleccionColumnasParaVer(_servicio);
			Navigation.PushAsync(pagina, true);
		}

		protected override void OnSizeAllocated(double ancho, double alto)
		{
			base.OnSizeAllocated(ancho, alto);
			if (_anchoActual == ancho) return;
			SombraEncabezado.WidthRequest = ancho > alto ? App.AnchoApaisadoDePantalla : App.AnchoRetratoDePantalla;
			_anchoActual = ancho;
		}
	}
}
