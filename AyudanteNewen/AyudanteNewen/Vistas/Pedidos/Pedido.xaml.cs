
using System;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

namespace AyudanteNewen.Vistas
{
	public partial class Pedido
	{
		private readonly ClasePedido _pedido;
		private readonly SpreadsheetsService _servicio;
		private string _mensaje = "";
		private ActivityIndicator _indicadorActividad;
		private Image _volver;
		private Image _guardarCambios;

		public Pedido(ClasePedido pedido, string[] nombres, SpreadsheetsService servicio)
		{
			InitializeComponent();
			InicializarValoresGenerales();
			_pedido = pedido;
			_servicio = servicio;

			ConstruirVistaDePedido();
		}

		private void InicializarValoresGenerales()
		{
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);

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

        private void ConfigurarBotones()
		{
			_volver = App.Instancia.ObtenerImagen(TipoImagen.BotonVolver);
			_volver.GestureRecognizers.Add(new TapGestureRecognizer
				{
					Command = new Command(Volver),
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
			ContenedorBotones.Children.Add(_guardarCambios);
		}

		private Label ObtenerEtiquetaProp(string texto, int anchoEtiqueta)
        {
			return new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				Text = texto,
				TextColor = Color.Black
			};
		}
		
		private Entry ObtenerCampoProp(string texto, int anchoCampo)
        {
			return new Entry
			{
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.Start,
				WidthRequest = anchoCampo,
				IsEnabled = false,
				Text = texto,
				TextColor = Color.Black
			};
		}
		
		private StackLayout ObtenerControlProp(Label nombreCampo, Entry valorCampo)
        {
			return new StackLayout
			{
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = 50,
				Children = { nombreCampo, valorCampo }
			};
		}
		
		private Label ObtenerEtiquetaTitulo(string texto, int ancho)
		{
			return new Label
			{
				Text = texto,
				FontSize = 13,
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				FontAttributes = FontAttributes.Bold,
				TextColor = Color.Black,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = ancho
			};
		}

		private Label ObtenerEtiquetaDato(string campo, int ancho)
		{
			var etiquetaDato = new Label
			{
				FontSize = 14,
				TextColor = Color.FromHex("#1D1D1B"),
				VerticalOptions = LayoutOptions.CenterAndExpand,
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				WidthRequest = ancho,
				Margin = 3,
				HorizontalTextAlignment = TextAlignment.Center
			};
			etiquetaDato.SetBinding(Label.TextProperty, campo);
			return etiquetaDato;
		}

		private BoxView ObtenerSeparado(int altoTeja)
		{
			return new BoxView
			{
				WidthRequest = 2,
				BackgroundColor = Color.FromHex("#FFFFFF"),
				HeightRequest = altoTeja - 5
			};
		}

		private StackLayout ConstruirEncabezadoDetalle(int anchoGrilla, int anchoColumna)
		{
			var encabezado = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Color.FromHex("#C0C0C0"),
				HeightRequest = 30,
				Padding = 3,
				WidthRequest= anchoGrilla,
				Children =
					{
						ObtenerEtiquetaTitulo("Producto", (int)anchoColumna),
						ObtenerEtiquetaTitulo("Cantidad", (int)anchoColumna),
						ObtenerEtiquetaTitulo("Precio", (int)anchoColumna)
					}
			};

			return encabezado;
		}

		private ListView ConstruirGrillaDetalle(List<DetallePedido> detalle, int anchoGrilla, int anchoColumna)
		{
			var altoTeja = 30;
			var vista = new ListView
			{
				RowHeight = altoTeja,
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Center,
				ItemsSource = detalle,
				WidthRequest = anchoGrilla,
				ItemTemplate = new DataTemplate(() =>
				{
					var tecla = new StackLayout
					{
						Padding = 3,
						Orientation = StackOrientation.Horizontal,
						Children = {
							ObtenerEtiquetaDato("NombreProducto", (int)anchoColumna),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("Cantidad", (int)anchoColumna),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("Precio", (int)anchoColumna)
						}
					};
					tecla.SetBinding(BackgroundColorProperty, "ColorFondo");

					return new ViewCell { View = tecla };
				})
			};

			return vista;
		}

		private void ConstruirVistaDePedido()
		{
			var anchoEtiqueta = App.AnchoRetratoDePantalla / 3 - 10;
			var anchoCampo = App.AnchoRetratoDePantalla / 3 * 2 - 30;

			Label nombreCampo;
			Entry valorCampo;
			StackLayout campoValor;

			#region Campos de planilla

			nombreCampo = ObtenerEtiquetaProp("Código", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Id, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Fecha", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Fecha, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Cliente", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Cliente, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Fecha entrega", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.FechaEntrega, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Estado", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Estado, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			// Detalle
			var anchoGrilla = (int)(App.AnchoRetratoDePantalla * 0.9);
			var anchoColumna = anchoGrilla / 3 - 2; // - 2 por el divisor (2px)
			var grillaDetalle = ConstruirGrillaDetalle(Auxiliar.ParsearJsonDetallePedido(_pedido.Detalle), anchoGrilla, anchoColumna); // -6 por los divisores restados a las columnas
			ContenedorProducto.Children.Add(ConstruirEncabezadoDetalle(anchoGrilla, anchoColumna));
			ContenedorProducto.Children.Add(grillaDetalle);

			nombreCampo = ObtenerEtiquetaProp("Usuario", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Usuario, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Comentario", (int)anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Comentario, (int)anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			#endregion
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
					//if (control.StyleId != null && control.StyleId.Contains("movimiento-"))
					//{
					//	columna = Convert.ToInt32(control.StyleId.Split('-')[1]);
					//	valor = ((Entry)control).Text;
					//	valor = !string.IsNullOrEmpty(valor)
					//		? valor.Replace('.', ',')
					//		: "0"; //Todos los decimales con coma, evita problema de cultura.
					//	_cantidades.SetValue(Convert.ToDouble(valor), columna);
					//}

					//if (control.StyleId != null && control.StyleId.Contains("precio-"))
					//{
					//	columna = Convert.ToInt32(control.StyleId.Split('-')[1]);
					//	valor = ((Entry)control).Text;
					//	valor = !string.IsNullOrEmpty(valor)
					//		? valor.Replace('.', ',')
					//		: "0"; //Todos los decimales con coma, evita problema de cultura.
					//	_precios.SetValue(Convert.ToDouble(valor), columna);
					//}

					//if (_listaLugares != null && control.StyleId != null && control.StyleId.Contains("punto-"))
					//{
					//	columna = Convert.ToInt32(control.StyleId.Split('-')[1]);
					//	var combo = (Picker)control;
					//	valor = combo.SelectedIndex != -1 ? combo.Items[combo.SelectedIndex] : "-";
					//	_lugares.SetValue(valor, columna);
					//}

					if (control.StyleId != null && control.StyleId.Contains("comentario"))
					{
						valor = ((Editor)control).Text;
						//_comentario = valor;
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
			//foreach (var celda in _pedido)
			//{
			//	if (_listaColumnasInventario[(int)celda.Column - 1] == "1")
			//	{
			//		var multiplicador = _signoPositivo[(int)celda.Column - 1] ? 1 : -1;
			//		var cantidad = _cantidades[(int)celda.Column - 1];
			//		var precio = _precios[(int)celda.Column - 1];
			//		var lugar = _listaLugares != null ? _lugares[(int)celda.Column - 1] : "No tiene configurado.";

			//		if (cantidad != 0)
			//		{
			//			try
			//			{
			//				// Si no hay lugares no hay campo de PrecioTotal, entonces el precio lo toma de la cantidad
			//				if (_listaLugares == null)
			//					precio = multiplicador * cantidad;

			//				//Ingresa el movimiento de existencia (entrada - salida) en la tabla principal
			//				servicioGoogle.EnviarMovimiento(_servicio, celda, multiplicador * cantidad, precio, lugar, _comentario, _pedido, _nombresColumnas,
			//					_listaColumnasInventario, CuentaUsuario.ObtenerLinkHojaHistoricos());
			//				//Si es página principal y tiene las relaciones insumos - productos, ingresa los movimientos de insumos
			//				if (multiplicador == 1) //Si es ingreso positivo
			//					servicioGoogle.InsertarMovimientosRelaciones(_servicio, cantidad, _pedido);

			//				grabo = true;
			//			}
			//			catch (Exception)
			//			{
			//				// Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token
			//				var paginaAuntenticacion = new PaginaAuntenticacion(true);
			//				Navigation.InsertPageBefore(paginaAuntenticacion, this);
			//				await Navigation.PopAsync();
			//			}
			//		}
			//	}
			//}
			_mensaje = grabo ? "El movimiento ha sido guardado correctamente." : "No se han registrado movimientos.";
		}

		private void GuardarProductoBaseDeDatos()
		{
			_mensaje = "Ha ocurrido un error mientras se guardaba el movimiento.";
			//const string url = @"http://169.254.80.80/PruebaMision/Service.asmx/ActualizarProducto?codigo={0}&movimiento={1}";

			var grabo = false;

			using (var cliente = new HttpClient())
			{
				grabo = true;
			}

			_mensaje = grabo ? "El movimiento ha sido guardado correctamente." : "No se han registrado movimientos.";
		}
	}
}
