
using Google.GData.Spreadsheets;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using ZXing.Net.Mobile.Forms;
using DataTemplate = Xamarin.Forms.DataTemplate;
using TextAlignment = Xamarin.Forms.TextAlignment;

namespace AyudanteNewen.Vistas
{
	public partial class PaginaGrilla
	{
		private readonly ServiciosGoogle _servicioGoogle;
		private readonly SpreadsheetsService _servicio;
		private CellFeed _celdas;
		private string _linkHojaConsulta;
		private string[] _nombresColumnas;
		private string[] _listaColumnasParaVer;
		private string[] _listaColumnasInventario;
		private ViewCell _ultimoItemSeleccionado;
		private Color _ultimoColorSeleccionado;
		private List<string[]> _productos;
		private bool _esCargaInicial = true;
		private ActivityIndicator _indicadorActividad;
		private Picker _listaHojas;
		private double _anchoActual;

		//Constructor para Hoja de cálculo de Google
		public PaginaGrilla(string linkHojaConsulta, SpreadsheetsService servicio)
		{
			InitializeComponent();

			_linkHojaConsulta = linkHojaConsulta;
			_servicioGoogle = new ServiciosGoogle();
			//El servicio viene nulo cuando se llama directamente desde el lanzador (ya tiene conexión a datos configurada)
			_servicio = servicio ?? _servicioGoogle.ObtenerServicioParaConsultaGoogleSpreadsheets(CuentaUsuario.ObtenerTokenActualDeGoogle());

			InicializarValoresGenerales();
			ConfigurarSelectorHojas();

			//La carga de los productos se realiza en el OnAppearing
		}

		//Constructor para Base de Datos
		public PaginaGrilla()
		{
			InitializeComponent();
			InicializarValoresGenerales();
			ObtenerProductosDesdeBD();
		}

		#region Métodos para Hoja de cálculo de Google

		private async void ObtenerDatosProductosDesdeHCG(bool refrescarLugaresRelaciones)
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						_celdas = _servicioGoogle.ObtenerCeldasDeUnaHoja(_linkHojaConsulta, _servicio);

						//Refresca los lugares (compra o venta) y relaciones (insumos - productos) sólo cuando presionamos el botón de refrescar y durante la carga inicial
						if (refrescarLugaresRelaciones)
							RefrescarLugCompVtas_RelacionInsProd();
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

			_nombresColumnas = CuentaUsuario.ObtenerColumnasProductos()?.Split(',');

			var productos = new List<string[]>();
			var producto = new string[_celdas.ColCount.Count];

			foreach (CellEntry celda in _celdas.Entries)
			{
				if (celda.Row != 1)
				{
					if (celda.Column == 1)
						producto = new string[_celdas.ColCount.Count];

					producto.SetValue(celda.Value, (int)celda.Column - 1);

					if (celda.Column == _celdas.ColCount.Count)
						productos.Add(producto);
				}
			}

			LlenarGrillaDeProductos(productos);
		}

		private void ConfigurarSelectorHojas()
		{
			_listaHojas = new Picker
			{
				IsVisible = false,
				WidthRequest = App.AnchoRetratoDePantalla * .5,
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center
			};

			_listaHojas.IsVisible = true;
			var nombreHojaActual = CuentaUsuario.ObtenerNombreHoja(_linkHojaConsulta);
			var nombres = CuentaUsuario.ObtenerTodosLosNombresDeHojas();
			var i = 0;
			foreach (var nombre in nombres)
			{
				_listaHojas.Items.Add(nombre);
				if (nombre == nombreHojaActual)
					_listaHojas.SelectedIndex = i;
				i += 1;
			}

			_listaHojas.SelectedIndexChanged += CargarHoja;
			Cabecera.Children.Add(_listaHojas);
		}

		#endregion

		#region Métodos para Base de Datos

		private async void ObtenerProductosDesdeBD()
		{
			//var url = $@"http://169.254.80.80/PruebaMision/Service.asmx/RecuperarProductos?token={
			//		CuentaUsuario.ObtenerTokenActualDeBaseDeDatos()
			//	}";

			RefrescarUIGrilla();

			const string url = "http://www.misionantiinflacion.com.ar/api/v1/products?token=05f9a1a6683c2ba246c2b057d0433429a176b674d9a68557ddbdcf33c474aee4";

			using (var cliente = new HttpClient())
			{
				List<string[]> productos = null;
				try
				{
					IsBusy = true;

					await Task.Run(async () =>
					{
						//Obtiene json de productos desde el webservice
						var jsonProductos = await cliente.GetStringAsync(url);
						//Parsea el json para obtener la lista de productos
						productos = ParsearJSONProductos(jsonProductos);

						_nombresColumnas = new[] { "Código", "Nombre", "Stock" };
					});
				}
				finally
				{
					IsBusy = false;
				}

				if (productos != null)
					LlenarGrillaDeProductos(productos);
			}
		}

		private static List<string[]> ParsearJSONProductos(string jsonProductos)
		{
			jsonProductos = jsonProductos.Substring(jsonProductos.IndexOf("\"data\":[{") + 9)
				.Replace("}]}", "")
				.Replace("},{\"id\"", "|");
			var arregloProductos = jsonProductos.Split('|');
			var productos = new List<string[]>();

			foreach (var datos in arregloProductos)
			{
				var temporal = datos.Replace(",\"", "|").Split('|');

				//Si el producto no está oculto lo agregamos
				if (temporal[12].Split(':')[1].TrimStart('"').TrimEnd('"') == "true") continue;
				var producto = new string[3];
				producto[0] = temporal[0].Split(':')[1].TrimStart('"').TrimEnd('"'); // ID
				producto[1] = temporal[2].Split(':')[1].TrimStart('"').TrimEnd('"').Replace("\\\"", "\""); // Nombre
				var stock = temporal[18].Split(':')[1].TrimStart('"').TrimEnd('"'); // Stock
				producto[2] = stock == "null" ? "0" : stock;

				productos.Add(producto);
			}

			return productos;
		}

		#endregion

		#region Métodos comunes

		private void InicializarValoresGenerales()
		{
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			ConfigurarBotones();

			var columnasParaVer = CuentaUsuario.ObtenerColumnasParaVer();
			if (!string.IsNullOrEmpty(columnasParaVer))
				_listaColumnasParaVer = columnasParaVer.Split(',');

			var columnasInventario = CuentaUsuario.ObtenerColumnasInventario();
			if (!string.IsNullOrEmpty(columnasInventario))
				_listaColumnasInventario = columnasInventario.Split(',');

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
			var anchoBoton = App.AnchoRetratoDePantalla / 3;
			BotonPedidos.WidthRequest = anchoBoton;
			BotonRefrescar.WidthRequest = anchoBoton;
			BotonEscaner.WidthRequest = anchoBoton;
		}

		private void LlenarGrillaDeProductos(List<string[]> productos, bool esBusqueda = false)
		{
			//Se carga la grilla de productos y se muestra en pantalla.
			ConstruirVistaDeLista(productos);
			if (!esBusqueda)
				FijarProductosYBuscador(productos);
		}

		private async void IrAlProducto(string codigoProductoSeleccionado)
		{
			var fila = -1;
			GrupoEncabezado.IsVisible = false;
			if (CuentaUsuario.ObtenerAccesoDatos() == "G")
			{
				var productoSeleccionado = new CellEntry[_celdas.ColCount.Count];

				// Obtener el arreglo del producto para enviar
				foreach (CellEntry celda in _celdas.Entries)
				{
					if (celda.Column == 1 && celda.Value == codigoProductoSeleccionado)
						fila = (int)celda.Row;
					if (celda.Row == fila)
						productoSeleccionado.SetValue(celda, (int)celda.Column - 1);

					// Si encontró producto (fila > -1) y ya pasó al próximo producto (celda.Row > fila) o es el último producto (celda.Column == _celdas.ColCount.Count)
					if (fila > -1 && (celda.Row > fila || celda.Column == _celdas.ColCount.Count))
					{
						var titulo = _nombresColumnas != null && _nombresColumnas.Length > 1 ? _nombresColumnas[1] : "PRODUCTO";
						await Navigation.PushAsync(new Producto(productoSeleccionado, _nombresColumnas, _servicio, titulo), true);
						break;
					}
				}
			}
			else
			{
				foreach (var producto in _productos)
				{
					if (producto[0] != codigoProductoSeleccionado) continue;
					fila = 0;
					await Navigation.PushAsync(new Producto(producto, _nombresColumnas), true);
					break;
				}
			}
			GrupoEncabezado.IsVisible = true;
			// Si fila = -1 no se ha encuentrado el código
			if (fila == -1)
				await DisplayAlert("Código", "No se ha encontrado un producto para el código escaneado.", "Listo");
		}

		private List<ClaseProducto> ObtenerListaProductos(IReadOnlyCollection<string[]> productos)
        {
			bool esTeclaPar = false;
			var listaProductos = new List<ClaseProducto>();
			foreach (var datosProducto in productos)
			{
				var datosParaVer = new List<string>();
				var i = 0;
				decimal stockTotalProducto = 0;
				decimal nivelStockMinimo = 0;

				foreach (var dato in datosProducto)
				{
					var textoDato = "";

					if (_listaColumnasParaVer != null && _listaColumnasParaVer[i] == "1")
					{
						textoDato += _nombresColumnas[i] + ": " + dato + '\n';
						datosParaVer.Add(textoDato);
					}
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

				bool tieneBajoStock = stockTotalProducto <= nivelStockMinimo;
				var producto = new ClaseProducto(datosProducto[0], datosParaVer, esTeclaPar, tieneBajoStock);
				listaProductos.Add(producto);
				esTeclaPar = !esTeclaPar;
			}
			return listaProductos;
		}

		private void ConstruirVistaDeLista(IReadOnlyCollection<string[]> productos)
		{
			var listaProductos = ObtenerListaProductos(productos);

			var anchoColumnaNombreProd = App.AnchoRetratoDePantalla * 0.55;
			var anchoColumnaDatosProd = App.AnchoRetratoDePantalla - (anchoColumnaNombreProd + 2); // 2 por el ancho del divisor

			var titulo = _nombresColumnas != null && _nombresColumnas.Length > 1 ? _nombresColumnas[1].ToUpper() : "PRODUCTO";
			var encabezado = new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Color.FromHex("#C0C0C0"),
				HeightRequest = productos.Count <= 25 ? 35 : 50,
				Children =
					{
						new Label
						{
							Text = titulo,
							FontSize = 13,
							HorizontalOptions = LayoutOptions.CenterAndExpand,
							FontAttributes = FontAttributes.Bold,
							TextColor = Color.Black,
							VerticalTextAlignment = TextAlignment.Center,
							HorizontalTextAlignment = TextAlignment.Center,
							VerticalOptions = LayoutOptions.Center,
							WidthRequest = anchoColumnaNombreProd
						},
						new Label
						{
							Text = "INFO",
							FontSize = 13,
							HorizontalOptions = LayoutOptions.CenterAndExpand,
							FontAttributes = FontAttributes.Bold,
							TextColor = Color.Black,
							VerticalTextAlignment = TextAlignment.Center,
							HorizontalTextAlignment = TextAlignment.Center,
							VerticalOptions = LayoutOptions.Center,
							WidthRequest = anchoColumnaDatosProd
						}
					}
			};

			var altoTeja = (_listaColumnasParaVer?.Where(x => x == "1").Count() ?? 0) * 17;

			var vista = new ListView
			{
				RowHeight = altoTeja,
				VerticalOptions = LayoutOptions.StartAndExpand,
				HorizontalOptions = LayoutOptions.Fill,
				ItemsSource = listaProductos,
				ItemTemplate = new DataTemplate(() =>
				{
					var nombreProducto = new Label
					{
						FontSize = 16,
						TextColor = Color.FromHex("#1D1D1B"),
						FontAttributes = FontAttributes.Bold,
						VerticalOptions = LayoutOptions.CenterAndExpand,
						WidthRequest = anchoColumnaNombreProd,
						Margin = 3
					};
					nombreProducto.SetBinding(Label.TextProperty, "Nombre");

					var datos = new Label
					{
						FontSize = 15,
						TextColor = Color.FromHex("#1D1D1B"),
						VerticalOptions = LayoutOptions.CenterAndExpand,
						WidthRequest = anchoColumnaDatosProd
					};
					datos.SetBinding(Label.TextProperty, "Datos");

					var separador = new BoxView
					{
						WidthRequest = 2,
						BackgroundColor = Color.FromHex("#FFFFFF"),
						HeightRequest = altoTeja - 5
					};

					var tecla = new StackLayout
					{
						Padding = 2,
						Orientation = StackOrientation.Horizontal,
						Children = { nombreProducto, separador, datos }
					};
					tecla.SetBinding(BackgroundColorProperty, "ColorFondo");

					var celda = new ViewCell { View = tecla };

					celda.Tapped += (sender, args) =>
					{
						if (_ultimoItemSeleccionado != null)
							_ultimoItemSeleccionado.View.BackgroundColor = _ultimoColorSeleccionado;
						IrAlProducto(((ClaseProducto)((ViewCell)sender).BindingContext).Id);
						_ultimoColorSeleccionado = celda.View.BackgroundColor;
						celda.View.BackgroundColor = Color.Silver;
						_ultimoItemSeleccionado = (ViewCell)sender;
					};

					return celda;
				})
			};

			ContenedorTabla.Children.Clear();
			ContenedorTabla.Children.Add(encabezado);
			ContenedorTabla.Children.Add(vista);
		}

		private void FijarProductosYBuscador(List<string[]> productos)
		{
			//Si hay más de 25 productos se muestra el buscador
			if (productos.Count <= 25) return;
			//Almacena la lista de productos en la variable global que usará el buscador
			_productos = productos;
			Buscador.IsVisible = true;
			Buscador.Text = "";
		}

		private void RefrescarUIGrilla()
		{
			//Se quita la grilla para recargarla.
			ContenedorTabla.Children.Clear();
			Buscador.IsVisible = false;
			ContenedorTabla.Children.Add(_indicadorActividad);
		}

		private void RefrescarDatos(bool refrescarLugaresRelaciones = false)
		{
			RefrescarUIGrilla();

			if (CuentaUsuario.ObtenerAccesoDatos() == "G")
				ObtenerDatosProductosDesdeHCG(refrescarLugaresRelaciones); //Hoja de cálculo de Google
			else
				ObtenerProductosDesdeBD(); //Base de Datos
		}

		private async void RefrescarLugCompVtas_RelacionInsProd()
		{
			if (CuentaUsuario.ValidarTokenDeGoogle())
			{
				new LugaresCompraVenta().ObtenerActualizarLugares(_linkHojaConsulta, _servicio);
				new RelacionesInsumoProducto().ObtenerActualizarRelaciones(_linkHojaConsulta, _servicio);
			}
			else
			{
				//Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token
				var paginaAuntenticacion = new PaginaAuntenticacion(true);
				Navigation.InsertPageBefore(paginaAuntenticacion, this);
				await Navigation.PopAsync();
			}
		}

		#endregion

		#region Eventos

		[Android.Runtime.Preserve]
		private  void IrPedidos(object sender, EventArgs args)
		{
			BotonPedidos.BackgroundColor = Color.FromHex("#FB9F0B");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				Navigation.PushAsync(new PedidosGrilla(_servicio), true);
				BotonPedidos.BackgroundColor = Color.FromHex("#FD8A18");
				return false;
			});
		}

		[Android.Runtime.Preserve]
		private void EventoRefrescarDatos(object sender, EventArgs args)
		{
			BotonRefrescar.BackgroundColor = Color.FromHex("#32CEF9");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				RefrescarDatos(true);
				BotonRefrescar.BackgroundColor = Color.FromHex("#32BBF9");
				return false;
			});
		}

		[Android.Runtime.Preserve]
		private void AbrirPaginaEscaner(object sender, EventArgs args)
		{
			BotonEscaner.BackgroundColor = Color.FromHex("#FB9F0B");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				var paginaEscaner = new ZXingScannerPage();

				paginaEscaner.OnScanResult += (result) =>
				{
					// Detiene el escaner
					paginaEscaner.IsScanning = false;

					//Hace autofoco, particularmente para los códigos de barra
					var ts = new TimeSpan(0, 0, 0, 3, 0);
					Device.StartTimer(ts, () =>
					{
						if (paginaEscaner.IsScanning)
							paginaEscaner.AutoFocus();
						return true;
					});

					// Cierra la página del escaner y llama a la página del producto
					Device.BeginInvokeOnMainThread(() =>
					{
						Navigation.PopModalAsync();
						IrAlProducto(result.Text);
					});
				};

				// Abre la página del escaner
				Navigation.PushModalAsync(paginaEscaner);
				BotonEscaner.BackgroundColor = Color.FromHex("#FD8A18");
				return false;
			});
		}

		[Android.Runtime.Preserve]
		private void FiltrarProductos(object sender, EventArgs args)
		{
			if (Buscador.Text.Length > 2 || Buscador.Text.Length == 0)
			{
				//Se quita la grilla para recargarla.
				ContenedorTabla.Children.Clear();
				var productos = new List<string[]>();
				foreach (var producto in _productos)
				{
					if (producto[1].ToLower().Contains(Buscador.Text.ToLower()))
						productos.Add(producto);
				}

				LlenarGrillaDeProductos(productos, true);
			}
		}

		[Android.Runtime.Preserve]
		private void CargarHoja(object sender, EventArgs args)
		{
			Device.StartTimer(TimeSpan.FromMilliseconds(100), () =>
			{
				_linkHojaConsulta = CuentaUsuario.CambiarHojaSeleccionada(_listaHojas.Items[_listaHojas.SelectedIndex]);
				_listaColumnasParaVer = CuentaUsuario.ObtenerColumnasParaVer().Split(',');
				_listaColumnasInventario = CuentaUsuario.ObtenerColumnasInventario().Split(',');

				RefrescarDatos(true);
				return false;
			});
		}

		protected override async void OnSizeAllocated(double ancho, double alto)
		{
			base.OnSizeAllocated(ancho, alto);
			if (_anchoActual == ancho) return;
			if (ancho > alto)
			{
				if (_anchoActual != 0)
					await GrupoEncabezado.TranslateTo(0, -100, 1000);
				GrupoEncabezado.IsVisible = false;
			}
			else
			{
				GrupoEncabezado.IsVisible = true;
				if (_anchoActual != 0)
					await GrupoEncabezado.TranslateTo(0, 0, 1000);
			}
			_anchoActual = ancho;
		}

		//Cuando carga la página y cuando vuelve de registrar un movimiento.
		protected override void OnAppearing()
		{
			RefrescarDatos(_esCargaInicial);
			_esCargaInicial = false; //Si era carga inicial venía en true, si no ya estaba en false
		}

		#endregion
	}

	//Clase Producto: utilizada para armar la lista scrolleable de productos
	[Android.Runtime.Preserve]
	public class ClaseProducto
	{
		[Android.Runtime.Preserve]
		public ClaseProducto(string id, IList<string> datos, bool esTeclaPar, bool tieneBajoStock)
		{
			Id = id;
			Nombre = datos[0];
			Datos = string.Join("", datos.Skip(1).Take(datos.Count));
			ColorFondo = tieneBajoStock
				? Color.FromHex("#FE6161")
				: esTeclaPar
					? Color.FromHex("#EDEDED")
					: Color.FromHex("#E2E2E1");
		}

		[Android.Runtime.Preserve]
		public string Id { get; }
		[Android.Runtime.Preserve]
		public string Nombre { get; }
		[Android.Runtime.Preserve]
		public string Datos { get; }
		[Android.Runtime.Preserve]
		public Color ColorFondo { get; }
	}
}
