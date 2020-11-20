
using Google.GData.Spreadsheets;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using DataTemplate = Xamarin.Forms.DataTemplate;
using TextAlignment = Xamarin.Forms.TextAlignment;

namespace AyudanteNewen.Vistas
{
	public partial class PedidosGrilla
	{
		private readonly ServiciosGoogle _servicioGoogle;
		private readonly SpreadsheetsService _servicio;
		private ViewCell _ultimoItemSeleccionado;
		private Color _ultimoColorSeleccionado;
		private ActivityIndicator _indicadorActividad;
		private List<Clases.Pedido> _listaPedidos;
        // private List<string[]> _pedidos;
		private CellFeed _celdas;

		// Constructor para Hoja de cálculo de Google
		public PedidosGrilla(SpreadsheetsService servicio)
		{
			InitializeComponent();

			_servicioGoogle = new ServiciosGoogle();
			// El servicio viene nulo cuando se llama directamente desde el lanzador (ya tiene conexión a datos configurada)
			_servicio = servicio ?? _servicioGoogle.ObtenerServicioParaConsultaGoogleSpreadsheets(CuentaUsuario.ObtenerTokenActualDeGoogle());

			InicializarValoresGenerales();
			// La carga de los pedidos se realiza en el OnAppearing
		}

		// Constructor para Base de Datos
		public PedidosGrilla()
		{
			InitializeComponent();
			InicializarValoresGenerales();
			ObtenerPedidosDesdeBD();
		}

		#region Métodos para Hoja de cálculo de Google

		private async void ObtenerDatosPedidosDesdeHCG()
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						var linkHojaPedidos = CuentaUsuario.ObtenerLinkHojaPedidos(); ;
						_celdas = _servicioGoogle.ObtenerCeldasDeUnaHoja(linkHojaPedidos, _servicio);

						var pedidos = new List<string[]>();
						var pedido = new string[_celdas.ColCount.Count];

						foreach (CellEntry celda in _celdas.Entries)
						{
							if (celda.Row != 1)
							{
								if (celda.Column == 1)
									pedido = new string[_celdas.ColCount.Count];

								pedido.SetValue(celda.Value, (int)celda.Column - 1);

								if (celda.Column == _celdas.ColCount.Count)
									pedidos.Add(pedido);
							}
						}

						LlenarGrillaDePedidos(pedidos);
					}
					else
					{
						// Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token.
						var paginaAuntenticacion = new PaginaAuntenticacion(true);
						Navigation.InsertPageBefore(paginaAuntenticacion, this);
						await Navigation.PopAsync();
					}
				});
			}
			finally
			{
				// Remueve el Indicador de Actividad.
				IsBusy = false;
			}
		}

		#endregion

		#region Métodos para Base de Datos

		private async void ObtenerPedidosDesdeBD()
		{
			RefrescarUIGrilla();

			const string url = "http://www.misionantiinflacion.com.ar/api/v1/products?token=05f9a1a6683c2ba246c2b057d0433429a176b674d9a68557ddbdcf33c474aee4";

			using (var cliente = new HttpClient())
			{
				List<string[]> pedidos = null;
				try
				{
					IsBusy = true;

					await Task.Run(async () =>
					{
						// Obtiene json de pedidos desde el webservice
						var jsonPedidos = await cliente.GetStringAsync(url);
						// Parsea el json para obtener la lista de pedidos
						pedidos = ParsearJSONPedidos(jsonPedidos);
					});
				}
				finally
				{
					IsBusy = false;
				}

				if (pedidos != null)
					LlenarGrillaDePedidos(pedidos);
			}
		}

		private static List<string[]> ParsearJSONPedidos(string jsonPedidoss)
		{
			jsonPedidoss = jsonPedidoss.Substring(jsonPedidoss.IndexOf("\"data\":[{") + 9)
				.Replace("}]}", "")
				.Replace("},{\"id\"", "|");
			var arregloPedidos = jsonPedidoss.Split('|');
			var pedidos = new List<string[]>();

			foreach (var datos in arregloPedidos)
			{
				var temporal = datos.Replace(",\"", "|").Split('|');

				// Si el pedido no está oculto lo agregamos
				if (temporal[12].Split(':')[1].TrimStart('"').TrimEnd('"') == "true") continue;
				var pedido = new string[3];
				pedido[0] = temporal[0].Split(':')[1].TrimStart('"').TrimEnd('"'); // ID
				pedido[1] = temporal[2].Split(':')[1].TrimStart('"').TrimEnd('"').Replace("\\\"", "\""); // Nombre
				var stock = temporal[18].Split(':')[1].TrimStart('"').TrimEnd('"'); // Stock
				pedido[2] = stock == "null" ? "0" : stock;

				pedidos.Add(pedido);
			}

			return pedidos;
		}

		#endregion

		#region Métodos comunes

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
			var anchoBoton = App.AnchoRetratoDePantalla / 2;
			BotonRefrescar.WidthRequest = anchoBoton;
			BotonNuevoPedido.WidthRequest = anchoBoton;
		}

		private void LlenarGrillaDePedidos(List<string[]> pedidos, bool esBusqueda = false)
		{
			// Se carga la grilla de pedidos y se muestra en pantalla.
			ConstruirVistaDeLista(pedidos);
			if (!esBusqueda)
				FijarPedidosYBuscador(pedidos);
		}

		private async void IrAlPedido(string idPedidoSeleccionado)
		{
			var pedisoSeleccionado = _listaPedidos.FirstOrDefault(pedido => pedido.Id == idPedidoSeleccionado);
			if (pedisoSeleccionado != null)
				await Navigation.PushAsync(new Pedido(pedisoSeleccionado, _servicio, _celdas), true);
			else
				await DisplayAlert("Código", "No se ha encontrado un pedido para el código seleccionado.", "Listo");

		}

		private List<Clases.Pedido> ObtenerListaPedidos(IReadOnlyCollection<string[]> pedidos)
        {
			bool esTeclaPar = false;
			var listaPedidos = new List<Clases.Pedido>();
			var filaPlanillaCalculo = 2;
			foreach (var filaPedido in pedidos)
			{
				// Mostraremos solo las sig. columnas de la planilla: Fecha, Cliente, Detalle, Fecha entrega, Estado, Usuario
				var pedido = new Clases.Pedido(
					filaPedido[0],
					filaPedido[1],
					filaPedido[2],
					filaPedido[3],
					filaPedido[4],
					filaPedido[5],
					filaPedido[6],
					filaPedido[7],
					filaPedido[8],
					filaPedido[9],
					filaPlanillaCalculo
				);
				listaPedidos.Add(pedido);
				esTeclaPar = !esTeclaPar;
				filaPlanillaCalculo += 1;
			}
			return listaPedidos;
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
				WidthRequest = ancho,
				Margin = 3
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

		private void ConstruirVistaDeLista(IReadOnlyCollection<string[]> pedidos)
		{
			_listaPedidos = ObtenerListaPedidos(pedidos);

			var anchoColumna = (App.AnchoRetratoDePantalla / 4) - 2; // - 2 por el divisor (2px)

			var encabezado = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Color.FromHex("#C0C0C0"),
				HeightRequest = pedidos.Count <= 25 ? 35 : 50,
				Padding = 3,
				Children =
					{
						ObtenerEtiquetaTitulo("Fecha", (int)anchoColumna),
						ObtenerEtiquetaTitulo("Cliente", (int)anchoColumna),
						ObtenerEtiquetaTitulo("Fecha Entrega", (int)anchoColumna),
						ObtenerEtiquetaTitulo("Estado", (int)anchoColumna)
					}
			};

			var altoTeja = 60;
			var vista = new ListView
			{
				RowHeight = altoTeja,
				VerticalOptions = LayoutOptions.StartAndExpand,
				HorizontalOptions = LayoutOptions.Fill,
				ItemsSource = _listaPedidos,
				ItemTemplate = new DataTemplate(() =>
				{
					var tecla = new StackLayout
					{
						Padding = 3,
						Orientation = StackOrientation.Horizontal,
						Children = {
							ObtenerEtiquetaDato("Fecha", (int)anchoColumna),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("Cliente", (int)anchoColumna),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("FechaEntrega", (int)anchoColumna),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("Estado", (int)anchoColumna)
						}
					};
					tecla.SetBinding(BackgroundColorProperty, "ColorFondo");

					var celda = new ViewCell { View = tecla };

					celda.Tapped += (sender, args) =>
					{
						if (_ultimoItemSeleccionado != null)
							_ultimoItemSeleccionado.View.BackgroundColor = _ultimoColorSeleccionado;
						IrAlPedido(((Clases.Pedido)((ViewCell)sender).BindingContext).Id);
						_ultimoColorSeleccionado = celda.View.BackgroundColor;
						celda.View.BackgroundColor = Color.Silver;
						_ultimoItemSeleccionado = (ViewCell)sender;
					};

					return celda;
				})
			};

			Device.BeginInvokeOnMainThread(() =>
			{
				ContenedorTabla.Children.Clear();
				ContenedorTabla.Children.Add(encabezado);
				ContenedorTabla.Children.Add(vista);
			});
		}

		private void FijarPedidosYBuscador(List<string[]> pedidos)
		{
			// Si hay más de 25 pedidos se muestra el buscador
			if (pedidos.Count <= 25) return;
			// Almacena la lista de pedidos en la variable global que usará el buscador
			// _pedidos = pedidos;
		}

		private void RefrescarUIGrilla()
		{
			// Se quita la grilla para recargarla.
			ContenedorTabla.Children.Clear();
			ContenedorTabla.Children.Add(_indicadorActividad);
		}

		#endregion

		#region Eventos
		private void RefrescarDatos(object sender, EventArgs e)
		{
			BotonRefrescar.BackgroundColor = Color.FromHex("#32CEF9");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				RefrescarUIGrilla();

				if (CuentaUsuario.ObtenerAccesoDatos() == "G")
					ObtenerDatosPedidosDesdeHCG();
				else
					ObtenerPedidosDesdeBD();
				BotonRefrescar.BackgroundColor = Color.FromHex("#32BBF9");
				return false;
			});
		}

		private void CrearPedido(object sender, EventArgs e)
		{
			BotonNuevoPedido.BackgroundColor = Color.FromHex("#FB9F0B");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				Navigation.PushAsync(new NuevoPedido(_servicio, _listaPedidos), true);
				BotonNuevoPedido.BackgroundColor = Color.FromHex("#FD8A18");
				return false;
			});
		}

		// Cuando carga la página.
		protected override void OnAppearing()
		{
			RefrescarDatos(null, null);
		}

		#endregion
	}
}
