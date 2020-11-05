
using System;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AyudanteNewen.Vistas
{
	public partial class NuevoPedido
	{
		private readonly SpreadsheetsService _servicio;
		private readonly ServiciosGoogle _servicioGoogle;
		private List<string[]> _clientes;
		private List<string[]> _productos;
		private int _indiceProducto = 0;

		public NuevoPedido(SpreadsheetsService servicio)
		{
			InitializeComponent();
			_servicioGoogle = new ServiciosGoogle();
			_servicio = servicio;
			ObtenerDatosClientesDesdeHCG();
		}

		private List<string[]> ObtenerListado(string linkHoja)
        {
			var celdas = _servicioGoogle.ObtenerCeldasDeUnaHoja(linkHoja, _servicio);

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

		private async void ObtenerDatosClientesDesdeHCG()
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						var linkHojaClientes = CuentaUsuario.ObtenerLinkHojaClientes();
						_clientes = ObtenerListado(linkHojaClientes);
						var linkHojaProductos = CuentaUsuario.ObtenerLinkHojaPorNombre("Productos App");
						_productos = ObtenerListado(linkHojaProductos);
						ConstruirVista();
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

		private void ConstruirVista()
        {
			var anchoEtiqueta = App.AnchoRetratoDePantalla / 3 - 10;
			var anchoCampo = App.AnchoRetratoDePantalla / 3 * 2 - 30;
			var altoCampos = 55;

			var etiquetaCliente = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Cliente",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black
			};

			var comboClientes = new Picker
			{
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				VerticalOptions = LayoutOptions.Center,
				StyleId = "comboCliente",
				WidthRequest = anchoCampo - 55
			};
			foreach (var cliente in _clientes)
			{
				comboClientes.Items.Add(cliente?[1]);
			}

			var vistaCliente = new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaCliente, comboClientes }
			};

			var botonAgregarProducto = new Button
			{
				Text = "Agregar producto",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.CenterAndExpand,
				FontSize = 13,
				HeightRequest = altoCampos,
				BackgroundColor = Color.FromHex("#32CEF9")
			};
			botonAgregarProducto.Clicked += AgregarProducto;

			var vistaBotonAgregar = new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				Margin = 5,
				VerticalOptions = LayoutOptions.Fill,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Vertical,
				HeightRequest = altoCampos * 3,
				Children = { botonAgregarProducto },
				StyleId = "agregarProducto",
			};

			var etiquetaNuevoProducto = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Producto",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black
			};

			var comboProductos = new Picker
			{
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				VerticalOptions = LayoutOptions.Center,
				StyleId = "comboProducto",
				WidthRequest = anchoCampo - 55
			};
			foreach (var producto in _productos)
			{
				comboProductos.Items.Add(producto?[1]);
			}

			var vistaProducto = new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaNuevoProducto, comboProductos }
			};

			var etiquetaCantidad = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Cantidad",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black
			};

			var campoCantidad = new Entry
			{
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.Start,
				StyleId = "cantidad-" + _indiceProducto,
				WidthRequest = anchoCampo - 65,
				Keyboard = Keyboard.Numeric
			};

			var vistaCantidad = new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaCantidad, campoCantidad }
			};

			var botonGuardarProducto = new Button
			{
				Text = "Guardar producto",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				FontSize = 13,
				HeightRequest = altoCampos,
				BackgroundColor = Color.FromHex("#FD8A18")
			};
			botonGuardarProducto.Clicked += GuardarProducto;

			var vistaNuevoProducto = new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				Margin = 5,
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Vertical,
				HeightRequest = altoCampos * 3,
				Children = { vistaProducto, vistaCantidad, botonGuardarProducto },
				IsVisible = false,
				StyleId = "nuevoProducto"
			};

			Device.BeginInvokeOnMainThread(() =>
			{
				ContenedorPedido.Children.Clear();
				ContenedorPedido.Children.Add(vistaCliente);
				ContenedorPedido.Children.Add(vistaBotonAgregar);
				ContenedorPedido.Children.Add(vistaCliente);
				ContenedorPedido.Children.Add(vistaNuevoProducto);
			});
		}

		private void AgregarProducto(object sender, EventArgs e)
		{
			_indiceProducto += 1;
			var boton = (Button)sender;
			((StackLayout)(boton.Parent)).IsVisible = false;
			foreach (var control in ContenedorPedido.Children)
			{
				if (control.StyleId == "nuevoProducto") control.IsVisible = true;
			}
		}

		private void GuardarProducto(object sender, EventArgs e)
		{
			var boton = (Button)sender;
			((StackLayout)(boton.Parent)).IsVisible = false;
			foreach (var control in ContenedorPedido.Children)
			{
				if (control.StyleId == "agregarProducto") control.IsVisible = true;
			}
		}
	}
}
