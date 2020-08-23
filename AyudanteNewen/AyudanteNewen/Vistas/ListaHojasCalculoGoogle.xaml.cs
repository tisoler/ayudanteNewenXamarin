
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using System.Collections.Generic;
using System.Threading.Tasks;
using AyudanteNewen.Servicios;

namespace AyudanteNewen.Vistas
{
	public partial class ListaHojasCalculoGoogle
	{
		private AtomEntryCollection _listaHojas;
		private readonly SpreadsheetsService _servicio;
		private readonly string _linkLibro;
		private readonly ActivityIndicator _indicadorActividad;

		public ListaHojasCalculoGoogle(SpreadsheetsService servicio, string linkLibro)
		{
			InitializeComponent();
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			_servicio = servicio;
			_linkLibro = linkLibro;

			_indicadorActividad = new ActivityIndicator
			{
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = true,
				BindingContext = this
			};
			_indicadorActividad.SetBinding(IsVisibleProperty, "IsBusy");
			_indicadorActividad.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
		}

		private async void EnviarPagina(string linkHoja, string nombreHoja, bool desvinculaHoja)
		{
			//Si está desvinculando borra todas las configuraciones anteriores de la hoja seleccionada
			if (desvinculaHoja)
			{
				CuentaUsuario.ReiniciarHoja(linkHoja);
				CargarListaHojas();
			}
			else
			{
				ContentPage pagina;
				//Si ya se usó esta hoja alguna vez y si el botón presionado NO es el de Desvincular, carga las columnas ya seleccionadas y envía a Grilla.
				//Si no (alguna de las dos condiciones) envía a pantallas de selección de histórico y columnas.
				if (CuentaUsuario.VerificarHojaUsadaRecuperarColumnas(linkHoja))
					App.Instancia.LimpiarNavegadorLuegoIrPagina(new PaginaGrilla(linkHoja, _servicio));
				else
				{
					if (CuentaUsuario.VerificarHojaUsada(linkHoja))
					{
						await DisplayAlert("Configuración incompleta",
							"La hoja a la que intenta acceder tiene una configuración incompleta, por favor, desvincule la hoja y vuelva a configurarla.",
							"Listo");
						return;
					}

					if (CuentaUsuario.VerificarHojaUsadaPorNombre(nombreHoja))
					{
						await DisplayAlert("Hoja con el mismo nombre",
							"Ya existe una hoja con el mismo nombre en uso, por favor, modifique el nombre de la hoja y vuelva a configurarla.",
							"Listo");
						return;
					}

					//Se almacena el link para recobrar los datos de stock de la hoja cuando ingrese nuevamente.
					CuentaUsuario.AlmacenarLinkHojaConsulta(linkHoja);
					CuentaUsuario.AlmacenarNombreDeHoja(linkHoja, nombreHoja);

					//Se ha seleccionado una hoja principal (de nombre "Productos App"), se valida si tenemos que habilitar la funcionalidad de Relación Insumo - Producto
					//Ocurre si existe en el libro una hoja de nombre "Costos variables"
					if (nombreHoja == "Productos App")
						ValidarHabilitacionRelacionesInsumoProducto(linkHoja);

					pagina = new ListaHojasHistoricoGoogle(_servicio, _listaHojas);
					await Navigation.PushAsync(pagina, true);
				}
			}
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

		private async void CargarListaHojas()
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						if (_listaHojas == null || _listaHojas.Count == 0)
							_listaHojas = new ServiciosGoogle().ObtenerListaHojas(_linkLibro, _servicio);
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

			var listaHojas = new List<ClaseHoja>();
			var esTeclaPar = false;
			foreach (var datosHoja in _listaHojas)
			{
				//Sólo lista hojas que contengan la palabra App (es el sufijo que tendrán las hojas para carga de movimientos, las otras son para cálculos y análisis).
				if (!datosHoja.Title.Text.Contains("App")) continue;

				var linkHoja = datosHoja.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
				var linkHistoricos = datosHoja.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null).HRef.ToString();
				var estaUsada = CuentaUsuario.VerificarHojaUsada(linkHoja); // Tiene que haber sido seleccionada alguna vez.
				var esHistorico = CuentaUsuario.VerificarHojaHistoricosUsada(linkHistoricos);
				var esPuntosVenta = CuentaUsuario.VerificarHojaPuntosVentaUsada(linkHoja);
				//Agrega hoja (tecla) para potencial hoja de stock.
				//Si ya está siendo usada le agrega ícono.
				//Si en cambio es hoja de Históricos le pone el ícono correspondiente.
				//Nunca es hoja en uso de stock y de históricos; es una, otra o ninguna.
				var hoja = new ClaseHoja(linkHoja, datosHoja.Title.Text, false, estaUsada, esHistorico, esPuntosVenta, esTeclaPar);
				listaHojas.Add(hoja);
				esTeclaPar = !esTeclaPar;

				if (!estaUsada) continue;
				//Si la hoja está siendo usada, agrega hoja (tecla) para desvincular (con ícono).
				hoja = new ClaseHoja(linkHoja, "Desvincular " + datosHoja.Title.Text, true, false, false, false, esTeclaPar);
				listaHojas.Add(hoja);
				esTeclaPar = !esTeclaPar;
			}

			var vista = new ListView
			{
				RowHeight = 60,
				VerticalOptions = LayoutOptions.StartAndExpand,
				HorizontalOptions = LayoutOptions.Fill,
				ItemsSource = listaHojas,
				ItemTemplate = new DataTemplate(() =>
				{
					var nombreHoja = new Label
					{
						FontSize = 18,
						TextColor = Color.FromHex("#1D1D1B"),
						VerticalOptions = LayoutOptions.CenterAndExpand,
						HorizontalOptions = LayoutOptions.CenterAndExpand
					};
					nombreHoja.SetBinding(Label.TextProperty, "Nombre");

					var icono = new Image
					{
						VerticalOptions = LayoutOptions.Center,
						HorizontalOptions = LayoutOptions.End,
						HeightRequest = App.AnchoRetratoDePantalla * .09259
					};
					icono.SetBinding(Image.SourceProperty, "ArchivoIcono");
					icono.SetBinding(IsVisibleProperty, "TieneImagen");

					var tecla = new StackLayout
					{
						Padding = 7,
						Orientation = StackOrientation.Horizontal,
						Children = {nombreHoja, icono}
					};
					tecla.SetBinding(BackgroundColorProperty, "ColorFondo");

					var celda = new ViewCell { View = tecla };
					
					celda.Tapped += (sender, args) =>
					{
						var hoja = (ClaseHoja)((ViewCell)sender).BindingContext;
						EnviarPagina(hoja.Link, hoja.Nombre, hoja.Desvincular);
						celda.View.BackgroundColor = Color.Silver;
					};

					return celda;
				})
			};

			ContenedorHojas.Children.Clear();
			ContenedorHojas.Children.Add(vista);
		}

		//Cuando carga la página y cuando vuelve.
		protected override void OnAppearing()
		{
			RefrescarDatos();
		}

		private void RefrescarDatos()
		{
			//Se quita la grilla para recargarla.
			ContenedorHojas.Children.Clear();
			ContenedorHojas.Children.Add(_indicadorActividad);
			CargarListaHojas();
		}
	}

	//Clase hoja: utilizada para armar la lista scrolleable de hojas
	[Android.Runtime.Preserve]
	public class ClaseHoja
	{
		[Android.Runtime.Preserve]
		public ClaseHoja(string link, string nombre, bool desvincular, bool esInventario, bool esHistorico, bool esPuntoVenta, bool esTeclaPar, 
			string linkHistoricoCeldas = null)
		{
			Link = link;
			Nombre = nombre;
			Desvincular = desvincular;
			TieneImagen = desvincular || esInventario || esHistorico || esPuntoVenta;
			var nombreArchivoIcono = desvincular ? "refrescarHoja" : esInventario ? "hojaInventario" : esHistorico ? "hojaHistoricos" : "hojaPtoVta";
			ArchivoIcono = ImageSource.FromResource($"AyudanteNewen.Imagenes.{nombreArchivoIcono}.png");
			ColorFondo = esTeclaPar ? Color.FromHex("#EDEDED") : Color.FromHex("#E2E2E1");
			LinkHistoricoCeldas = linkHistoricoCeldas;
		}

		[Android.Runtime.Preserve]
		public string Link { get; }
		[Android.Runtime.Preserve]
		public string Nombre { get; }
		[Android.Runtime.Preserve]
		public bool Desvincular { get; }
		[Android.Runtime.Preserve]
		public bool TieneImagen { get; }
		[Android.Runtime.Preserve]
		public ImageSource ArchivoIcono { get; }
		[Android.Runtime.Preserve]
		public Color ColorFondo { get; }
		[Android.Runtime.Preserve]
		public string LinkHistoricoCeldas { get; }
	}

}
