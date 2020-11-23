
using System;
using System.Collections.Generic;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Threading.Tasks;
using OxyPlot.Xamarin.Forms;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using System.Globalization;

namespace AyudanteNewen.Vistas
{
	public partial class EstadisticasProducto
	{
		private readonly string[] _productoString;
		private readonly SpreadsheetsService _servicio;
		private readonly ServiciosGoogle _servicioGoogle;
		private ActivityIndicator _indicadorActividad;

		public EstadisticasProducto(string[] producto, SpreadsheetsService servicio)
		{
			InitializeComponent();
			_servicio = servicio;
			_servicioGoogle = new ServiciosGoogle();
			_productoString = producto;

			InicializarValoresGenerales();
			ObtenerDatosConstruirGrafica();
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

		private void AsignarCampoMovimiento(MovimientoProducto movimiento, string nombreColumna, string valorCelda)
		{
			switch(nombreColumna)
            {
				case "fecha":
					movimiento.Fecha = valorCelda;
					break;
				case "código":
					movimiento.IdProducto = valorCelda;
					break;
				case "producto":
					movimiento.Producto = valorCelda;
					break;
				case "precio":
					movimiento.Precio = valorCelda != "-" ? Convert.ToDouble(valorCelda) : 0;
					break;
				case "stock bajo":
					movimiento.StockBajo = valorCelda != "-" ? Convert.ToDouble(valorCelda) : 0;
					break;
				case "stock":
					if(valorCelda != "-")
						movimiento.Stock = Convert.ToDouble(valorCelda);
					break;
				case "cantidad":
					movimiento.Cantidad = valorCelda != "-" ? Convert.ToDouble(valorCelda) : 0;
					break;
				case "precio total":
					movimiento.PrecioTotal = valorCelda != "-" ? Convert.ToDouble(valorCelda) : 0;
					break;
				case "lugar":
					movimiento.Lugar = valorCelda;
					break;
				case "usuario":
					movimiento.Usuario = valorCelda;
					break;
				case "eliminado":
					movimiento.Eliminado = valorCelda == "sí" ? true : false;
					break;
			}
		}

		private List<MovimientoProducto> ObtenerDatosDesdeHG()
        {
			var linkHistoricosCeldas = CuentaUsuario.ObtenerLinkHojaHistoricosCeldas(CuentaUsuario.ObtenerLinkHojaConsulta());
			CellFeed celdas = _servicioGoogle.ObtenerCeldasDeUnaHoja(linkHistoricosCeldas, _servicio);

			var listaMovimientos = new List<MovimientoProducto>();
			var movimiento = new MovimientoProducto();
			var diccionarioCampos = new Dictionary<uint, string>();

			foreach (CellEntry celda in celdas.Entries)
			{
				var valorCelda = celda.Value.Trim().ToLower();
				if (celda.Row != 1)
				{
					if (celda.Column == 1)
						movimiento = new MovimientoProducto();

					var nombreColumna = diccionarioCampos[celda.Column];
					if (nombreColumna != null)
					{
						AsignarCampoMovimiento(movimiento, nombreColumna, valorCelda);
					}

					if (celda.Column == celdas.ColCount.Count)
						listaMovimientos.Add(movimiento);
				}
				else
				{
					if (!valorCelda.Contains("stock") || valorCelda == "stock bajo")
					{
						diccionarioCampos[celda.Column] = valorCelda;
					}
					else
					{
						diccionarioCampos[celda.Column] = "stock";
					}
				}
			}

			return listaMovimientos;
		}

		private async void ObtenerDatosConstruirGrafica()
		{
			PlotView grafica = null;
			try
			{
				ContenedorMovimientos.Children.Add(_indicadorActividad);
				IsBusy = true;

				await Task.Run(async () => {
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						// Recupera el caché desde CuentaUsuario
						var listaMovimientos = CuentaUsuario.ListaMovimientos ?? ObtenerDatosDesdeHG();
						// Almacena en caché
						// Se purga cada vez que insertamos un movimiento (ServiciosGoogle: EnviarMovimiento - InsertarMovimientosRelaciones)
						CuentaUsuario.ListaMovimientos = listaMovimientos;

						grafica = ConstruirGrafica(listaMovimientos);
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

			if(grafica != null)
			{
				ContenedorMovimientos.Children.Clear();
				ContenedorMovimientos.Children.Add(grafica);
			}
		}

        private PlotView ConstruirGrafica(List<MovimientoProducto> listaMovimientos)
		{
			var listaMovimientosProducto = new List<MovimientoProducto>();

			// Usamos for para ordenar los movimientos por fecha en forma descendente
			foreach (var movimiento in listaMovimientos)
			{
				// Sólo incluimos los movimientos (no eliminados) y con cantidad negativa (egreso - venta) del producto seleccionado
				if (movimiento.IdProducto != _productoString[0] || movimiento.Eliminado || movimiento.Cantidad >= 0) continue;

				listaMovimientosProducto.Add(movimiento);
			}
			
			var model = new PlotModel();
			var ejeX = new DateTimeAxis() {
				Position = AxisPosition.Bottom
			};
			model.Axes.Add(ejeX);

			var ventas = new LineSeries()
			{
				Title = "Ventas",
				Color = OxyColors.MediumSpringGreen,
				MarkerType = MarkerType.Circle,
				MarkerSize = 2,
				MarkerStroke = OxyColors.MediumSpringGreen,
				MarkerFill = OxyColors.MediumSpringGreen,
				MarkerStrokeThickness = 2.5
			};

			var stock = new LineSeries()
			{
				Title = "Stock",
				Color = OxyColors.LightBlue,
				MarkerType = MarkerType.Circle,
				MarkerSize = 2,
				MarkerStroke = OxyColors.LightBlue,
				MarkerFill = OxyColors.LightBlue,
				MarkerStrokeThickness = 2.5
			};			

			var stockBajo = new LineSeries()
			{
				Title = "Stock bajo",
				Color = OxyColors.LightCoral,
				MarkerType = MarkerType.Circle,
				MarkerSize = 2,
				MarkerStroke = OxyColors.LightCoral,
				MarkerFill = OxyColors.LightCoral,
				MarkerStrokeThickness = 2.5
			};

			DateTime fecha;
			for (int i = 0; i < listaMovimientosProducto.Count; i++)
			{
				fecha = DateTime.ParseExact(listaMovimientosProducto[i].Fecha, "dd-MM-yyyy", CultureInfo.InvariantCulture);
				double cantidadVenta = -1 * listaMovimientosProducto[i].Cantidad;
				ventas.Points.Add(new DataPoint(DateTimeAxis.ToDouble(fecha), cantidadVenta));
				stock.Points.Add(new DataPoint(DateTimeAxis.ToDouble(fecha), listaMovimientosProducto[i].Stock - cantidadVenta));
				stockBajo.Points.Add(new DataPoint(DateTimeAxis.ToDouble(fecha), listaMovimientosProducto[i].StockBajo));
			}
			model.Series.Add(ventas);
			model.Series.Add(stock);
			model.Series.Add(stockBajo);

			return new PlotView
			{
				Model = model,
				VerticalOptions = LayoutOptions.FillAndExpand,
				HorizontalOptions = LayoutOptions.FillAndExpand
			};
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
}
