
using System;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Net.Http;
using System.Threading.Tasks;

namespace AyudanteNewen.Vistas
{
	public partial class Producto
	{
		private bool[] _signoPositivo;
		private double[] _cantidades;
		private double[] _precios;
		private string[] _lugares;
		private string _comentario;
		private readonly CellEntry[] _producto;
		private string[] _listaColumnasInventario;
		private string[] _listaLugares;
		private readonly string[] _productoString;
		private readonly SpreadsheetsService _servicio;
		private readonly string[] _nombresColumnas;
		private string _mensaje = "";
		private ActivityIndicator _indicadorActividad;
		private Image _volver;
		private Image _movimientos;
		private Image _guardarCambios;

		public Producto(CellEntry[] producto, string[] nombresColumnas, SpreadsheetsService servicio, string titulo)
		{
			InitializeComponent();
			_nombresColumnas = nombresColumnas;
			// Almacenar el arreglo de strings para obtener el nivel de stock y para cargar el producto en pantalla
			_productoString = ObtenerArregloCeldasProducto(producto);
			InicializarValoresGenerales();
			_producto = producto;
			_servicio = servicio;
			Titulo.Text += " " + titulo.Replace("App", "").Replace("es ", " ").Replace("s ", " ");

			ConstruirVistaDeProducto();
		}

		private string[] ObtenerArregloCeldasProducto(CellEntry[] producto)
        {
			var productoString = new string[producto.Length];
			var i = 0;
			foreach (var celda in producto)
			{
				productoString.SetValue(celda.Value, i);
				i += 1;
			}
			return productoString;
		}

		public Producto(string[] productoBD, string[] nombresColumnas)
		{
			InitializeComponent();
			_nombresColumnas = nombresColumnas;
			_productoString = productoBD;
			InicializarValoresGenerales();

			ConstruirVistaDeProducto();
		}

		private void InicializarValoresGenerales()
		{
			var columnasInventario = CuentaUsuario.ObtenerColumnasInventario();
			_listaColumnasInventario = !string.IsNullOrEmpty(columnasInventario) ? _listaColumnasInventario = columnasInventario.Split(',') : null;

			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			IndicadorBajoStock.IsVisible = EsBajoStock();

			ConfigurarBotones();

			_indicadorActividad = new ActivityIndicator
			{
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = true,
				BindingContext = this
			};
			_indicadorActividad.SetBinding(IsVisibleProperty, "IsBusy");
			_indicadorActividad.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
		}

        private bool EsBajoStock()
        {
			int i = 0;
			decimal stockTotalProducto = 0;
			decimal nivelStockMinimo = 0;

			foreach (var dato in _productoString)
			{
				if (_listaColumnasInventario != null && _listaColumnasInventario[i] == "1")
				{
					stockTotalProducto += Convert.ToDecimal(dato);
				}
				if (_nombresColumnas[i]?.ToLower() == "stock bajo")
				{
					nivelStockMinimo = Convert.ToDecimal(dato);
				}
				i += 1;
			}
			return stockTotalProducto <= nivelStockMinimo;
		}

        private void ConfigurarBotones()
		{
			_volver = App.Instancia.ObtenerImagen(TipoImagen.BotonVolver);
			_volver.GestureRecognizers.Add(new TapGestureRecognizer
				{
					Command = new Command(Volver),
					NumberOfTapsRequired = 1
				}
			);
			_movimientos = App.Instancia.ObtenerImagen(TipoImagen.BotonMovimientos);
			_movimientos.GestureRecognizers.Add(new TapGestureRecognizer
				{
					Command = new Command(AccederMovimientos),
					NumberOfTapsRequired = 1
				}
			);
			_guardarCambios = App.Instancia.ObtenerImagen(TipoImagen.BotonGuardarCambios);
			_guardarCambios.GestureRecognizers.Add(new TapGestureRecognizer
				{
					Command = new Command(EventoGuardarCambios),
					NumberOfTapsRequired = 1
				}
			);

			ContenedorBotones.Children.Add(_volver);
			ContenedorBotones.Children.Add(_movimientos);
			ContenedorBotones.Children.Add(_guardarCambios);
		}

		private void ConstruirVistaDeProducto()
		{
			//Obtener, si existen, los puntos de venta.
			var puntosVentaTexto = CuentaUsuario.ObtenerPuntosVenta();
			_listaLugares = null;
			if (!string.IsNullOrEmpty(puntosVentaTexto))
				_listaLugares = puntosVentaTexto.Split('|');

			_signoPositivo = new bool[_productoString.Length];
			_cantidades = new double[_productoString.Length];
			_precios = new double[_productoString.Length];
			_lugares = new string[_productoString.Length];
			var i = 0;

			var anchoEtiqueta = App.AnchoRetratoDePantalla / 3 - 10;
			var anchoCampo = App.AnchoRetratoDePantalla / 3 * 2 - 30;

			Label nombreCampo;
			StackLayout campoValor;

			#region Campos de planilla
			foreach (var celda in _productoString)
			{
				if (celda != null)
				{
					#region Datos en planilla o BD

					nombreCampo = new Label
					{
						HorizontalOptions = LayoutOptions.EndAndExpand,
						VerticalOptions = LayoutOptions.Center,
						HorizontalTextAlignment = TextAlignment.End,
						FontSize = 16,
						WidthRequest = anchoEtiqueta,
						Text = _nombresColumnas[i],
						TextColor = Color.Black
					};

					var valorCampo = new Entry
					{
						HorizontalOptions = LayoutOptions.CenterAndExpand,
						VerticalOptions = LayoutOptions.Center,
						HorizontalTextAlignment = TextAlignment.Start,
						WidthRequest = anchoCampo,
						IsEnabled = false,
						Text = celda,
						TextColor = Color.Black
					};

					campoValor = new StackLayout
					{
						VerticalOptions = LayoutOptions.Start,
						HorizontalOptions = LayoutOptions.Fill,
						Orientation = StackOrientation.Horizontal,
						HeightRequest = 50,
						Children = { nombreCampo, valorCampo }
					};

					ContenedorProducto.Children.Add(campoValor);

					#endregion

					//Si es columna de stock agrega el campo Cantidad. Si tiene lugar de compra/venta agrega los campos Precio total y Lugar.
					if (!string.IsNullOrEmpty(celda) && _listaColumnasInventario != null && _listaColumnasInventario[i] == "1")
					{
						#region Movimiento stock

						_signoPositivo.SetValue(true, i);

						nombreCampo = new Label
						{
							HorizontalOptions = LayoutOptions.EndAndExpand,
							VerticalOptions = LayoutOptions.Center,
							HorizontalTextAlignment = TextAlignment.End,
							//Si no hay lugares no hay campo PrecioTotal, el campo Cantidad toma esa etiqueta.
							Text = _listaLugares != null ? "Cantidad" : "Monto Total",
							FontSize = 16,
							WidthRequest = anchoEtiqueta - 30,
							TextColor = Color.Black
						};

						var botonSigno = new Button
						{
							Text = "Ingreso",
							HorizontalOptions = LayoutOptions.Center,
							VerticalOptions = LayoutOptions.Center,
							FontSize = 13,
							HeightRequest = 60,
							WidthRequest = 80,
							StyleId = i.ToString(),
							BackgroundColor = Color.FromHex("#32CEF9")
						};

						botonSigno.Clicked += DefinirSigno;

						valorCampo = new Entry
						{
							HorizontalOptions = LayoutOptions.StartAndExpand,
							VerticalOptions = LayoutOptions.Center,
							HorizontalTextAlignment = TextAlignment.Start,
							StyleId = "movimiento-" + i,
							WidthRequest = anchoCampo - 65,
							Keyboard = Keyboard.Numeric
						};

						campoValor = new StackLayout
						{
							BackgroundColor = Color.FromHex("#FFFFFF"),
							VerticalOptions = LayoutOptions.Start,
							HorizontalOptions = LayoutOptions.Fill,
							Orientation = StackOrientation.Horizontal,
							HeightRequest = 60,
							Children = { nombreCampo, botonSigno, valorCampo }
						};

						ContenedorProducto.Children.Add(campoValor);

						#endregion

						if (_listaLugares != null)
						{

							#region Precio movimiento

							nombreCampo = new Label
							{
								HorizontalOptions = LayoutOptions.EndAndExpand,
								VerticalOptions = LayoutOptions.Center,
								HorizontalTextAlignment = TextAlignment.End,
								Text = "Monto Total",
								FontSize = 16,
								WidthRequest = anchoEtiqueta,
								TextColor = Color.Black
							};

							valorCampo = new Entry
							{
								HorizontalOptions = LayoutOptions.CenterAndExpand,
								VerticalOptions = LayoutOptions.Center,
								HorizontalTextAlignment = TextAlignment.Start,
								StyleId = "precio-" + i,
								WidthRequest = anchoCampo - 55,
								Keyboard = Keyboard.Numeric
							};

							campoValor = new StackLayout
							{
								BackgroundColor = Color.FromHex("#FFFFFF"),
								VerticalOptions = LayoutOptions.Start,
								HorizontalOptions = LayoutOptions.Fill,
								Orientation = StackOrientation.Horizontal,
								HeightRequest = 60,
								Children = { nombreCampo, valorCampo }
							};

							ContenedorProducto.Children.Add(campoValor);

							#endregion

							#region Lugar de compra/venta

							nombreCampo = new Label
							{
								HorizontalOptions = LayoutOptions.EndAndExpand,
								VerticalOptions = LayoutOptions.Center,
								HorizontalTextAlignment = TextAlignment.End,
								Text = "Lugar",
								FontSize = 16,
								WidthRequest = anchoEtiqueta,
								TextColor = Color.Black
							};

							var puntoVenta = new Picker
							{
								HorizontalOptions = LayoutOptions.CenterAndExpand,
								VerticalOptions = LayoutOptions.Center,
								StyleId = "punto-" + i,
								WidthRequest = anchoCampo - 55
							};
							foreach (var punto in _listaLugares)
							{
								puntoVenta.Items.Add(punto);
							}

							campoValor = new StackLayout
							{
								BackgroundColor = Color.FromHex("#FFFFFF"),
								VerticalOptions = LayoutOptions.Start,
								HorizontalOptions = LayoutOptions.Fill,
								Orientation = StackOrientation.Horizontal,
								HeightRequest = 60,
								Children = { nombreCampo, puntoVenta }
							};

							ContenedorProducto.Children.Add(campoValor);

							#endregion

						}
					}
				}

				i += 1;
			}

			#endregion

			#region Comentario

			nombreCampo = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Comentario",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black
			};

			var valorCampoArea = new Editor
			{
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				VerticalOptions = LayoutOptions.Center,
				StyleId = "comentario",
				WidthRequest = anchoCampo,
				HeightRequest = 90,
				Keyboard = Keyboard.Text
			};

			campoValor = new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = 90,
				Children = { nombreCampo, valorCampoArea }
			};

			ContenedorProducto.Children.Add(campoValor);

			#endregion

		}

		private void DefinirSigno(object sender, EventArgs e)
		{
			var boton = (Button)sender;
			var columna = Convert.ToInt32(boton.StyleId);
			boton.Text = _signoPositivo[columna] ? "Egreso" : "Ingreso";
			boton.BackgroundColor = _signoPositivo[columna] ? Color.FromHex("#FD8A18") : Color.FromHex("#32CEF9");
			_signoPositivo.SetValue(boton.Text.ToLower() == "ingreso", columna);
		}

		[Android.Runtime.Preserve]
		private void EventoGuardarCambios()
		{
			_guardarCambios.Opacity = 0.5f;
			Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
			{
				GuardarCambios();
				_guardarCambios.Opacity = 1f;
				return false;
			});
		}

		private async void GuardarCambios()
		{
			await TareaGuardarCambios();

			await Navigation.PopAsync();
			await DisplayAlert("Producto", _mensaje, "Listo");
		}

		private async Task TareaGuardarCambios()
		{
			foreach (var stackLayout in ContenedorProducto.Children)
			{
				foreach (var control in ((StackLayout)stackLayout).Children)
				{
					int columna;
					string valor;
					if (control.StyleId != null && control.StyleId.Contains("movimiento-"))
					{
						columna = Convert.ToInt32(control.StyleId.Split('-')[1]);
						valor = ((Entry)control).Text;
						valor = !string.IsNullOrEmpty(valor)
							? valor.Replace('.', ',')
							: "0"; //Todos los decimales con coma, evita problema de cultura.
						_cantidades.SetValue(Convert.ToDouble(valor), columna);
					}

					if (control.StyleId != null && control.StyleId.Contains("precio-"))
					{
						columna = Convert.ToInt32(control.StyleId.Split('-')[1]);
						valor = ((Entry)control).Text;
						valor = !string.IsNullOrEmpty(valor)
							? valor.Replace('.', ',')
							: "0"; //Todos los decimales con coma, evita problema de cultura.
						_precios.SetValue(Convert.ToDouble(valor), columna);
					}

					if (_listaLugares != null && control.StyleId != null && control.StyleId.Contains("punto-"))
					{
						columna = Convert.ToInt32(control.StyleId.Split('-')[1]);
						var combo = (Picker)control;
						valor = combo.SelectedIndex != -1 ? combo.Items[combo.SelectedIndex] : "-";
						_lugares.SetValue(valor, columna);
					}

					if (control.StyleId != null && control.StyleId.Contains("comentario"))
					{
						valor = ((Editor)control).Text;
						_comentario = valor;
					}

				}
			}

			ContenedorProducto.Children.Clear();
			ContenedorProducto.Children.Add(_indicadorActividad);

			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						if (CuentaUsuario.ObtenerAccesoDatos() == "G")
							GuardarProductoHojaDeCalculoGoogle();
						else
							GuardarProductoBaseDeDatos();
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

		}

		[Android.Runtime.Preserve]
		private void AccederMovimientos()
		{
			_movimientos.Opacity = 0.5f;
			Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
			{
				Navigation.PushAsync(new ProductoMovimientos(_producto, _servicio), true);
				_movimientos.Opacity = 1f;
				return false;
			});
		}

		[Android.Runtime.Preserve]
		private void Volver()
		{
			_volver.Opacity = 0.5f;
			Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
			{
				Navigation.PopAsync();
				_volver.Opacity = 1f;
				return false;
			});
		}

		private async void GuardarProductoHojaDeCalculoGoogle()
		{
			_mensaje = "Ha ocurrido un error mientras se guardaba el movimiento.";
			var servicioGoogle = new ServiciosGoogle();
			var grabo = false;
			foreach (var celda in _producto)
			{
				if (_listaColumnasInventario[(int)celda.Column - 1] == "1")
				{
					var multiplicador = _signoPositivo[(int)celda.Column - 1] ? 1 : -1;
					var cantidad = _cantidades[(int)celda.Column - 1];
					var precio = _precios[(int)celda.Column - 1];
					var lugar = _listaLugares != null ? _lugares[(int)celda.Column - 1] : "No tiene configurado.";

					if (cantidad != 0)
					{
						try
						{
							// Si no hay lugares no hay campo de PrecioTotal, entonces el precio lo toma de la cantidad
							if (_listaLugares == null)
								precio = multiplicador * cantidad;

							//Ingresa el movimiento de existencia (entrada - salida) en la tabla principal
							servicioGoogle.EnviarMovimiento(_servicio, celda, multiplicador * cantidad, precio, lugar, _comentario, _producto, _nombresColumnas,
								_listaColumnasInventario, CuentaUsuario.ObtenerLinkHojaHistoricos());
							//Si es página principal y tiene las relaciones insumos - productos, ingresa los movimientos de insumos
							if (multiplicador == 1) //Si es ingreso positivo
								servicioGoogle.InsertarMovimientosRelaciones(_servicio, cantidad, _producto);

							grabo = true;
						}
						catch (Exception)
						{
							// Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token
							var paginaAuntenticacion = new PaginaAuntenticacion(true);
							Navigation.InsertPageBefore(paginaAuntenticacion, this);
							await Navigation.PopAsync();
						}
					}
				}
			}
			_mensaje = grabo ? "El movimiento ha sido guardado correctamente." : "No se han registrado movimientos.";
		}

		private void GuardarProductoBaseDeDatos()
		{
			_mensaje = "Ha ocurrido un error mientras se guardaba el movimiento.";
			//const string url = @"http://169.254.80.80/PruebaMision/Service.asmx/ActualizarProducto?codigo={0}&movimiento={1}";
			var i = 0;
			var grabo = false;

			foreach (var celda in _productoString)
			{
				if (_listaColumnasInventario[i] == "1")
				{
					//var multiplicador = _signoPositivo[i] ? 1 : -1;
					var movimiento = _cantidades[i];

					if (movimiento != 0)
					{
						using (var cliente = new HttpClient())
						{
							grabo = true; //await cliente.GetStringAsync(string.Format(url, _productoString[0], (Convert.ToDouble(celda) + multiplicador * movimiento)));
						}
					}
				}

				i += 1;
			}
			_mensaje = grabo ? "El movimiento ha sido guardado correctamente." : "No se han registrado movimientos.";
		}
	}
}
