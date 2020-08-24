
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Servicios;
using System.Collections.Generic;
using System.Threading.Tasks;
using AyudanteNewen.Clases;

namespace AyudanteNewen.Vistas
{
	public partial class ListaLibrosGoogle
	{
		private AtomEntryCollection _listaLibros;
		private readonly ServiciosGoogle _servicioGoogle;
		private readonly SpreadsheetsService _servicio;
		private bool _esTeclaPar;
		private readonly ActivityIndicator _indicadorActividad;

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

		private async void EnviarListaHojas(string linkLibro)
		{
			var paginaListaLibros = new ListaHojasCalculoGoogle(_servicio, linkLibro);
			await Navigation.PushAsync(paginaListaLibros, true);
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
						EnviarListaHojas(((ClaseLibro)((ViewCell)sender).BindingContext).Link);
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
