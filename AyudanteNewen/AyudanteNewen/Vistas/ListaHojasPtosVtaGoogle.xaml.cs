
using Google.GData.Client;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using System.Collections.Generic;

namespace AyudanteNewen.Vistas
{
	public partial class ListaHojasPtosVtaGoogle
	{
		private readonly AtomEntryCollection _listaHojas;
		private readonly SpreadsheetsService _servicio;

		public ListaHojasPtosVtaGoogle(SpreadsheetsService servicio, AtomEntryCollection listaHojas)
		{
			InitializeComponent();
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			_servicio = servicio;
			_listaHojas = listaHojas;
		}

		private async void EnviarPaginaSeleccionColumnas(string linkHoja)
		{
			if (!CuentaUsuario.ValidarTokenDeGoogle())
			{
				var paginaAuntenticacion = new PaginaAuntenticacion(true);
				Navigation.InsertPageBefore(paginaAuntenticacion, this);
				await Navigation.PopAsync();
			}

			CuentaUsuario.AlmacenarLinkHojaPuntosVentaDeHoja(CuentaUsuario.ObtenerLinkHojaConsulta(), linkHoja);

			ContentPage pagina = new SeleccionColumnasParaVer(_servicio);
			await Navigation.PushAsync(pagina, true);
		}

		private void CargarListaHojas()
		{
			var listaHojas = new List<ClaseHoja>();
			var esTeclaPar = false;
			foreach (var datosHoja in _listaHojas)
			{
				//Sólo lista hojas que contengan la palabra App (es el sufijo que tendrán las hojas para carga de movimientos, las otras son para cálculos y análisis).
				if (!datosHoja.Title.Text.Contains("App")) continue;

				var linkHoja = datosHoja.Links.FindService(GDataSpreadsheetsNameTable.CellRel, null).HRef.ToString();
				var linkHistoricos = datosHoja.Links.FindService(GDataSpreadsheetsNameTable.ListRel, null).HRef.ToString();
				var estaSeleccionada = CuentaUsuario.ObtenerLinkHojaConsulta() == linkHoja; // Tiene que ser la actualmente seleccionada
				var estaUsada = CuentaUsuario.VerificarHojaUsada(linkHoja); // Tiene que haber sido seleccionada alguna vez.
				var esHistorico = CuentaUsuario.VerificarHojaHistoricosUsada(linkHistoricos);
				var esPuntosVenta = CuentaUsuario.VerificarHojaPuntosVentaUsada(linkHoja);

				if (estaSeleccionada || estaUsada || esHistorico) continue;
				var hoja = new ClaseHoja(linkHoja, datosHoja.Title.Text, false, false, false, esPuntosVenta, esTeclaPar);
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
						EnviarPaginaSeleccionColumnas(hoja.Link);
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
			CargarListaHojas();
		}
	}

}
