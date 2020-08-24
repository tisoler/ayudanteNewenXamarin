
using AyudanteNewen.Clases;
using System;
using Xamarin.Forms;

namespace AyudanteNewen.Vistas
{
	public partial class PaginaConexionBaseDeDatos
	{
		public PaginaConexionBaseDeDatos()
		{
			InitializeComponent();
			SombraEncabezado.Source = ImageSource.FromResource(App.RutaImagenSombraEncabezado);
			CuentaUsuario.AlmacenarAccesoDatos("B");
			Usuario.Text = CuentaUsuario.ObtenerUsuarioDeBaseDeDatos();
		}

		[Android.Runtime.Preserve]
		private void Conectar(object sender, EventArgs args)
		{
			if (Usuario.Text.ToUpper() == "HUGO" && Contrasena.Text == "Chavez")
			{
				const string token = "token1234";
				if (string.IsNullOrEmpty(token)) return;

				CuentaUsuario.AlmacenarTokenDeBaseDeDatos(token);
				CuentaUsuario.AlmacenarUsuarioDeBaseDeDatos(Usuario.Text.ToUpper());
				CuentaUsuario.AlmacenarColumnasParaVer("0,1,1");
				CuentaUsuario.AlmacenarColumnasInventario("0,0,1");
				
				App.Instancia.LimpiarNavegadorLuegoIrPagina(new PaginaGrilla());
			}
		}
	}
}
