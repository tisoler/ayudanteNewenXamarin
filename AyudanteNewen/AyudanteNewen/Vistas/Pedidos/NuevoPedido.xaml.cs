
using System;
using Google.GData.Spreadsheets;
using Xamarin.Forms;
using AyudanteNewen.Clases;
using AyudanteNewen.Servicios;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace AyudanteNewen.Vistas
{
	public partial class NuevoPedido
	{
		private readonly SpreadsheetsService _servicio;
		private readonly ServiciosGoogle _servicioGoogle;
		private List<string[]> _clientes;
		private List<string[]> _productos;
		private ObservableCollection<DetallePedido> _listaProducto;
		private string _mensaje;
		private ActivityIndicator _indicadorActividad;
		List<Clases.Pedido> _listaPedidos;
		Dictionary<string, uint> _valoreComboInventarios;

		public NuevoPedido(SpreadsheetsService servicio, List<Clases.Pedido> listaPedidos)
		{
			InitializeComponent();
			_servicioGoogle = new ServiciosGoogle();
			_servicio = servicio;
			ObtenerDatosClientesDesdeHCG();
			_listaProducto = new ObservableCollection<DetallePedido>();
			_listaPedidos = listaPedidos;

			_indicadorActividad = new ActivityIndicator
			{
				VerticalOptions = LayoutOptions.CenterAndExpand,
				IsEnabled = true,
				BindingContext = this
			};
			_indicadorActividad.SetBinding(IsVisibleProperty, "IsBusy");
			_indicadorActividad.SetBinding(ActivityIndicator.IsRunningProperty, "IsBusy");
			ContenedorPedido.Children.Add(_indicadorActividad);
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

		private StackLayout ObtenerControlCliente(double anchoEtiqueta, double anchoCampo, int altoCampos)
		{
			var etiquetaCliente = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Cliente:",
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

			return new StackLayout
			{
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaCliente, comboClientes }
			};
		}

		private StackLayout ObtenerControlProducto(double anchoEtiqueta, double anchoCampo, int altoCampos)
		{
			// Producto
			var etiquetaNuevoProducto = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Producto:",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black
			};

			var comboProductos = new Picker
			{
				HorizontalOptions = LayoutOptions.StartAndExpand,
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
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaNuevoProducto, comboProductos }
			};

			//Cantidad
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
				StyleId = "campoCantidad",
				WidthRequest = anchoCampo - 65,
				Keyboard = Keyboard.Numeric
			};

			var vistaCantidad = new StackLayout
			{
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaCantidad, campoCantidad }
			};

			// Lugares Inventario
			var etiquetaInventario = new Label
			{
				HorizontalOptions = LayoutOptions.EndAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.End,
				Text = "Lugar stock",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black
			};

			var columnasInventario = CuentaUsuario.ObtenerColumnasInventario();
			var listaColumnasInventario = !string.IsNullOrEmpty(columnasInventario) ? columnasInventario.Split(',') : null;
			var columnasProductos = CuentaUsuario.ObtenerColumnasProductos()?.Split(',');
			_valoreComboInventarios = new Dictionary<string, uint>();
			for (uint i = 0; i < listaColumnasInventario.Length; i++)
			{
				if (listaColumnasInventario[i] == "1")
					_valoreComboInventarios[columnasProductos[i]] = i;
			}

			var comboInventarios = new Picker
			{
				HorizontalOptions = LayoutOptions.StartAndExpand,
				VerticalOptions = LayoutOptions.Center,
				StyleId = "comboInventario",
				WidthRequest = anchoCampo - 55
			};
			foreach (var inventario in _valoreComboInventarios)
			{
				comboInventarios.Items.Add(inventario.Key);
			}

			var vistaInventarios = new StackLayout
			{
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaInventario, comboInventarios }
			};

			// Botón guardar
			var botonGuardarProducto = new Button
			{
				Text = "Agregar producto",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				FontSize = 13,
				HeightRequest = 50,
				BackgroundColor = Color.FromHex("#FD8A18")
			};
			botonGuardarProducto.Clicked += AgregarProducto;

			return new StackLayout
			{
				BackgroundColor = Color.FromHex("#FFFFFF"),
				Margin = 5,
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Vertical,
				HeightRequest = altoCampos * 4.5,
				Children = { vistaProducto, vistaCantidad, vistaInventarios, botonGuardarProducto }
			};
		}

		private Label ObtenerEtiquetaTitulo(string texto, double ancho)
		{
			return new Label
			{
				Text = texto,
				FontSize = 13,
				HorizontalOptions = LayoutOptions.Start,
				FontAttributes = FontAttributes.Bold,
				TextColor = Color.Black,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalOptions = LayoutOptions.Center,
				WidthRequest = ancho
			};
		}

		private Label ObtenerEtiquetaDato(string campo, double ancho)
		{
			var etiquetaDato = new Label
			{
				FontSize = 14,
				TextColor = Color.FromHex("#1D1D1B"),
				VerticalOptions = LayoutOptions.CenterAndExpand,
				HorizontalOptions = LayoutOptions.Start,
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

		private StackLayout ObtenerEncabezadoGrilla(int anchoColumna)
		{
			return new StackLayout
			{
				Orientation = StackOrientation.Horizontal,
				VerticalOptions = LayoutOptions.Start,
				BackgroundColor = Color.FromHex("#C0C0C0"),
				HeightRequest = 30,
				Padding = 3,
				Children =
					{
						ObtenerEtiquetaTitulo("Producto", anchoColumna * 1.25),
						ObtenerEtiquetaTitulo("Cantidad", anchoColumna * 0.75),
						ObtenerEtiquetaTitulo("Precio", anchoColumna)
					}
			};
		}

		private ListView ObtenerDetalleGrilla(int anchoGrilla, int altoGrilla, int anchoColumna)
		{
			var altoTeja = 40;
			var vista = new ListView
			{
				
				RowHeight = altoTeja,
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Center,
				ItemsSource = _listaProducto,
				WidthRequest = anchoGrilla,
				HeightRequest = altoGrilla,
				ItemTemplate = new DataTemplate(() =>
				{
					var etiquetaIconoEliminar = new Label
					{
						FontFamily = "FontAwesome5Solid.otf#Regular",
						HorizontalTextAlignment = TextAlignment.Center,
						FontSize = 22,
						TextColor = Color.FromHex("#ffffff"),
						VerticalTextAlignment = TextAlignment.Center,
						VerticalOptions = LayoutOptions.CenterAndExpand,
						HorizontalOptions = LayoutOptions.CenterAndExpand,
						Text = "\uf2ed"
					};

					var contenedorEliminar = new StackLayout
					{
						WidthRequest = 45,
						VerticalOptions = LayoutOptions.FillAndExpand,
						Orientation = StackOrientation.Vertical,
						Children = { etiquetaIconoEliminar },
						BackgroundColor = Color.FromHex("#FD8A18")
					};
					contenedorEliminar.SetBinding(ClassIdProperty, "IdProducto");
					contenedorEliminar.GestureRecognizers.Add(new TapGestureRecognizer(EliminarLineaDetalle));

					var tecla = new StackLayout
					{
						Padding = 3,
						Orientation = StackOrientation.Horizontal,
						Children = {
							ObtenerEtiquetaDato("NombreProducto", anchoColumna * 1.25),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("Cantidad", anchoColumna * 0.75),
							ObtenerSeparado(altoTeja),
							ObtenerEtiquetaDato("Precio", anchoColumna),
							contenedorEliminar
						}
					};
					tecla.SetBinding(BackgroundColorProperty, "ColorFondo");

					return new ViewCell { View = tecla };
				}),
				StyleId = "listaProductos"
			};

			return vista;
		}

		private StackLayout ObtenerControlTotal(double anchoEtiqueta, int altoCampos)
		{
			var etiquetaTotal = new Label
			{
				HorizontalOptions = LayoutOptions.CenterAndExpand,
				VerticalOptions = LayoutOptions.Center,
				HorizontalTextAlignment = TextAlignment.Center,
				Text = "Total: 0",
				FontSize = 16,
				WidthRequest = anchoEtiqueta,
				TextColor = Color.Black,
				StyleId = "total"
			};

			return new StackLayout
			{
				VerticalOptions = LayoutOptions.Start,
				HorizontalOptions = LayoutOptions.Fill,
				Orientation = StackOrientation.Horizontal,
				HeightRequest = altoCampos,
				Children = { etiquetaTotal }
			};
		}

		private StackLayout ObtenerBotonGuardar()
		{
			// Botón eliminar
			var etiquetaIconoGuardar = new Label
			{
				FontFamily = "FontAwesome5Solid.otf#Regular",
				HorizontalTextAlignment = TextAlignment.Center,
				FontSize = 22,
				TextColor = Color.FromHex("#ffffff"),
				VerticalTextAlignment = TextAlignment.Center,
				VerticalOptions = LayoutOptions.EndAndExpand,
				Text = "\uf0c7"
			};

			var etiquetaGuardar = new Label
			{
				HorizontalTextAlignment = TextAlignment.Center,
				FontSize = 14,
				TextColor = Color.FromHex("#ffffff"),
				VerticalTextAlignment = TextAlignment.Center,
				VerticalOptions = LayoutOptions.StartAndExpand,
				Text = "Guardar"
			};

			var contenedorBotonGuardar = new StackLayout
			{
				HorizontalOptions = LayoutOptions.FillAndExpand,
				HeightRequest = 65,
				Orientation = StackOrientation.Vertical,
				Children = { etiquetaIconoGuardar, etiquetaGuardar },
				BackgroundColor = Color.FromHex("#32BBF9"),
				GestureRecognizers = { new TapGestureRecognizer(EventoGuardarPedido) }
			};
			// contenedorBotonGuardar.GestureRecognizers.Add(new TapGestureRecognizer(EventoGuardarPedido));
			return contenedorBotonGuardar;
		}

		private void ConstruirVista()
		{
			var anchoEtiqueta = App.AnchoRetratoDePantalla / 3 - 10;
			var anchoCampo = App.AnchoRetratoDePantalla / 3 * 2 - 30;
			var altoCampos = 45;

			//  Combo Cliente
			var vistaCliente = ObtenerControlCliente(anchoEtiqueta, anchoCampo, altoCampos);
			// Controles para agregar producto
			var vistaNuevoProducto = ObtenerControlProducto(anchoEtiqueta, anchoCampo, altoCampos);
			// Encabezado grilla
			var anchoGrilla = (int)(App.AnchoRetratoDePantalla * 0.99);
			var altoGrilla = (int)(App.AnchoApaisadoDePantalla * 0.24); // AnchoApaisadoDePantalla es el alto en retrato
			var anchoColumna = (anchoGrilla - 45) / 3 - 2; // -2 por el divisor (2px), -45 por el tacho (45px) 
			var vistaEncabezadoGrilla = ObtenerEncabezadoGrilla(anchoColumna);
			// Detalle grilla
			var vistaGrillaProducto = ObtenerDetalleGrilla(anchoGrilla, altoGrilla, anchoColumna);
			// Total
			var vistaTotal = ObtenerControlTotal(anchoEtiqueta, altoCampos);
			// Botón guardar
			var botonGuardar = ObtenerBotonGuardar();

			Device.BeginInvokeOnMainThread(() =>
			{
				ContenedorClienteProducto.Children.Clear();
				ContenedorClienteProducto.Children.Add(vistaCliente);
				ContenedorClienteProducto.Children.Add(vistaNuevoProducto);
				ContenedorGrillaTotal.Children.Clear();
				ContenedorGrillaTotal.Children.Add(vistaEncabezadoGrilla);
				ContenedorGrillaTotal.Children.Add(vistaGrillaProducto);
				ContenedorGrillaTotal.Children.Add(vistaTotal);
				ContenedorGrillaTotal.Children.Add(botonGuardar);
			});
		}

        private View BuscarControlEnHijos(StackLayout controlPadre, string id)
		{
			foreach (var control in controlPadre.Children)
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

		private async void GuardarPedido()
        {
			var comboCliente = (Picker)BuscarControlEnHijos(ContenedorPedido, "comboCliente");
			if (comboCliente.SelectedIndex < 0)
			{
				await DisplayAlert("Pedido", "Debe ingresar un cliente.", "Listo");
				return;
			}
			if (_listaProducto.Count == 0)
			{
				await DisplayAlert("Pedido", "Debe agregar productos.", "Listo");
				return;
			}

			await TareaGuardarPedido(comboCliente);

			await Navigation.PopAsync();
			await DisplayAlert("Pedido", _mensaje, "Listo");
		}

		private async Task TareaGuardarPedido(Picker comboCliente)
		{
			try
			{
				IsBusy = true;

				await Task.Run(async () =>
				{
					if (CuentaUsuario.ValidarTokenDeGoogle())
					{
						GuardarPedidoHojaDeCalculoGoogle(comboCliente);
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

		private async void GuardarPedidoHojaDeCalculoGoogle(Picker comboCliente)
		{
			_mensaje = "Ha ocurrido un error mientras se guardaba el pedido.";
			var servicioGoogle = new ServiciosGoogle();
			var grabo = false;

			var idCliente = _clientes[comboCliente.SelectedIndex][0];
			var cliente = _clientes[comboCliente.SelectedIndex][1];
			var idPedido = _listaPedidos.OrderByDescending(p => Convert.ToInt32(p.Id)).First()?.Id ?? "0";

			var pedido = new Clases.Pedido()
			{
				Id = (Convert.ToInt32(idPedido) + 1).ToString(),
				Fecha = DateTime.Now.ToString("dd/MM/yyyy"),
				IdCliente = idCliente,
				Cliente = cliente,
				Detalle = _listaProducto.ToList(),
				FechaEntrega = DateTime.Now.ToString("dd/MM/yyyy"),
				Estado = "Pendiente",
				Usuario = CuentaUsuario.ObtenerNombreUsuarioGoogle() ?? "-",
				Comentario = "-",
				Lugar = "-"
			};

			try
			{
				//Ingresa el pedido en la tabla
				servicioGoogle.InsertarPedido(_servicio, pedido);
				grabo = true;
			}
			catch (Exception)
			{
				// Si se quedó la pantalla abierta un largo tiempo y se venció el token, se cierra y refresca el token
				var paginaAuntenticacion = new PaginaAuntenticacion(true);
				Navigation.InsertPageBefore(paginaAuntenticacion, this);
				await Navigation.PopAsync();
			}
			_mensaje = grabo ? "El pedido ha sido guardado correctamente." : "No se ha registrado el pedido.";
		}

		#region Eventos

		private void AgregarProducto(object sender, EventArgs e)
		{
			var comboProducto = (Picker)BuscarControlEnHijos(ContenedorPedido, "comboProducto");
			var campoCantidad = (Entry)BuscarControlEnHijos(ContenedorPedido, "campoCantidad");
			var comboInventario = (Picker)BuscarControlEnHijos(ContenedorPedido, "comboInventario");

			if (comboProducto.SelectedIndex < 0 || string.IsNullOrEmpty(campoCantidad.Text) || comboInventario.SelectedIndex < 0)
			{
				DisplayAlert("Produto", "Debe ingresar producto, cantidad y lugar de stock.", "Listo");
				return;
			}
			var producto = _productos[comboProducto.SelectedIndex];
			var cantidad = campoCantidad.Text;
			var valorComboInvetarioElegido = comboInventario.Items[comboInventario.SelectedIndex];
			var lugarInventarioElegido = _valoreComboInventarios[valorComboInvetarioElegido];
			_listaProducto.Add(new DetallePedido()
			{
				IdProducto = producto[0],
				NombreProducto = producto[1],
				Cantidad = cantidad,
				Precio = (Convert.ToDouble(producto[2]) * Convert.ToDouble(cantidad)).ToString(),
				ColumnaStockElegido = lugarInventarioElegido
			});

			var total = 0.0;
			foreach (DetallePedido linea in _listaProducto)
			{
				total += Convert.ToDouble(linea.Precio);
			}
			var etiquetaTotal = (Label)BuscarControlEnHijos(ContenedorPedido, "total");
			etiquetaTotal.Text = "Total: " + total;
		}

		private void EliminarLineaDetalle(View boton, object e)
		{
			var tacho = (StackLayout)boton;
			var idProducto = tacho.ClassId;

			tacho.BackgroundColor = Color.FromHex("#FB9F0B");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				tacho.BackgroundColor = Color.FromHex("#FD8A18");
				return false;
			});

			for (int i = 0; i < _listaProducto.Count; i++)
			{
				if (_listaProducto[i].IdProducto == idProducto)
				{
					_listaProducto.Remove(_listaProducto[i]);
					break;
				}
			}
		}

		[Android.Runtime.Preserve]
		private void EventoGuardarPedido(View boton, object e)
		{
			var BotonGuardar = (StackLayout)boton;
			BotonGuardar.BackgroundColor = Color.FromHex("#32CEF9");
			Device.StartTimer(TimeSpan.FromMilliseconds(200), () =>
			{
				ContenedorClienteProducto.IsVisible = false;
				ContenedorGrillaTotal.IsVisible = false;

				GuardarPedido();
				BotonGuardar.BackgroundColor = Color.FromHex("#32BBF9");
				return false;
			});
		}

		protected override void OnSizeAllocated(double width, double height)
		{
			base.OnSizeAllocated(width, height);
			if (width > height)
			{
				ContenedorPedido.Orientation = StackOrientation.Horizontal;
				Titulo.IsVisible = false;
			}
			else
			{
				ContenedorPedido.Orientation = StackOrientation.Vertical;
				Titulo.IsVisible = true;
			}
		}

		#endregion
	}
}
