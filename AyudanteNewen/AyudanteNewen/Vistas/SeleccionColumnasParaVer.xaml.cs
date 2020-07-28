
using System;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Servicios;
using AyudanteNewen.Clases;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AyudanteNewen.Vistas
{
	public partial class SeleccionColumnasParaVer
	{
		private readonly SpreadsheetsService _servicio;
		private int[] _listaColumnas;
		private List<CellEntry> _columnas;
		private string _linkHojaConsulta;
		private CellFeed _celdas;
		private readonly ActivityIndicator _indicadorActividad;
		private Image _listo;

		public SeleccionColumnasParaVer(SpreadsheetsService servicio)
		{
			InitializeComponent();
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			ConfigurarBotones();
			_servicio = servicio;

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
			_listo = App.Instancia.ObtenerImagen(TipoImagen.BotonListo);
			_listo.GestureRecognizers.Add(new TapGestureRecognizer(Listo));

			ContenedorBotones.Children.Add(_listo);
		}

		private async void ObtenerColumnas()
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						_linkHojaConsulta = CuentaUsuario.ObtenerLinkHojaConsulta();
						if (_celdas == null || _celdas.RowCount.Count == 0)
							_celdas = new ServiciosGoogle().ObtenerCeldasDeUnaHoja(_linkHojaConsulta, _servicio);
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

			_columnas = new List<CellEntry>(); //Columnas para listar en pantalla, si hay una de nombre Usuario - Movimiento se quita.

			foreach (CellEntry celda in _celdas.Entries)
			{
				if (celda.Row == 1)
				{
					_columnas.Add(celda);
				}
				else
				{
					break;
				}
			}

			_listaColumnas = Enumerable.Repeat(1, _columnas.Count).ToArray(); //El arreglo de columnas para ver, todas con valor inicial en 1
			LlenarGrillaColumnasParaVer(_columnas);
		}

		private void LlenarGrillaColumnasParaVer(List<CellEntry> columnas)
		{
			ContenedorColumnas.Children.Clear();

			var esGris = false;
			foreach (var columna in columnas)
			{
				var etiquetaColumna = new Label
				{
					TextColor = Color.Black,
					HorizontalOptions = LayoutOptions.StartAndExpand,
					HorizontalTextAlignment = TextAlignment.Start,
					VerticalOptions = LayoutOptions.Center,
					Text = columna.Value,
					FontSize = 18
				};

				var seleccionar = new Switch
				{
					HorizontalOptions = LayoutOptions.End,
					VerticalOptions = LayoutOptions.Center,
					StyleId = columna.Column.ToString()
				};
				seleccionar.Toggled += AgregarColumna;

				var itemColumna = new StackLayout
				{
					VerticalOptions = LayoutOptions.Start,
					HorizontalOptions = LayoutOptions.Fill,
					Orientation = StackOrientation.Horizontal,
					HeightRequest = 45,
					WidthRequest = 300,
					Children = { etiquetaColumna, seleccionar },
					Spacing = 0,
					Padding = 4,
					BackgroundColor = esGris ? Color.Silver : Color.FromHex("#F5F5F5")
				};

				esGris = !esGris;
				ContenedorColumnas.Children.Add(itemColumna);
			}
		}

		[Android.Runtime.Preserve]
		private void AgregarColumna(object sender, ToggledEventArgs e)
		{
			var ficha = (Switch)sender;
			_listaColumnas.SetValue(Convert.ToInt32(!e.Value), Convert.ToInt32(ficha.StyleId) - 1);
		}

		[Android.Runtime.Preserve]
		private void Listo(View arg1, object arg2)
		{
			_listo.Opacity = 0.5f;
			Device.StartTimer(TimeSpan.FromMilliseconds(300), () =>
			{
				CuentaUsuario.AlmacenarColumnasParaVerDeHoja(_linkHojaConsulta, string.Join(",", _listaColumnas));
				var paginaSeleccionColumnasInventario = new SeleccionColumnasInventario(_columnas, _linkHojaConsulta, _servicio);
				Navigation.PushAsync(paginaSeleccionColumnasInventario, true);

				_listo.Opacity = 1f;
				return false;
			});
		}

		//Cuando carga la página y cuando vuelve.
		protected override void OnAppearing()
		{
			RefrescarDatos();
		}

		private void RefrescarDatos()
		{
			//Se quita la grilla para recargarla.
			ContenedorColumnas.Children.Clear();
			ContenedorColumnas.Children.Add(_indicadorActividad);
			ObtenerColumnas();
		}

	}
}
