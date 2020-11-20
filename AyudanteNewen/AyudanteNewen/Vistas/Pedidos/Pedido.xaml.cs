
using System;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace AyudanteNewen.Vistas
{
	public partial class Pedido
	{
		private readonly Clases.Pedido _pedido;
		private readonly SpreadsheetsService _servicio;
		private string _mensaje = "";
		private ActivityIndicator _indicadorActividad;
		private CellFeed _celdas;
		private string[] _listaColumnasInventario;

		public Pedido(Clases.Pedido pedido, SpreadsheetsService servicio, CellFeed celdas = null)
		{
			InitializeComponent();
			InicializarValoresGenerales();
			_pedido = pedido;
			_servicio = servicio;
			_celdas = celdas;

			ConstruirVistaDePedido();
		}

		private void InicializarValoresGenerales()
		{
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);

			var columnasInventario = CuentaUsuario.ObtenerColumnasInventario();
			_listaColumnasInventario = !string.IsNullOrEmpty(columnasInventario) ? columnasInventario.Split(',') : null;

			_indicadorActividad = new ActivityIndicator
			{
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = true,
				BindingContext = this
			};
			_indicadorActividad.SetBinding(IsVisibleProperty, "IsBusy");
			_indicadorActividad.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
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

		private View BuscarControlEnHijos(StackLayout controlPadre, string id)
        {
			foreach(var control in controlPadre.Children)
            {
				if (control.StyleId == id) return control;
				if (control.GetType() == typeof(StackLayout))
				{
					var res = BuscarControlEnHijos((StackLayout)control, id);
					if (res != null) return res;
				}
			}
			return null;
        }

		private async void CambiarEstadoPedido(object sender, EventArgs e)
		{
			if (!await DisplayAlert("Pedido", "¿Confirma el cambio de estado?", "Sí", "No")) return;
			var boton = (Button)sender;
			var nuevoEstado = boton.StyleId;

			var descontarStock = nuevoEstado == "finalizado" && await DisplayAlert("Pedido", "¿Desea descontar el stock?", "Sí", "No");

			await TareaActualizarEstado(nuevoEstado, descontarStock);

			await Navigation.PopAsync();
			await DisplayAlert("Pedido", _mensaje, "Listo");
		}

		private View ObtenerControlEstado(string estado)
		{
			var ancho = (int)((App.AnchoRetratoDePantalla - 30) / 3);

			var estadoActual = new Label
			{
				FontSize = 14,
				TextColor = Color.FromHex("#1D1D1B"),
				HeightRequest = 55,
				WidthRequest = ancho,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				BackgroundColor = ("cancelado, finalizado").Contains(estado.ToLower())
					? Color.FromHex("#E2E2E1")
					: Color.FromHex("#32CEF9"),
				Text = estado.ToUpper(),
				VerticalOptions = LayoutOptions.Center,
				StyleId = "estadoActual"
			};

			var flecha = new Label
			{
				FontSize = 16,
				TextColor = Color.FromHex("#1D1D1B"),
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				WidthRequest = 15,
				HeightRequest = 65,
				Text = "->",
				StyleId = "flechaProximoEstado"
			};

			var botonProximoEstado = new Button
			{
				HeightRequest = 65,
				WidthRequest = ancho,
				Text = "Finalizar",
				BackgroundColor = Color.FromHex("#FD8A18"),
				FontSize = 14,
				StyleId = "finalizado"
			};
			botonProximoEstado.Clicked += CambiarEstadoPedido;

			var botoCancelar = new Button
			{
				HeightRequest = 65,
				WidthRequest = ancho,
				Text = "Cancelar",
				BackgroundColor = Color.FromHex("#FE6161"),
				FontSize = 14,
				StyleId = "cancelado"
			};
			botoCancelar.Clicked += CambiarEstadoPedido;

			var controlProximoEstado = new StackLayout
			{
				Children = { flecha, botonProximoEstado, botoCancelar },
				StyleId = "controlProximoEstado",
				Orientation = StackOrientation.Horizontal
			};

			var vista = new StackLayout
			{
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Center,
				Orientation = StackOrientation.Horizontal,
				Margin = 5,
				Children = { estadoActual }
			};

			if (estado.ToLower() == "pendiente")
            {
				vista.Children.Add(controlProximoEstado);
			}

			return vista;
		}

		private void ConstruirVistaDePedido()
		{
			var anchoEtiqueta = (int)(App.AnchoRetratoDePantalla / 3 - 10);
			var anchoCampo = (int)(App.AnchoRetratoDePantalla / 3 * 2 - 30);

			Label nombreCampo;
			Entry valorCampo;
			StackLayout campoValor;

			#region Campos de planilla

			// Estado
			ContenedorProducto.Children.Add(ObtenerControlEstado(_pedido.Estado));

			nombreCampo = ObtenerEtiquetaProp("Código", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Id, anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Fecha", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Fecha, anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Cliente", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Cliente, anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Fecha entrega", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.FechaEntrega,  anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			// Detalle
			var anchoGrilla = (int)(App.AnchoRetratoDePantalla * 0.99);
			var anchoColumna = anchoGrilla / 3 - 2; // - 2 por el divisor (2px)
			var grillaDetalle = ConstruirGrillaDetalle(_pedido.Detalle, anchoGrilla, anchoColumna);
			ContenedorProducto.Children.Add(ConstruirEncabezadoDetalle(anchoGrilla, anchoColumna));
			ContenedorProducto.Children.Add(grillaDetalle);

			nombreCampo = ObtenerEtiquetaProp("Usuario", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Usuario, anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Comentario", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Comentario, anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			nombreCampo = ObtenerEtiquetaProp("Lugar", anchoEtiqueta);
			valorCampo = ObtenerCampoProp(_pedido.Lugar, anchoCampo);
			campoValor = ObtenerControlProp(nombreCampo, valorCampo);
			ContenedorProducto.Children.Add(campoValor);

			#endregion
		}

		private async Task TareaActualizarEstado(string estado, bool descontarStock)
		{
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
						{
							ActualizarEstadoPedidoHCG(estado);
							if (descontarStock) ActualizarInventario();
						}
						else
							ActualizarEstadoPedidoBD();

						_mensaje = estado == "finalizado" ? "Los cambios han sido registrados correctamente." : "El pedido ha sido cancelado.";
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

        private async void ActualizarEstadoPedidoHCG(string estado)
		{
			_mensaje = "Ha ocurrido un error mientras se actualizaba el estado.";
            try
            {
				var estadoPrimeraMayus = char.ToUpper(estado[0]) + estado.Substring(1);
				foreach (CellEntry celda in _celdas.Entries)
				{
					if (celda.Row != _pedido.FilaPlanillaCalculo) continue;

					if (celda.Column == 7 || celda.Column == 8)
					{
						celda.InputValue = celda.Column == 7 ? estadoPrimeraMayus : CuentaUsuario.ObtenerNombreUsuarioGoogle() ?? "-";
						celda.Update();
					}
				}
            }
            catch (Exception)
            {
                // Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token
                var paginaAuntenticacion = new PaginaAuntenticacion(true);
                Navigation.InsertPageBefore(paginaAuntenticacion, this);
                await Navigation.PopAsync();
            }
		}

		private List<string[]> ObtenerListado(ServiciosGoogle servicioGoogle, string linkHoja)
		{
			var celdas = servicioGoogle.ObtenerCeldasDeUnaHoja(linkHoja, _servicio);

			var listado = new List<string[]>();
			var item = new string[celdas.ColCount.Count];

			foreach (CellEntry celda in celdas.Entries)
			{
				if (celda.Row != 1)
				{
					if (celda.Column == 1)
						item = new string[celdas.ColCount.Count];

					item.SetValue(celda.Value, (int)celda.Column - 1);

					if (celda.Column == celdas.ColCount.Count)
						listado.Add(item);
				}
			}
			return listado;
		}

		private void ActualizarInventario()
		{
			var servicioGoogle = new ServiciosGoogle();
			var columnasProductos = CuentaUsuario.ObtenerColumnasProductos()?.Split(',');
			var linkHojaProducto = CuentaUsuario.ObtenerLinkHojaPorNombre("Productos App");
			var linkHojaProductoHistorico = CuentaUsuario.ObtenerLinkHojaHistoricosParaLinkHoja(linkHojaProducto);

			var productos = ObtenerListado(servicioGoogle, linkHojaProducto);
			string comentario = "Pedido " + _pedido.Id + " a " + _pedido.Cliente + ". " + _pedido.Comentario;

			foreach (var lineaDetalle in _pedido.Detalle)
            {
				var producto = new string[columnasProductos.Length];
				// Buscar datos producto
				foreach(var prod in productos)
                {
					if(prod[0] == lineaDetalle.IdProducto)
                    {
						producto = prod;
						break;
					}
				}

				servicioGoogle.EnviarMovimiento(
					_servicio,
					lineaDetalle.ColumnaStockElegido,
					-1 * Convert.ToDouble(lineaDetalle.Cantidad.Replace(',','.')),
					Convert.ToDouble(lineaDetalle.Precio.Replace(',', '.')),
					_pedido.Lugar,
					comentario,
					producto,
					columnasProductos,
					_listaColumnasInventario,
					linkHojaProductoHistorico
				);
			}
		}

		private void ActualizarEstadoPedidoBD()
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
