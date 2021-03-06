﻿
using System;
using System.Collections.Generic;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Threading.Tasks;

namespace AyudanteNewen.Vistas
{
	public partial class ProductoMovimientos
	{
		private readonly string[] _productoString;
		private readonly SpreadsheetsService _servicio;
		private CellFeed _celdas;
		private readonly ServiciosGoogle _servicioGoogle;
		private string[] _nombresColumnas;
		private ActivityIndicator _indicadorActividad;

		public ProductoMovimientos(string[] producto, SpreadsheetsService servicio)
		{
			InitializeComponent();
			_servicio = servicio;
			_servicioGoogle = new ServiciosGoogle();
			_productoString = producto;

			InicializarValoresGenerales();
			ObtenerDatosMovimientosDesdeHCG();
		}

		public ProductoMovimientos(string[] productoBD)
		{
			InitializeComponent();
			_productoString = productoBD;

			InicializarValoresGenerales();
		}

		private void InicializarValoresGenerales()
		{
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);

			if (_productoString.Length > 1)
			{
				Titulo.Text += " " + _productoString[1];
				Titulo.FontSize = Titulo.Text?.Length < 38
					? 18 
					: Titulo.Text?.Length < 45
						? 16
						: 15;
			}

			_indicadorActividad = new ActivityIndicator
			{
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = true,
				BindingContext = this
			};
			_indicadorActividad.SetBinding(IsVisibleProperty, "IsBusy");
			_indicadorActividad.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
		}

		private async void ObtenerDatosMovimientosDesdeHCG()
		{
			try
			{
				ContenedorMovimientos.Children.Add(_indicadorActividad);
				IsBusy = true;

				await Task.Run(async () => {
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						var linkHistoricosCeldas = CuentaUsuario.ObtenerLinkHojaHistoricosCeldas(CuentaUsuario.ObtenerLinkHojaConsulta());
						_celdas = _servicioGoogle.ObtenerCeldasDeUnaHoja(linkHistoricosCeldas, _servicio);
					}
					else
					{
						//Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token
						var paginaAuntenticacion = new PaginaAuntenticacion(true);
						Navigation.InsertPageBefore(paginaAuntenticacion, this);
						await Navigation.PopAsync();
					}
				});
			}
			finally
			{
				IsBusy = false; //Remueve el Indicador de Actividad.
			}

			_nombresColumnas = new string[_celdas.ColCount.Count];

			var movimientos = new List<string[]>();
			var movimiento = new string[_celdas.ColCount.Count];

			foreach (CellEntry celda in _celdas.Entries)
			{
				if (celda.Row != 1)
				{
					if (celda.Column == 1)
						movimiento = new string[_celdas.ColCount.Count];

					movimiento.SetValue(celda.Value, (int)celda.Column - 1);

					if (celda.Column == _celdas.ColCount.Count)
						movimientos.Add(movimiento);
				}
				else
				{
					_nombresColumnas.SetValue(celda.Value, (int)celda.Column - 1);
				}
			}

			LlenarGrillaDeMovimientos(movimientos);
		}

		private void LlenarGrillaDeMovimientos(List<string[]> movimientos)
		{
			var esTeclaPar = false;
			var listaMovimientos = new List<ClaseMovimiento>();

			//Usamos for para ordenar los movimientos por fecha en forma descendente
			for (var indice = movimientos.Count - 1; indice >= 0; indice--)
			{
				var datosMovimiento = movimientos[indice];
				//Sólo incluimos los movimientos (no eliminados) del producto seleccionado
				if (datosMovimiento[1] != _productoString[0] || datosMovimiento[datosMovimiento.Length - 2] == "Sí") continue;

				var datosParaVer = new List<string>();
				var i = -1;
				foreach (var dato in datosMovimiento)
				{
					i += 1;
					//No incluimos Código y Nombre del producto, tampoco Eliminado y Eliminado Por porque son los movimientos (no eliminados) del producto seleccionado.
					if (i == 1 || i == 2 || i == datosMovimiento.Length - 2 || i == datosMovimiento.Length - 1) continue;

					datosParaVer.Add(_nombresColumnas[i] + " : " + dato);
				}

				var movimiento = new ClaseMovimiento(indice + 2, datosParaVer, esTeclaPar);
				listaMovimientos.Add(movimiento);
				esTeclaPar = !esTeclaPar;
			}

			var anchoColumnaEliminar = App.AnchoRetratoDePantalla / 6;
			var anchoColumnaDatos = anchoColumnaEliminar * 5;
			var vista = new ListView
			{
				RowHeight = 100,
				VerticalOptions = LayoutOptions.StartAndExpand,
				HorizontalOptions = LayoutOptions.Fill,
				ItemsSource = listaMovimientos,
				ItemTemplate = new DataTemplate(() =>
				{
					// Datos
					var datos = new Label
					{
						FontSize = 15,
						TextColor = Color.FromHex("#1D1D1B"),
						VerticalOptions = LayoutOptions.CenterAndExpand,
						WidthRequest = anchoColumnaDatos
					};
					datos.SetBinding(Label.TextProperty, "Datos");

					// Separador
					var separador = new BoxView
					{
						WidthRequest = 2,
						BackgroundColor = Color.FromHex("#FFFFFF"),
						HeightRequest = 55
					};

					// Botón eliminar
					var etiquetaIconoEliminar = new Label
					{
						FontFamily = "FontAwesome5Solid.otf#Regular",
						HorizontalTextAlignment = TextAlignment.Center,
						FontSize = 22,
						TextColor = Color.FromHex("#ffffff"),
						VerticalTextAlignment = TextAlignment.Center,
						VerticalOptions = LayoutOptions.EndAndExpand,
						Text = "\uf2ed"
					};

					var etiquetaEliminar = new Label
					{
						HorizontalTextAlignment = TextAlignment.Center,
						FontSize = 14,
						TextColor = Color.FromHex("#ffffff"),
						VerticalTextAlignment = TextAlignment.Center,
						VerticalOptions = LayoutOptions.StartAndExpand,
						Text = "Eliminar"
					};

					var contenedorEliminar = new StackLayout
					{
						WidthRequest = anchoColumnaEliminar,
						Orientation = StackOrientation.Vertical,
						Children = { etiquetaIconoEliminar, etiquetaEliminar },
						BackgroundColor = Color.FromHex("#FD8A18")
					};
					contenedorEliminar.SetBinding(ClassIdProperty, "IdMovimiento");
					contenedorEliminar.GestureRecognizers.Add(new TapGestureRecognizer(EliminarMovimiento));

					// Teja
					var tecla = new StackLayout
					{
						Padding = 2,
						Spacing = 2,
						Orientation = StackOrientation.Horizontal,
						Children = { datos, separador, contenedorEliminar }
					};
					tecla.SetBinding(BackgroundColorProperty, "ColorFondo");

					return new ViewCell { View = tecla };

				})
			};

			ContenedorMovimientos.Children.Clear();
			ContenedorMovimientos.Children.Add(vista);
		}

		private void RefrescarUIGrilla()
		{
			//Se quita la grilla para recargarla.
			ContenedorMovimientos.Children.Clear();
			ContenedorMovimientos.Children.Add(_indicadorActividad);
			IsBusy = true;
		}

		private async void EliminarMovimiento(View boton, object e)
		{
			var tacho = (StackLayout)boton;
			var fila = Convert.ToInt32(tacho.ClassId);

			tacho.BackgroundColor = Color.FromHex("#FB9F0B");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				tacho.BackgroundColor = Color.FromHex("#FD8A18");
				return false;
			});

			if(!await DisplayAlert("Movimiento", "¿Desea eliminar el movimiento seleccionado?", "Sí", "No")) return;

			RefrescarUIGrilla();

			foreach (CellEntry celda in _celdas.Entries)
			{
				if (celda.Row != fila) continue;

				if (celda.Column == _celdas.ColCount.Count - 1 || celda.Column == _celdas.ColCount.Count)
				{
					celda.InputValue = celda.Column == _celdas.ColCount.Count - 1 ? "Sí" : CuentaUsuario.ObtenerNombreUsuarioGoogle() ?? "-";
					celda.Update();
				}
			}

			ObtenerDatosMovimientosDesdeHCG();
		}

		[Android.Runtime.Preserve]
		private void Listo(object sender, EventArgs e)
		{
			BotonListo.BackgroundColor = Color.FromHex("#32CEF9");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				Navigation.PopAsync();
				BotonListo.BackgroundColor = Color.FromHex("#32BBF9");
				return false;
			});
		}
	}

	//Clase Movimiento: utilizada para armar la lista scrolleable de movimientos
	[Android.Runtime.Preserve]
	public class ClaseMovimiento
	{
		[Android.Runtime.Preserve]
		public ClaseMovimiento(int idMovimiento, IList<string> datos, bool esTeclaPar)
		{
			IdMovimiento = idMovimiento;
			Datos = string.Join(" | ", datos);
			ColorFondo = esTeclaPar ? Color.FromHex("#EDEDED") : Color.FromHex("#E2E2E1");
		}

		[Android.Runtime.Preserve]
		public int IdMovimiento { get; }
		[Android.Runtime.Preserve]
		public string Datos { get; }
		[Android.Runtime.Preserve]
		public Color ColorFondo { get; }
	}
}
