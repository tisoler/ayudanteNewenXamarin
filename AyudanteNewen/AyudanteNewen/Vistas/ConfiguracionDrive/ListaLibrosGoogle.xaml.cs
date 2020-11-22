
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Servicios;
using System.Collections.Generic;
using System.Threading.Tasks;
using AyudanteNewen.Clases;
using System.Linq;

namespace AyudanteNewen.Vistas
{
	public partial class ListaLibrosGoogle
	{
		private AtomEntryCollection _listaLibros;
		private readonly ServiciosGoogle _servicioGoogle;
		private readonly SpreadsheetsService _servicio;
		private bool _esTeclaPar;
		private readonly ActivityIndicator _indicadorActividad;
		private AtomEntryCollection _listaHojas;
		private string _linkHojaConsulta;
		private string _mensajeError;

		public ListaLibrosGoogle(string tokenDeAcceso)
		{
			InitializeComponent();
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);

			_servicioGoogle = new ServiciosGoogle();
			_servicio = _servicioGoogle.ObtenerServicioParaConsultaGoogleSpreadsheets(tokenDeAcceso);

			_indicadorActividad = new ActivityIndicator
			{
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = true,
				BindingContext = this
			};
			_indicadorActividad.SetBinding(IsVisibleProperty, "IsBusy");
			_indicadorActividad.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
		}

		/* Anterior para usar configuración de hojas y columnas
		private async void EnviarListaHojas(string linkLibro)
		{
			var paginaListaLibros = new ListaHojasCalculoGoogle(_servicio, linkLibro);
			await Navigation.PushAsync(paginaListaLibros, true);
		}
		*/

		private async void ConfigurarIrPlanillaStock(string linkLibro)
		{
			ContenedorLibros.Children.Clear();
			ContenedorLibros.Children.Add(_indicadorActividad);

			await ObtenerYConfigurarHojas(linkLibro);
			if (_mensajeError != "")
			{
				await DisplayAlert("Libro", _mensajeError, "Listo");
				return;
			}

			var paginaGrilla = new PaginaGrilla(_linkHojaConsulta, _servicio);
			App.Instancia.LimpiarNavegadorLuegoIrPagina(paginaGrilla);
		}

		private async Task ObtenerYConfigurarHojas(string linkLibro)
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						_listaHojas = new ServiciosGoogle().ObtenerListaHojas(linkLibro, _servicio);
						ConfigurarHojas();
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

		private void AlmacenarColumnasVerEInventario(string linkHoja) {
			var columnasProducto = new List<string>();
			var celdas = new ServiciosGoogle().ObtenerCeldasDeUnaHoja(linkHoja, _servicio);
			foreach (CellEntry celda in celdas.Entries)
			{
				if (celda.Row == 1) columnasProducto.Add(celda.Value);
				else break;
			}

			// Almacena el arreglo de visibilidad de columnas
			var listaColumnas = Enumerable.Repeat(1, columnasProducto.Count).ToArray();
			// La columna Código no se muestra en PaginaGrilla
			listaColumnas.SetValue(0, 0);
			CuentaUsuario.AlmacenarColumnasParaVerDeHoja(linkHoja, string.Join(",", listaColumnas));

			// Almacena el arreglo de columnas de inventario
			listaColumnas = Enumerable.Repeat(0, columnasProducto.Count).ToArray();
			// Solo se indican como columnas de inventario las que comienzan con la palabra Stock, exceptuando "Stock bajo"
			for (var i = 0; i<columnasProducto.Count; i++)
			{
				var nombreColumna = columnasProducto[i].Trim().ToLower();
				if (nombreColumna != "stock bajo" && (nombreColumna.Contains("stock") || nombreColumna == "total"))
					listaColumnas.SetValue(1, i);
			}
			CuentaUsuario.AlmacenarColumnasInventarioDeHoja(linkHoja, string.Join(",", listaColumnas));
		}

		private void AlmacenarColumnasProducto(string linkHoja)
		{
			var columnasProducto = new List<string>();
			var celdas = new ServiciosGoogle().ObtenerCeldasDeUnaHoja(linkHoja, _servicio);
			foreach (CellEntry celda in celdas.Entries)
			{
				if (celda.Row == 1) columnasProducto.Add(celda.Value);
				else break;
			}
			// Almacena la lista de nombres de columnas
			CuentaUsuario.AlmacenarColumnasProducto(string.Join(",", columnasProducto));
		}

		private void ValidarHabilitacionRelacionesInsumoProducto(string linkHoja)
		{
			foreach (var datosHoja in _listaHojas)
			{
				if (!datosHoja.Title.Text.Equals("Costos variables App")) continue;

				var linkHojaRelacionesInsumoProducto = datosHoja.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
				CuentaUsuario.AlmacenarLinkHojaRelacionesInsumoProducto(linkHoja, linkHojaRelacionesInsumoProducto);
			}
		}

		private string AlmacenarHojaConsulta(string tituloHoja, bool requerida)
        {
			var hojaConsulta = _listaHojas.FirstOrDefault(datosHoja => datosHoja.Title.Text == tituloHoja);
			if (hojaConsulta == null)
				return requerida ? "Libro incorrecto, no tiene hoja " + tituloHoja + "." : "";

			var linkHoja = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
			CuentaUsuario.AlmacenarLinkHojaConsulta(linkHoja);
			CuentaUsuario.AlmacenarNombreDeHoja(linkHoja, tituloHoja);

			AlmacenarColumnasVerEInventario(linkHoja);

			if (tituloHoja.Equals("Productos App"))
			{
				// Link para pantalla PaginaGrilla
				_linkHojaConsulta = linkHoja;
				// 1 - Se valida si tenemos que habilitar la funcionalidad de Relación Insumo - Producto
				// Ocurre si existe en el libro una hoja de nombre "Costos variables".
				// 2 - Almacena las columnas de Producto - Se usarán en la pantalla Pedido (para almacenar movimientos de stock desde el detalle de pedido).
				ValidarHabilitacionRelacionesInsumoProducto(_linkHojaConsulta);
				AlmacenarColumnasProducto(_linkHojaConsulta);
			}
			return "";
		}

		private string AlmacenerHojaHistoricos(string tituloHoja, bool requerida)
        {
			var hojaConsulta = _listaHojas.FirstOrDefault(datosHoja => datosHoja.Title.Text == tituloHoja);
			if (hojaConsulta == null)
				return requerida ? "Libro incorrecto, no tiene hoja " + tituloHoja + "." : "";

			var linkHoja = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
			var linkHojaLista = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null).HRef.ToString();

			//Almacenar la hoja para el historial de movimientos
			CuentaUsuario.AlmacenarLinkHojaHistoricos(linkHojaLista);
			//Almacena la hoja de Histórico en el diccionario para cambiarla cuando se cambie la hoja de stock
			CuentaUsuario.AlmacenarLinkHistoricosDeHoja(CuentaUsuario.ObtenerLinkHojaConsulta(), linkHojaLista);
			//Almacena la hoja de Histórico (celdas) en el diccionario para cambiarla cuando se cambie la hoja de stock
			CuentaUsuario.AlmacenarLinkHistoricosCeldasDeHoja(CuentaUsuario.ObtenerLinkHojaConsulta(), linkHoja);
			return "";
		}

		private void AlmacenarLugar(string tituloHoja)
		{
			var hojaConsulta = _listaHojas.FirstOrDefault(datosHoja => datosHoja.Title.Text == tituloHoja);
			if (hojaConsulta == null) return;
			var linkHoja = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
			CuentaUsuario.AlmacenarLinkHojaPuntosVentaDeHoja(CuentaUsuario.ObtenerLinkHojaConsulta(), linkHoja);
		}

		private string AlmacenarClientes()
		{
			var tituloHoja = "Clientes App";
			var hojaConsulta = _listaHojas.FirstOrDefault(datosHoja => datosHoja.Title.Text == tituloHoja);
			if (hojaConsulta == null)
				return "Libro incorrecto, no tiene hoja " + tituloHoja + ".";

			var linkHoja = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
			CuentaUsuario.AlmacenarLinkHojaClientes(linkHoja);
			return "";
		}

		private string AlmacenarPedidos()
		{
			var tituloHoja = "Pedidos App";
			var hojaConsulta = _listaHojas.FirstOrDefault(datosHoja => datosHoja.Title.Text == tituloHoja);
			if (hojaConsulta == null)
				return "Libro incorrecto, no tiene hoja " + tituloHoja + ".";

			var linkHoja = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
			var linkHojaListaPedidos = hojaConsulta.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null).HRef.ToString();

			CuentaUsuario.AlmacenarLinkHojaPedidos(linkHoja);
			CuentaUsuario.AlmacenarLinkHojaPedidosParaEditar(linkHojaListaPedidos);
			return "";
		}

		private void ConfigurarHojas()
        {
			AlmacenarHojaConsulta("Materias Primas App", false);
			AlmacenerHojaHistoricos("Materias Primas Historico App", false);
			AlmacenarLugar("Proveedores App");

			AlmacenarHojaConsulta("Gastos Generales App", false);
			AlmacenerHojaHistoricos("Gastos Generales Historico App", false);

			AlmacenarHojaConsulta("Caja Pesos App", false);
			AlmacenerHojaHistoricos("Caja Pesos Historico App", false);

			AlmacenarHojaConsulta("Caja Dolares App", false);
			AlmacenerHojaHistoricos("Caja Dolares Historico App", false);

			// Se almacena Productos en último lugar
			// para que la hoja de consulta y de históricos predeterminadas sean las de Productos
			_mensajeError = AlmacenarHojaConsulta("Productos App", true);
			if (_mensajeError != "") return;
			_mensajeError = AlmacenerHojaHistoricos("Productos Historico App", true);
			if (_mensajeError != "") return;
			AlmacenarLugar("Lugares de venta App");

			_mensajeError = AlmacenarClientes();
			if (_mensajeError != "") return;

			_mensajeError = AlmacenarPedidos();
		}

		private async void CargarListaLibros()
		{

			var listaLibros = new List<ClaseLibro>();
			
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						if (_listaLibros == null || _listaLibros.Count == 0)
							_listaLibros = _servicioGoogle.ObtenerListaLibros(_servicio);
						foreach (var datosLibro in _listaLibros)
						{
							var libro = new ClaseLibro(datosLibro.Links.FindService(GDataSpreadsheetsNameTable.WorksheetRel, null).HRef.ToString(), datosLibro.Title.Text);
							listaLibros.Add(libro);
						}
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

			var vista = new ListView
			{
				RowHeight = 60,
				VerticalOptions = LayoutOptions.StartAndExpand,
				HorizontalOptions = LayoutOptions.Fill,
				ItemsSource = listaLibros,
				ItemTemplate = new DataTemplate(() =>
				{
					var nombreLibro = new Label
					{
						FontSize = 18,
						TextColor = Color.FromHex("#1D1D1B"),
						VerticalOptions = LayoutOptions.CenterAndExpand,
						HorizontalOptions = LayoutOptions.CenterAndExpand
					};
					nombreLibro.SetBinding(Label.TextProperty, "Nombre");
					
					var celda = new ViewCell
					{
						View = new StackLayout
						{
							Orientation = StackOrientation.Horizontal,
							Children = { nombreLibro }
						}
					};
					
					celda.Tapped += (sender, args) =>
					{
						ConfigurarIrPlanillaStock(((ClaseLibro)((ViewCell)sender).BindingContext).Link);
						celda.View.BackgroundColor = Color.Silver;
					};

					celda.Appearing += (sender, args) =>
					{
						var viewCell = (ViewCell)sender;
						if (viewCell.View != null)
						{
							viewCell.View.BackgroundColor = _esTeclaPar ? Color.FromHex("#EDEDED") : Color.FromHex("#E2E2E1");
						}
						_esTeclaPar = !_esTeclaPar;
					};

					return celda;
				})
			};

			ContenedorLibros.Children.Clear();
			ContenedorLibros.Children.Add(vista);
		}

		//Cuando carga la página y cuando vuelve.
		protected override void OnAppearing()
		{
			RefrescarDatos();
		}

		private void RefrescarDatos()
		{
			//Se quita la grilla para recargarla.
			ContenedorLibros.Children.Clear();
			ContenedorLibros.Children.Add(_indicadorActividad);
			CargarListaLibros();
		}
	}

	//Clase Libro: utilizada para armar la lista scrolleable de libros
	[Android.Runtime.Preserve]
	public class ClaseLibro
	{
		[Android.Runtime.Preserve]
		public ClaseLibro(string link, string nombre)
		{
			Link = link;
			Nombre = nombre;
		}

		[Android.Runtime.Preserve]
		public string Link { get; }
		[Android.Runtime.Preserve]
		public string Nombre { get; }
	}
}
